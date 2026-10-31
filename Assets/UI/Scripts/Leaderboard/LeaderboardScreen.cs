using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RPGame.Leaderboard;
using RPGame.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RPGame.UI.Leaderboard
{
    public sealed class LeaderboardScreen : MonoBehaviour
    {
        [SerializeField] private LeaderboardEntryUI[] topScoreRows;
        [SerializeField] private LeaderboardEntryUI[] playerCenteredRows;
        [SerializeField] private GameObject playerCenteredSection;
        [SerializeField] private LeaderboardRunSubmitter runSubmitter;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private LoadingImageSpinner loadingSpinner;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text leaderboardText;

        private int loadVersion;

        private void OnEnable()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowSummary);
                backButton.onClick.AddListener(ShowSummary);
            }

        }

        private void OnDisable()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowSummary);
            }
        }

        private void Update()
        {
            if (leaderboardPanel != null
                && leaderboardPanel.activeInHierarchy
                && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                ShowSummary();
            }
        }

        public void Open()
        {
            if (summaryPanel != null)
            {
                summaryPanel.SetActive(false);
            }

            SetLeaderboardPanelVisible(true);
            _ = LoadAsync();
        }

        public void Close()
        {
            if (leaderboardPanel == null || !leaderboardPanel.activeSelf)
            {
                return;
            }

            loadVersion++;
            SetLoadingSpinnerVisible(false);
            SetStatus(string.Empty);
            SetLeaderboardPanelVisible(false);
        }

        public Task LoadAsync()
        {
            return LoadAsync(++loadVersion);
        }

        private async Task LoadAsync(int version)
        {
            ResetRows();
            SetLoadingSpinnerVisible(true);
            SetStatus("Loading leaderboard...");

            try
            {
                if (runSubmitter != null)
                {
                    await runSubmitter.SubmissionTask;
                    if (!CanUpdateUI(version))
                    {
                        return;
                    }
                }

                IReadOnlyList<LeaderboardEntry> topScores = await LeaderboardService.GetTopScoresAsync();
                if (!CanUpdateUI(version))
                {
                    return;
                }

                PopulateRows(topScoreRows, topScores);

                if (topScores.Count == 0)
                {
                    SetLeaderboardText(topScores, null);
                    SetStatus("No leaderboard entries yet.");
                    return;
                }

                SetLeaderboardText(topScores, null);
                SetStatus(string.Empty);

                if (ContainsCurrentPlayer(topScores))
                {
                    return;
                }

                if (playerCenteredSection != null)
                {
                    playerCenteredSection.SetActive(true);
                }

                IReadOnlyList<LeaderboardEntry> playerCenteredScores =
                    await LeaderboardService.GetPlayerCenteredScoresAsync();
                if (!CanUpdateUI(version))
                {
                    return;
                }

                PopulateRows(playerCenteredRows, playerCenteredScores);
                SetLeaderboardText(topScores, playerCenteredScores);
            }
            catch
            {
                if (CanUpdateUI(version))
                {
                    SetStatus("Unable to load leaderboard.");
                }
            }
            finally
            {
                if (this != null && version == loadVersion)
                {
                    SetLoadingSpinnerVisible(false);
                }
            }
        }

        private void ResetRows()
        {
            ResetRows(topScoreRows);
            ResetRows(playerCenteredRows);

            if (playerCenteredSection != null)
            {
                playerCenteredSection.SetActive(false);
            }

            if (leaderboardText != null)
            {
                leaderboardText.text = string.Empty;
            }
        }

        private static void ResetRows(LeaderboardEntryUI[] rows)
        {
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] != null)
                {
                    rows[i].ResetRow();
                }
            }
        }

        private static void PopulateRows(
            LeaderboardEntryUI[] rows,
            IReadOnlyList<LeaderboardEntry> entries)
        {
            if (rows == null || entries == null)
            {
                return;
            }

            int entryIndex = 0;
            for (int rowIndex = 0; rowIndex < rows.Length && entryIndex < entries.Count; rowIndex++)
            {
                if (rows[rowIndex] == null)
                {
                    continue;
                }

                rows[rowIndex].Populate(entries[entryIndex]);
                entryIndex++;
            }
        }

        private static bool ContainsCurrentPlayer(IReadOnlyList<LeaderboardEntry> entries)
        {
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].IsCurrentPlayer)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanUpdateUI(int version)
        {
            return this != null
                && version == loadVersion
                && isActiveAndEnabled
                && leaderboardPanel != null
                && leaderboardPanel.activeInHierarchy;
        }

        private void ShowSummary()
        {
            Close();

            if (summaryPanel != null)
            {
                summaryPanel.SetActive(true);
            }
        }

        private void SetLeaderboardPanelVisible(bool isVisible)
        {
            if (leaderboardPanel != null && leaderboardPanel.activeSelf != isVisible)
            {
                leaderboardPanel.SetActive(isVisible);
            }
        }

        private void SetLoadingSpinnerVisible(bool isVisible)
        {
            if (loadingSpinner != null && loadingSpinner.gameObject.activeSelf != isVisible)
            {
                loadingSpinner.gameObject.SetActive(isVisible);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
            statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void SetLeaderboardText(
            IReadOnlyList<LeaderboardEntry> topScores,
            IReadOnlyList<LeaderboardEntry> playerCenteredScores)
        {
            if (leaderboardText == null)
            {
                return;
            }

            var builder = new StringBuilder("TOP 10\n\nRANK     PLAYER                         WAVES     SCORE\n");
            AppendEntries(builder, topScores);

            if (playerCenteredScores != null && playerCenteredScores.Count > 0)
            {
                builder.Append("\nYOUR POSITION\n\nRANK     PLAYER                         WAVES     SCORE\n");
                AppendEntries(builder, playerCenteredScores);
            }

            leaderboardText.text = builder.ToString();
        }

        private static void AppendEntries(StringBuilder builder, IReadOnlyList<LeaderboardEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                LeaderboardEntry entry = entries[index];
                if (entry.IsCurrentPlayer)
                {
                    builder.Append("<b><color=#FFD54F>");
                }

                builder.Append(entry.Rank)
                    .Append("        ")
                    .Append(entry.PlayerName)
                    .Append("        ")
                    .Append(entry.WavesCompleted)
                    .Append("        ")
                    .Append(entry.Score.ToString("N0"));

                if (entry.IsCurrentPlayer)
                {
                    builder.Append("</color></b>");
                }

                builder.Append('\n');
            }
        }
    }
}
