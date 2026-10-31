using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RPGame.Leaderboard
{
    public readonly struct PlayerNameOperationResult
    {
        public PlayerNameOperationResult(bool isSuccess, string playerName, string errorMessage)
        {
            IsSuccess = isSuccess;
            PlayerName = playerName;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }
        public string PlayerName { get; }
        public string ErrorMessage { get; }
    }

    public static class PlayerNameService
    {
        private const int MaximumPlayerNameLength = 50;
        private const int MinimumSubstringMatchLength = 5;
        private const string BlockedWordsResourcePath = "PlayerNameBlockedWords";
        private const string ServicesUnavailableMessage = "Unable to connect to Unity Services. Please try again.";
        private const string PlayerNameUpdateFailedMessage = "Unable to update player name. Please try again.";
        private const string DisallowedNameMessage = "This player name is not allowed.";

        private static readonly HashSet<string> exactMatches = new(StringComparer.Ordinal);
        private static readonly SubstringMatcher substringMatcher = new();

        static PlayerNameService()
        {
            LoadBlockedWords();
        }

        public static async Task<PlayerNameOperationResult> GetCurrentPlayerNameAsync()
        {
            await LeaderboardService.InitializeAsync();
            if (LeaderboardService.State != LeaderboardServiceState.Ready)
            {
                return Failure(ServicesUnavailableMessage);
            }

            return Success(LeaderboardService.PlayerName);
        }

        public static async Task<PlayerNameOperationResult> SetPlayerNameAsync(string playerName)
        {
            if (!TryValidatePlayerName(playerName, out string errorMessage))
            {
                return Failure(errorMessage);
            }

            try
            {
                await LeaderboardService.UpdatePlayerNameAsync(playerName);
                return Success(LeaderboardService.PlayerName);
            }
            catch (Exception exception)
            {
                if (LeaderboardService.State != LeaderboardServiceState.Ready)
                {
                    return Failure(ServicesUnavailableMessage);
                }

                Debug.LogError($"Player name update request failed: {exception.Message}");
                return Failure(PlayerNameUpdateFailedMessage);
            }
        }

        public static bool TryValidatePlayerName(string playerName, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                errorMessage = "Enter a player name.";
                return false;
            }

            if (playerName.Length > MaximumPlayerNameLength)
            {
                errorMessage = $"Player name must not exceed {MaximumPlayerNameLength} characters.";
                return false;
            }

            for (int i = 0; i < playerName.Length; i++)
            {
                if (char.IsWhiteSpace(playerName[i]))
                {
                    errorMessage = "Player name cannot contain whitespace.";
                    return false;
                }

                if (char.IsControl(playerName[i]))
                {
                    errorMessage = "Player name cannot contain control characters.";
                    return false;
                }
            }

            if (ContainsBlockedWord(playerName))
            {
                errorMessage = DisallowedNameMessage;
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static void LoadBlockedWords()
        {
            TextAsset configuration = Resources.Load<TextAsset>(BlockedWordsResourcePath);
            if (configuration == null)
            {
                Debug.LogError("Player name blocked-word configuration is missing.");
                return;
            }

            string[] lines = configuration.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                string normalizedWord = NormalizeForComparison(DecodeEscapedUnicode(line));
                if (normalizedWord.Length > 0)
                {
                    if (normalizedWord.Length < MinimumSubstringMatchLength)
                    {
                        exactMatches.Add(normalizedWord);
                    }
                    else
                    {
                        substringMatcher.Add(normalizedWord);
                    }
                }
            }

            substringMatcher.BuildFailureLinks();
        }

        private static string DecodeEscapedUnicode(string value)
        {
            if (!value.Contains("\\u", StringComparison.Ordinal))
            {
                return value;
            }

            StringBuilder builder = new(value.Length);
            for (int index = 0; index < value.Length; index++)
            {
                if (value[index] == '\\' && index + 5 < value.Length && value[index + 1] == 'u' &&
                    int.TryParse(value.Substring(index + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int characterCode))
                {
                    builder.Append((char)characterCode);
                    index += 5;
                    continue;
                }

                builder.Append(value[index]);
            }

            return builder.ToString();
        }

        private static bool ContainsBlockedWord(string playerName)
        {
            string normalizedName = NormalizeForComparison(playerName);
            if (ContainsBlockedMatch(playerName.ToLowerInvariant()) || ContainsBlockedMatch(normalizedName))
            {
                return true;
            }

            return HasElongatedCharacters(normalizedName) &&
                   ContainsBlockedMatch(CollapseElongatedCharacters(normalizedName));
        }

        private static bool ContainsBlockedMatch(string value)
        {
            if (exactMatches.Contains(value))
            {
                return true;
            }

            return substringMatcher.ContainsMatch(value);
        }

        private static string NormalizeForComparison(string value)
        {
            StringBuilder normalized = new(value.Length);
            string decomposedValue = value.Normalize(NormalizationForm.FormD);
            for (int i = 0; i < decomposedValue.Length; i++)
            {
                char character = char.ToLowerInvariant(decomposedValue[i]);
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                char mappedCharacter = MapSubstitution(character);
                if (mappedCharacter != '\0')
                {
                    normalized.Append(mappedCharacter);
                }
                else if (char.IsLetterOrDigit(character))
                {
                    normalized.Append(character == '\u0142' ? 'l' : character);
                }
            }

            return normalized.ToString();
        }

        private static bool HasElongatedCharacters(string value)
        {
            for (int i = 2; i < value.Length; i++)
            {
                if (value[i] == value[i - 1] && value[i] == value[i - 2])
                {
                    return true;
                }
            }

            return false;
        }

        private static string CollapseElongatedCharacters(string value)
        {
            StringBuilder collapsed = new(value.Length);
            for (int i = 0; i < value.Length;)
            {
                int runEnd = i + 1;
                while (runEnd < value.Length && value[runEnd] == value[i])
                {
                    runEnd++;
                }

                int runLength = runEnd - i;
                collapsed.Append(value[i], runLength >= 3 ? 1 : runLength);
                i = runEnd;
            }

            return collapsed.ToString();
        }

        private static char MapSubstitution(char character)
        {
            return character switch
            {
                '@' => 'a',
                '4' => 'a',
                '0' => 'o',
                '1' => 'i',
                '!' => 'i',
                '|' => 'i',
                '3' => 'e',
                '\u20ac' => 'e',
                '5' => 's',
                '$' => 's',
                '7' => 't',
                '+' => 't',
                '8' => 'b',
                '9' => 'g',
                '6' => 'g',
                '2' => 'z',
                '\u0430' => 'a',
                '\u0432' => 'b',
                '\u0441' => 'c',
                '\u0435' => 'e',
                '\u04bb' => 'h',
                '\u0456' => 'i',
                '\u0458' => 'j',
                '\u043a' => 'k',
                '\u04cf' => 'l',
                '\u043c' => 'm',
                '\u043d' => 'h',
                '\u043e' => 'o',
                '\u0440' => 'p',
                '\u0455' => 's',
                '\u0442' => 't',
                '\u0445' => 'x',
                '\u0443' => 'y',
                _ => '\0'
            };
        }

        private sealed class SubstringMatcher
        {
            private readonly List<Node> nodes = new() { new Node() };

            public void Add(string pattern)
            {
                int state = 0;
                for (int i = 0; i < pattern.Length; i++)
                {
                    char character = pattern[i];
                    if (!nodes[state].Transitions.TryGetValue(character, out int nextState))
                    {
                        nextState = nodes.Count;
                        nodes[state].Transitions.Add(character, nextState);
                        nodes.Add(new Node());
                    }

                    state = nextState;
                }

                nodes[state].IsMatch = true;
            }

            public void BuildFailureLinks()
            {
                Queue<int> queue = new();
                foreach (int state in nodes[0].Transitions.Values)
                {
                    queue.Enqueue(state);
                }

                while (queue.Count > 0)
                {
                    int state = queue.Dequeue();
                    foreach (KeyValuePair<char, int> transition in nodes[state].Transitions)
                    {
                        char character = transition.Key;
                        int nextState = transition.Value;
                        int failureState = nodes[state].Failure;

                        while (failureState != 0 && !nodes[failureState].Transitions.ContainsKey(character))
                        {
                            failureState = nodes[failureState].Failure;
                        }

                        if (nodes[failureState].Transitions.TryGetValue(character, out int fallbackState))
                        {
                            nodes[nextState].Failure = fallbackState;
                        }

                        nodes[nextState].IsMatch |= nodes[nodes[nextState].Failure].IsMatch;
                        queue.Enqueue(nextState);
                    }
                }
            }

            public bool ContainsMatch(string value)
            {
                int state = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    char character = value[i];
                    int nextState = 0;
                    while (!nodes[state].Transitions.TryGetValue(character, out nextState))
                    {
                        if (state == 0)
                        {
                            break;
                        }

                        state = nodes[state].Failure;
                    }

                    state = nextState;
                    if (nodes[state].IsMatch)
                    {
                        return true;
                    }
                }

                return false;
            }

            private sealed class Node
            {
                public readonly Dictionary<char, int> Transitions = new();
                public int Failure;
                public bool IsMatch;
            }
        }

        private static PlayerNameOperationResult Success(string playerName)
        {
            return new PlayerNameOperationResult(true, playerName, string.Empty);
        }

        private static PlayerNameOperationResult Failure(string errorMessage)
        {
            return new PlayerNameOperationResult(false, string.Empty, errorMessage);
        }
    }
}
