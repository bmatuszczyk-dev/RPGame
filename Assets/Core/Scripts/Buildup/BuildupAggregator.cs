using RPGame.Core.Pooling;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Core.Buildup
{
    public sealed class BuildupAggregator : MonoBehaviour, IBuildupReceiver, IPooledEnemyResettable
    {
        private BuildupContainer buildupContainer;
        private IStatusReceiver statusReceiver;
        private IStatisticsController statisticsController;
        private IStatisticsController subscribedStatisticsController;

        private void Awake()
        {
            EnsureContainer();
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
            buildupContainer?.Tick(Time.deltaTime);
        }

        public void AddBuildup(BuildupDefinition definition, float amount, StatusContext context)
        {
            EnsureContainer();
            buildupContainer.Add(definition, amount, context);
        }

        public float GetCurrentBuildup(BuildupDefinition definition)
        {
            EnsureContainer();
            return buildupContainer.GetCurrentAmount(definition);
        }

        public void ClearBuildup()
        {
            EnsureContainer();
            buildupContainer.Clear();
        }

        public void ResetForSpawn()
        {
            ClearBuildup();
        }

        public void ResetForDespawn()
        {
            ClearBuildup();
        }

        private void EnsureContainer()
        {
            if (buildupContainer != null)
            {
                return;
            }

            statusReceiver = GetComponent<IStatusReceiver>();
            buildupContainer = new BuildupContainer(statusReceiver);
        }

        private IStatisticsController GetStatisticsController()
        {
            if (statisticsController == null)
            {
                statisticsController = GetComponent<IStatisticsController>();
            }

            return statisticsController;
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
            ClearBuildup();
        }
    }
}
