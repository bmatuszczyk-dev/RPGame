using System.Collections.Generic;
using RPGame.Core.Buildup;
using UnityEngine;

namespace RPGame.UI.Buildup
{
    public sealed class BuildupUIController : MonoBehaviour
    {
        [SerializeField] private BuildupAggregator buildupAggregator;
        [SerializeField] private Transform entriesRoot;
        [SerializeField] private BuildupBarUI buildupBarPrefab;

        private readonly Dictionary<BuildupDefinition, BuildupBarUI> entries = new();

        private void OnEnable()
        {
            Subscribe();
            Synchronize();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearEntries();
        }

        private void Subscribe()
        {
            if (buildupAggregator == null)
            {
                return;
            }

            buildupAggregator.BuildupChanged += HandleBuildupChanged;
            buildupAggregator.BuildupsCleared += ClearEntries;
        }

        private void Unsubscribe()
        {
            if (buildupAggregator == null)
            {
                return;
            }

            buildupAggregator.BuildupChanged -= HandleBuildupChanged;
            buildupAggregator.BuildupsCleared -= ClearEntries;
        }

        private void Synchronize()
        {
            ClearEntries();
            if (buildupAggregator == null)
            {
                return;
            }

            IReadOnlyList<BuildupSnapshot> activeBuildups = buildupAggregator.ActiveBuildups;
            for (int i = 0; i < activeBuildups.Count; i++)
            {
                BuildupSnapshot buildup = activeBuildups[i];
                HandleBuildupChanged(buildup.Definition, buildup.CurrentAmount, buildup.Threshold);
            }
        }

        private void HandleBuildupChanged(BuildupDefinition definition, float currentAmount, float threshold)
        {
            if (definition == null)
            {
                return;
            }

            if (currentAmount <= 0f)
            {
                RemoveEntry(definition);
                return;
            }

            BuildupBarUI entry = GetOrCreateEntry(definition);
            entry?.SetProgress(currentAmount, threshold);
        }

        private BuildupBarUI GetOrCreateEntry(BuildupDefinition definition)
        {
            if (entries.TryGetValue(definition, out BuildupBarUI entry))
            {
                return entry;
            }

            if (buildupBarPrefab == null || entriesRoot == null)
            {
                return null;
            }

            entry = Instantiate(buildupBarPrefab, entriesRoot);
            entry.Initialize(definition);
            entries.Add(definition, entry);
            return entry;
        }

        private void RemoveEntry(BuildupDefinition definition)
        {
            if (!entries.Remove(definition, out BuildupBarUI entry) || entry == null)
            {
                return;
            }

            DestroyEntry(entry.gameObject);
        }

        private void ClearEntries()
        {
            foreach (KeyValuePair<BuildupDefinition, BuildupBarUI> entry in entries)
            {
                if (entry.Value != null)
                {
                    DestroyEntry(entry.Value.gameObject);
                }
            }

            entries.Clear();
        }

        private static void DestroyEntry(GameObject entry)
        {
            if (Application.isPlaying)
            {
                Destroy(entry);
                return;
            }

            DestroyImmediate(entry);
        }
    }
}
