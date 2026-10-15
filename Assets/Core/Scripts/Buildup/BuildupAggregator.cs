using System;
using System.Collections.Generic;
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

        public event Action<BuildupDefinition, float, float> BuildupChanged;
        public event Action BuildupsCleared;

        public IReadOnlyList<BuildupSnapshot> ActiveBuildups
        {
            get
            {
                EnsureContainer();
                return buildupContainer.ActiveBuildups;
            }
        }

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
            if (buildupContainer != null)
            {
                buildupContainer.BuildupChanged -= HandleBuildupChanged;
                buildupContainer.BuildupsCleared -= HandleBuildupsCleared;
            }
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
            buildupContainer.BuildupChanged += HandleBuildupChanged;
            buildupContainer.BuildupsCleared += HandleBuildupsCleared;
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

        private void HandleBuildupChanged(BuildupDefinition definition, float currentAmount, float threshold)
        {
            BuildupChanged?.Invoke(definition, currentAmount, threshold);
        }

        private void HandleBuildupsCleared()
        {
            BuildupsCleared?.Invoke();
        }
    }
}
