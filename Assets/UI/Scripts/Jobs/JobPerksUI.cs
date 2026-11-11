using System.Collections.Generic;
using RPGame.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace RPGame.UI.Jobs
{
    public sealed class JobPerksUI : MonoBehaviour
    {
        [SerializeField] private CharacterProgression progression;
        [SerializeField] private JobDefinition editorPreviewJob;
        [SerializeField] private TMP_Text jobNameText;
        [SerializeField] private TMP_Text availableJobPointsText;
        [SerializeField] private RectTransform perkNodesParent;
        [SerializeField] private PerkTreeConnectionsGraphic connectionsGraphic;
        [SerializeField] private PerkTooltipUI perkTooltip;
        [SerializeField] private RectTransform perkNodePrefab;
        [SerializeField] private Button confirmPerkUnlocksButton;
        [SerializeField] private Button cancelPerkUnlocksButton;

        private readonly List<GameObject> spawnedPerkNodes = new();
        private readonly PendingPerkUnlocks pendingPerkUnlocks = new();
        private JobInstance job;

        private void Awake()
        {
            ResolveProgression();

            if (confirmPerkUnlocksButton != null)
            {
                confirmPerkUnlocksButton.onClick.AddListener(ConfirmPendingPerkUnlocks);
            }

            if (cancelPerkUnlocksButton != null)
            {
                cancelPerkUnlocksButton.onClick.AddListener(CancelPendingPerkUnlocks);
            }
        }

        private void OnDestroy()
        {
            if (confirmPerkUnlocksButton != null)
            {
                confirmPerkUnlocksButton.onClick.RemoveListener(ConfirmPendingPerkUnlocks);
            }

            if (cancelPerkUnlocksButton != null)
            {
                cancelPerkUnlocksButton.onClick.RemoveListener(CancelPendingPerkUnlocks);
            }
        }

        private void OnEnable()
        {
            ResolveProgression();
            Refresh();
        }

        public void SetProgression(CharacterProgression progression)
        {
            if (this.progression == progression)
            {
                return;
            }

            this.progression = progression;
            ClearPendingPerks();
            Refresh();
        }

        public void SetJob(JobInstance job)
        {
            GeneratePerkTree(job);
        }

        public void GeneratePerkTree(JobDefinition jobDefinition)
        {
            GeneratePerkTree(jobDefinition != null ? new JobInstance(jobDefinition) : null);
        }

        public void GeneratePerkTree(JobInstance job)
        {
            this.job = job;
            ClearPendingPerks();
            ResolveProgression();
            Refresh();
        }

        [ContextMenu("Rebuild Perk Tree")]
        private void RebuildPerkTreeFromContextMenu()
        {
            if (job?.Definition != null)
            {
                GeneratePerkTree(job);
                return;
            }

            if (editorPreviewJob == null)
            {
                Debug.LogWarning("Cannot rebuild perk tree because Editor Preview Job is not assigned.", this);
                GeneratePerkTree((JobInstance)null);
                return;
            }

            GeneratePerkTree(editorPreviewJob);
        }

        public void Refresh()
        {
            if (jobNameText != null)
            {
                jobNameText.text = job?.Definition != null ? job.Definition.DisplayName : job?.JobId ?? string.Empty;
            }

            if (availableJobPointsText != null)
            {
                int availableJobPoints = job != null ? job.JobPoints : 0;
                int pendingCost = pendingPerkUnlocks.GetPendingCost();
                availableJobPointsText.text = pendingCost > 0
                    ? $"Job Points: {availableJobPoints} (-{pendingCost})"
                    : $"Job Points: {availableJobPoints}";
            }

            RefreshConfirmButton();

            int createdNodes = RebuildPerkTree();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                RefreshEditorLayout();
                Debug.Log(
                    $"Rebuilt perk tree for {(job?.Definition != null ? job.Definition.DisplayName : "none")} under {(perkNodesParent != null ? perkNodesParent.name : "none")}. Created {createdNodes} nodes.",
                    this);
            }
#endif
        }

        private int RebuildPerkTree()
        {
            return PerkTreeBuilder.Rebuild(
                job,
                perkNodesParent,
                connectionsGraphic,
                perkNodePrefab,
                spawnedPerkNodes,
                GetPerkUnlockState,
                IsPerkPending,
                TogglePendingPerk,
                ShowPerkTooltip,
                HidePerkTooltip,
                Refresh);
        }

#if UNITY_EDITOR
        private void RefreshEditorLayout()
        {
            if (perkNodesParent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(perkNodesParent);
            }

            Canvas.ForceUpdateCanvases();
            EditorUtility.SetDirty(this);

            if (perkNodesParent != null)
            {
                EditorUtility.SetDirty(perkNodesParent);
            }

            if (gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
#endif

        private void ResolveProgression()
        {
            if (progression == null)
            {
                progression = FindAnyObjectByType<CharacterProgression>();
            }
        }

        private PerkUnlockState GetPerkUnlockState(PerkDefinition perk)
        {
            return pendingPerkUnlocks.GetPreviewUnlockState(job, perk, progression);
        }

        private bool IsPerkPending(PerkDefinition perk)
        {
            return pendingPerkUnlocks.IsPending(perk);
        }

        private void TogglePendingPerk(PerkDefinition perk)
        {
            pendingPerkUnlocks.Toggle(job, perk, progression);
        }

        private void ShowPerkTooltip(PerkDefinition perk, Vector2 screenPosition)
        {
            if (perkTooltip != null)
            {
                perkTooltip.Show(perk, screenPosition);
            }
        }

        private void HidePerkTooltip()
        {
            if (perkTooltip != null)
            {
                perkTooltip.Hide();
            }
        }

        private void ConfirmPendingPerkUnlocks()
        {
            if (progression == null || job == null || !pendingPerkUnlocks.HasPendingPerks)
            {
                return;
            }

            IReadOnlyList<PerkDefinition> pendingPerks = pendingPerkUnlocks.PendingPerks;
            for (int i = 0; i < pendingPerks.Count; i++)
            {
                progression.TryUnlockPerk(job, pendingPerks[i]);
            }

            ClearPendingPerks();
            Refresh();
        }

        private void CancelPendingPerkUnlocks()
        {
            if (!pendingPerkUnlocks.HasPendingPerks)
            {
                return;
            }

            ClearPendingPerks();
            Refresh();
        }

        private void ClearPendingPerks()
        {
            pendingPerkUnlocks.Clear();
        }

        private void RefreshConfirmButton()
        {
            bool hasPendingPerks = pendingPerkUnlocks.HasPendingPerks;

            if (confirmPerkUnlocksButton != null)
            {
                confirmPerkUnlocksButton.interactable = hasPendingPerks;
            }

            if (cancelPerkUnlocksButton != null)
            {
                cancelPerkUnlocksButton.interactable = hasPendingPerks;
            }
        }
    }
}
