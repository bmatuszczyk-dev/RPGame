using System.Collections.Generic;
using RPGame.Core.Damage;
using RPGame.Core.Effects;
using RPGame.Core.Movement;
using RPGame.Core.Pooling;
using RPGame.Core.Statistics;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Core.Statuses
{
    [MovedFrom(true, null, null, "StatusEffectAggregator")]
    public sealed class StatusAggregator : MonoBehaviour, IStatusReceiver, IStatusReader, IPooledEnemyResettable
    {
        private StatusContainer statusContainer;
        private IStatisticsController statisticsController;
        private IMovement movement;
        private IDamageable damageable;
        private StatusTarget statusTarget;
        private IStatisticsController subscribedStatisticsController;
        private readonly Dictionary<StatusInstance, StatusModifierEntry> statusModifiers = new();
        private readonly Dictionary<EffectStat, float> flatModifierValues = new();
        private readonly Dictionary<EffectStat, float> percentModifierValues = new();

        public IReadOnlyList<StatusInstance> Statuses => statusContainer.Statuses;

        private void Awake()
        {
            statisticsController = GetComponent<IStatisticsController>();
            CacheStatusTarget();
            statusContainer = new StatusContainer(statusTarget);
            SubscribeToStatusChanges();
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

        public float GetEffectValue(EffectStat stat, EffectModifierType modifierType)
        {
            Dictionary<EffectStat, float> values = modifierType == EffectModifierType.Flat
                ? flatModifierValues
                : percentModifierValues;
            return values.TryGetValue(stat, out float value) ? value : 0f;
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
            SubscribeToStatusChanges();
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

        private void SubscribeToStatusChanges()
        {
            statusContainer.StatusAdded += AddStatusModifier;
            statusContainer.StatusUpdated += UpdateStatusModifier;
            statusContainer.StatusRemoved += RemoveStatusModifier;
        }

        private void AddStatusModifier(StatusInstance instance)
        {
            if (instance?.Definition is not IStatusStatModifier modifier)
            {
                return;
            }

            float value = modifier.GetValue(instance);
            StatusModifierEntry entry = new(modifier.Stat, modifier.ModifierType, value);
            statusModifiers[instance] = entry;
            AddModifierValue(entry, 1f);
        }

        private void UpdateStatusModifier(StatusInstance instance)
        {
            RemoveStatusModifier(instance);
            AddStatusModifier(instance);
        }

        private void RemoveStatusModifier(StatusInstance instance)
        {
            if (!statusModifiers.TryGetValue(instance, out StatusModifierEntry entry))
            {
                return;
            }

            statusModifiers.Remove(instance);
            AddModifierValue(entry, -1f);
        }

        private void AddModifierValue(StatusModifierEntry entry, float sign)
        {
            Dictionary<EffectStat, float> values = entry.ModifierType == EffectModifierType.Flat
                ? flatModifierValues
                : percentModifierValues;
            values.TryGetValue(entry.Stat, out float currentValue);
            values[entry.Stat] = currentValue + entry.Value * sign;
        }

        private readonly struct StatusModifierEntry
        {
            public StatusModifierEntry(
                EffectStat stat,
                EffectModifierType modifierType,
                float value)
            {
                Stat = stat;
                ModifierType = modifierType;
                Value = value;
            }

            public EffectStat Stat { get; }
            public EffectModifierType ModifierType { get; }
            public float Value { get; }
        }
    }
}
