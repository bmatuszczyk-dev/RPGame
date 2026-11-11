using System.Collections.Generic;
using RPGame.Core.Pooling;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.UI
{
    public sealed class StatusPoolUI : MonoBehaviour, IPooledEnemyResettable
    {
        [SerializeField] private StatusAggregator statusAggregator;
        [SerializeField] private StatusIndicatorUI statusIndicatorPrefab;
        [SerializeField] private Transform container;

        private readonly Dictionary<StatusInstance, StatusIndicatorUI> indicators = new();

        public bool HasIndicators => indicators.Count > 0;

        private void Awake()
        {
            container ??= transform;
            statusAggregator ??= GetComponentInParent<StatusAggregator>();
        }

        private void OnEnable()
        {
            Subscribe();
            Synchronize();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearIndicators();
        }

        public void ResetForSpawn()
        {
            ClearIndicators();
            Synchronize();
        }

        public void ResetForDespawn()
        {
            ClearIndicators();
        }

        private void Subscribe()
        {
            if (statusAggregator == null)
            {
                return;
            }

            statusAggregator.StatusAdded += HandleStatusAdded;
            statusAggregator.StatusUpdated += HandleStatusUpdated;
            statusAggregator.StatusRemoved += HandleStatusRemoved;
        }

        private void Unsubscribe()
        {
            if (statusAggregator == null)
            {
                return;
            }

            statusAggregator.StatusAdded -= HandleStatusAdded;
            statusAggregator.StatusUpdated -= HandleStatusUpdated;
            statusAggregator.StatusRemoved -= HandleStatusRemoved;
        }

        private void Synchronize()
        {
            if (statusAggregator == null)
            {
                return;
            }

            IReadOnlyList<StatusInstance> statuses = statusAggregator.Statuses;
            for (int i = 0; i < statuses.Count; i++)
            {
                AddIndicator(statuses[i]);
            }
        }

        private void HandleStatusAdded(StatusInstance status)
        {
            AddIndicator(status);
        }

        private void HandleStatusUpdated(StatusInstance status)
        {
            if (indicators.TryGetValue(status, out StatusIndicatorUI indicator))
            {
                indicator.Refresh(status);
                return;
            }

            AddIndicator(status);
        }

        private void HandleStatusRemoved(StatusInstance status)
        {
            if (!indicators.Remove(status, out StatusIndicatorUI indicator))
            {
                return;
            }

            indicator.gameObject.SetActive(false);
            Destroy(indicator.gameObject);
        }

        private void AddIndicator(StatusInstance status)
        {
            if (!CanDisplay(status) || indicators.ContainsKey(status))
            {
                return;
            }

            StatusIndicatorUI indicator = Instantiate(statusIndicatorPrefab, container);
            indicator.Initialize(status);
            indicators.Add(status, indicator);
        }

        private void ClearIndicators()
        {
            foreach (StatusIndicatorUI indicator in indicators.Values)
            {
                if (indicator == null)
                {
                    continue;
                }

                indicator.gameObject.SetActive(false);
                Destroy(indicator.gameObject);
            }

            indicators.Clear();
        }

        private bool CanDisplay(StatusInstance status)
        {
            return status != null
                && status.Definition is not MarkStatusDefinition
                && status.Definition?.Icon != null
                && statusIndicatorPrefab != null;
        }
    }
}
