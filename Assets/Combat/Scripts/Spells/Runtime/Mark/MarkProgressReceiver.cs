using System;
using System.Collections.Generic;
using RPGame.Core.Pooling;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [RequireComponent(typeof(StatusAggregator))]
    public sealed class MarkProgressReceiver : MonoBehaviour, IMarkProgressReader, IPooledEnemyResettable
    {
        [SerializeField] private MarkStatusDefinition markDefinition;

        private StatusAggregator statusAggregator;
        private readonly Dictionary<GameObject, MarkProgressTracker> trackers = new();

        public int CurrentUniqueSpellProgress => GetDisplayedTracker()?.CurrentUniqueSpellProgress ?? 0;
        public float ComboWindowExpiresAt => GetDisplayedTracker()?.ComboWindowExpiresAt ?? -1f;
        public bool IsMarkActive => statusAggregator != null
            && markDefinition != null
            && statusAggregator.HasStatus(markDefinition);
        public event Action ProgressChanged;
        public event Action ProgressExpired;

        private void Awake()
        {
            ResolveStatusAggregator();
        }

        private void OnEnable()
        {
            ResolveStatusAggregator();
            SubscribeToStatusChanges();
        }

        private void OnDisable()
        {
            UnsubscribeFromStatusChanges();
        }

        private void Update()
        {
            if (trackers.Count == 0)
            {
                return;
            }

            float currentTime = Time.time;
            foreach (KeyValuePair<GameObject, MarkProgressTracker> pair in trackers)
            {
                pair.Value.ExpireIfNecessary(currentTime, pair.Key);
            }
        }

        public bool RecordSuccessfulSpell(SpellId spellId, StatusContext context)
        {
            ResolveStatusAggregator();
            if (statusAggregator == null || markDefinition == null || context.Source == null)
            {
                return false;
            }

            if (!trackers.TryGetValue(context.Source, out MarkProgressTracker tracker))
            {
                tracker = new MarkProgressTracker(statusAggregator, markDefinition);
                tracker.ProgressChanged += HandleTrackerProgressChanged;
                tracker.ProgressExpired += HandleTrackerProgressExpired;
                trackers.Add(context.Source, tracker);
            }

            bool recorded = tracker.RecordSuccessfulSpell(spellId, context);
            if (statusAggregator.HasStatus(markDefinition))
            {
                ClearTrackers();
            }

            return recorded;
        }

        public void ResetForSpawn()
        {
            ResetTrackers();
        }

        public void ResetForDespawn()
        {
            ResetTrackers();
        }

        private void ResolveStatusAggregator()
        {
            if (statusAggregator == null)
            {
                statusAggregator = GetComponent<StatusAggregator>();
            }
        }

        private void SubscribeToStatusChanges()
        {
            if (statusAggregator == null)
            {
                return;
            }

            statusAggregator.StatusAdded += HandleStatusChanged;
            statusAggregator.StatusRemoved += HandleStatusChanged;
        }

        private void UnsubscribeFromStatusChanges()
        {
            if (statusAggregator == null)
            {
                return;
            }

            statusAggregator.StatusAdded -= HandleStatusChanged;
            statusAggregator.StatusRemoved -= HandleStatusChanged;
        }

        private void HandleStatusChanged(StatusInstance status)
        {
            if (status?.Definition != markDefinition)
            {
                return;
            }

            if (IsMarkActive)
            {
                ClearTrackers();
            }

            ProgressChanged?.Invoke();
        }

        private void HandleTrackerProgressChanged()
        {
            ProgressChanged?.Invoke();
        }

        private void HandleTrackerProgressExpired()
        {
            ProgressExpired?.Invoke();
        }

        private void ClearTrackers()
        {
            foreach (MarkProgressTracker tracker in trackers.Values)
            {
                tracker.ClearProgress();
            }
        }

        private void ResetTrackers()
        {
            ClearTrackers();
            foreach (MarkProgressTracker tracker in trackers.Values)
            {
                tracker.ProgressChanged -= HandleTrackerProgressChanged;
                tracker.ProgressExpired -= HandleTrackerProgressExpired;
            }

            trackers.Clear();
        }

        private MarkProgressTracker GetDisplayedTracker()
        {
            MarkProgressTracker displayedTracker = null;
            foreach (MarkProgressTracker tracker in trackers.Values)
            {
                if (displayedTracker == null
                    || tracker.CurrentUniqueSpellProgress > displayedTracker.CurrentUniqueSpellProgress
                    || (tracker.CurrentUniqueSpellProgress == displayedTracker.CurrentUniqueSpellProgress
                        && tracker.ComboWindowExpiresAt > displayedTracker.ComboWindowExpiresAt))
                {
                    displayedTracker = tracker;
                }
            }

            return displayedTracker;
        }
    }
}
