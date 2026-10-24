using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGame.Encounter;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using UgsLeaderboardEntry = Unity.Services.Leaderboards.Models.LeaderboardEntry;

namespace RPGame.Leaderboard
{
    public enum LeaderboardServiceState
    {
        NotInitialized,
        Initializing,
        Ready,
        Failed
    }

    public static class LeaderboardService
    {
        private const string LeaderboardId = "endless_wave_leaderboard";
        private const string SubmitEndlessRunEndpoint = "SubmitEndlessRun";
        private const int TopScoreLimit = 10;
        private const int PlayerRangeLimit = 3;

        private static Task initializationTask;
        private static ILeaderboardsService leaderboardService;

        public static LeaderboardServiceState State { get; private set; } = LeaderboardServiceState.NotInitialized;
        public static string PlayerId { get; private set; }
        public static string PlayerName { get; private set; }

        public static Task InitializeAsync()
        {
            if (State == LeaderboardServiceState.Ready)
            {
                return Task.CompletedTask;
            }

            if (State == LeaderboardServiceState.Initializing && initializationTask != null)
            {
                return initializationTask;
            }

            initializationTask = InitializeInternalAsync();
            return initializationTask;
        }

        public static async Task RefreshPlayerNameAsync()
        {
            await InitializeAsync();
            if (State != LeaderboardServiceState.Ready)
            {
                return;
            }

            PlayerName = AuthenticationService.Instance.PlayerName;
            PlayerId = AuthenticationService.Instance.PlayerId;
        }

        public static async Task UpdatePlayerNameAsync(string playerName)
        {
            await InitializeAsync();
            if (State != LeaderboardServiceState.Ready)
            {
                throw new InvalidOperationException("Leaderboard service is not initialized.");
            }

            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            PlayerName = AuthenticationService.Instance.PlayerName;
            PlayerId = AuthenticationService.Instance.PlayerId;
        }

        public static async Task SubmitScoreAsync(EncounterResult result)
        {
            if (!IsValid(result))
            {
                Debug.LogWarning("Leaderboard score submission skipped: invalid run result.");
                return;
            }

            await InitializeAsync();
            if (State != LeaderboardServiceState.Ready)
            {
                return;
            }

            try
            {
                await CloudCodeService.Instance.CallEndpointAsync<bool>(
                    SubmitEndlessRunEndpoint,
                    new Dictionary<string, object>
                    {
                        { "score", result.Score },
                        { "wavesCompleted", result.WavesCompleted },
                        { "encounterSeed", result.EncounterSeed },
                        { "gameVersion", Application.version }
                    });
            }
            catch (Exception exception)
            {
                Debug.LogError($"Leaderboard score submission request failed: {exception.Message}");
            }
        }

        public static async Task<IReadOnlyList<LeaderboardEntry>> GetTopScoresAsync()
        {
            await InitializeAsync();
            if (State != LeaderboardServiceState.Ready)
            {
                throw new InvalidOperationException("Leaderboard service is not initialized.");
            }

            try
            {
                var response = await leaderboardService.GetScoresAsync(
                    LeaderboardId,
                    new GetScoresOptions
                    {
                        Limit = TopScoreLimit,
                        IncludeMetadata = true
                    });
                return MapEntries(response.Results);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Leaderboard top scores request failed: {exception.Message}");
                throw;
            }
        }

        public static async Task<LeaderboardEntry?> GetCurrentPlayerScoreAsync()
        {
            await InitializeAsync();
            if (State != LeaderboardServiceState.Ready)
            {
                return null;
            }

            try
            {
                UgsLeaderboardEntry response = await leaderboardService.GetPlayerScoreAsync(
                    LeaderboardId,
                    new GetPlayerScoreOptions
                    {
                        IncludeMetadata = true
                    });
                return MapEntry(response);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Leaderboard player score request failed: {exception.Message}");
                return null;
            }
        }

        public static async Task<IReadOnlyList<LeaderboardEntry>> GetPlayerCenteredScoresAsync()
        {
            await InitializeAsync();
            if (State != LeaderboardServiceState.Ready)
            {
                throw new InvalidOperationException("Leaderboard service is not initialized.");
            }

            try
            {
                var response = await leaderboardService.GetPlayerRangeAsync(
                    LeaderboardId,
                    new GetPlayerRangeOptions
                    {
                        RangeLimit = PlayerRangeLimit,
                        IncludeMetadata = true
                    });
                return MapEntries(response.Results);
            }
            catch (Exception exception) when (IsPlayerEntryMissing(exception))
            {
                return Array.Empty<LeaderboardEntry>();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Leaderboard player range request failed: {exception.Message}");
                throw;
            }
        }

        private static async Task InitializeInternalAsync()
        {
            State = LeaderboardServiceState.Initializing;

            try
            {
                await UnityServices.InitializeAsync();
                leaderboardService = UnityServices.Instance.GetLeaderboardsService();
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                PlayerId = AuthenticationService.Instance.PlayerId;
                PlayerName = AuthenticationService.Instance.PlayerName;
                State = LeaderboardServiceState.Ready;
            }
            catch (Exception exception)
            {
                SetFailed("initializing", exception);
            }
        }

        private static void SetFailed(string operation, Exception exception)
        {
            State = LeaderboardServiceState.Failed;
            Debug.LogError($"Leaderboard service failed while {operation}: {exception.Message}");
        }

        private static bool IsValid(EncounterResult result)
        {
            return result.Score >= 0f
                && !float.IsNaN(result.Score)
                && !float.IsInfinity(result.Score)
                && result.WavesCompleted >= 0;
        }

        private static IReadOnlyList<LeaderboardEntry> MapEntries(IEnumerable<UgsLeaderboardEntry> entries)
        {
            List<LeaderboardEntry> mappedEntries = new();
            if (entries == null)
            {
                return mappedEntries;
            }

            foreach (UgsLeaderboardEntry entry in entries)
            {
                mappedEntries.Add(MapEntry(entry));
            }

            return mappedEntries;
        }

        private static LeaderboardEntry MapEntry(UgsLeaderboardEntry entry)
        {
            string entryPlayerId = entry.PlayerId ?? string.Empty;
            return new LeaderboardEntry(
                Math.Max(1, entry.Rank + 1),
                entryPlayerId,
                entry.PlayerName ?? string.Empty,
                (float)entry.Score,
                GetWavesCompleted(entry.Metadata),
                !string.IsNullOrEmpty(PlayerId) && string.Equals(entryPlayerId, PlayerId, StringComparison.Ordinal));
        }

        private static int GetWavesCompleted(string metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata))
            {
                return 0;
            }

            try
            {
                ScoreMetadata scoreMetadata = JsonUtility.FromJson<ScoreMetadata>(metadata);
                return scoreMetadata != null && scoreMetadata.wavesCompleted >= 0
                    ? scoreMetadata.wavesCompleted
                    : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static bool IsPlayerEntryMissing(Exception exception)
        {
            return exception.Message.IndexOf(
                "Leaderboard entry could not be found",
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [Serializable]
        private sealed class ScoreMetadata
        {
            public int wavesCompleted;
            public int encounterSeed;
            public string gameVersion;
        }
    }
}
