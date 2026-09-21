using System.Collections.Generic;
using RPGame.Core.Damage;
using RPGame.Core.Movement;
using RPGame.Core.Pooling;
using RPGame.Core.Statistics;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Core.Statuses
{
    [MovedFrom(true, null, null, "StatusEffectAggregator")]
    public sealed class StatusAggregator : MonoBehaviour, IStatusReceiver, IPooledEnemyResettable
    {
        private StatusContainer statusContainer;
        private IStatisticsController statisticsController;
        private IMovement movement;
        private IDamageable damageable;
        private StatusTarget statusTarget;
        private IStatisticsController subscribedStatisticsController;

        public IReadOnlyList<StatusInstance> Statuses => statusContainer.Statuses;

        private void Awake()
        {
            statisticsController = GetComponent<IStatisticsController>();
            CacheStatusTarget();
            statusContainer = new StatusContainer(statusTarget);
        }

        private void OnEnable()
        {
            SubscribeToDied(GetStatisticsController());
        }

        private void OnDisable()
        {
            UnsubscribeFromDied();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDied();
        }

        private void Update()
        {
            statusContainer.Tick(Time.deltaTime);
        }

        public bool ApplyStatus(StatusDefinition status, float duration, StatusContext context)
        {
            return statusContainer.Add(status, duration, context);
        }

        public bool HasStatus(StatusDefinition status)
        {
            return statusContainer.HasStatus(status);
        }

        public bool TryConsumeStatus(StatusDefinition status)
        {
            return statusContainer.TryConsumeStatus(status);
        }

        public void ClearStatuses()
        {
            EnsureStatusContainer();
            statusContainer.Clear();
        }

        public void ResetForSpawn()
        {
            ClearStatuses();
        }

        public void ResetForDespawn()
        {
            ClearStatuses();
        }

        private void EnsureStatusContainer()
        {
            if (statusContainer != null)
            {
                return;
            }

            CacheStatusTarget();
            statusContainer = new StatusContainer(statusTarget);
        }

        private IStatisticsController GetStatisticsController()
        {
            if (statisticsController == null)
            {
                statisticsController = GetComponent<IStatisticsController>();
                CacheStatusTarget();
                SubscribeToDied(statisticsController);
            }

            return statisticsController;
        }

        private void CacheStatusTarget()
        {
            if (statisticsController == null)
            {
                statisticsController = GetComponent<IStatisticsController>();
            }

            movement ??= GetComponent<IMovement>();
            damageable ??= GetComponent<IDamageable>();
            statusTarget = new StatusTarget(statisticsController, movement, damageable);
        }

        private void SubscribeToDied(IStatisticsController targetStatisticsController)
        {
            if (targetStatisticsController == null || subscribedStatisticsController == targetStatisticsController)
            {
                return;
            }

            UnsubscribeFromDied();
            targetStatisticsController.Died += HandleDied;
            subscribedStatisticsController = targetStatisticsController;
        }

        private void UnsubscribeFromDied()
        {
            if (subscribedStatisticsController == null)
            {
                return;
            }

            subscribedStatisticsController.Died -= HandleDied;
            subscribedStatisticsController = null;
        }

        private void HandleDied()
        {
            ClearStatuses();
        }
    }
}
