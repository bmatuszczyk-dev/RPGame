using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGame.Core.Statuses
{
    [Serializable]
    public sealed class StatusInstance
    {
        [SerializeField] private StatusDefinition definition;
        [SerializeField] private string sourceId;
        [SerializeField] private GameObject source;
        [SerializeField] private float duration;
        [SerializeField] private float remainingDuration;
        [SerializeField] private float value;
        [SerializeField] private float remainingAmount;
        [SerializeField] private float periodicTickTimer;
        private readonly List<Action> cleanupActions = new();
        private bool wasApplied;
        private bool wasRemoved;

        public StatusInstance(StatusDefinition definition, float duration, StatusContext context)
        {
            this.definition = definition;
            SourceId = context.SourceId;
            source = context.Source;
            this.duration = Mathf.Max(0f, duration);
            remainingDuration = this.duration;
            remainingAmount = GetAmount(definition);
        }

        public StatusDefinition Definition => definition;

        public StatusSourceId SourceId
        {
            get => new(sourceId);
            private set => sourceId = value.ToString();
        }

        public GameObject Source => source;
        public float Duration => duration;
        public float RemainingDuration => remainingDuration;
        public float Value => value;
        public float RemainingAmount => remainingAmount;
        public StatusLifecycleEvent LastLifecycleEvent { get; private set; }
        public bool IsFinished => remainingDuration <= 0f;
        public bool IsInstant => duration <= 0f;

        public void RegisterCleanup(Action cleanup)
        {
            if (cleanup != null)
            {
                cleanupActions.Add(cleanup);
            }
        }

        public void SetValue(float value)
        {
            this.value = Mathf.Max(0f, value);
        }

        public bool HasSameIdentity(StatusDefinition definition, StatusContext context)
        {
            return HasDefinition(definition)
                && SourceId == context.SourceId
                && source == context.Source;
        }

        public bool HasDefinition(StatusDefinition definition)
        {
            return this.definition != null
                && definition != null
                && ReferenceEquals(this.definition, definition);
        }

        public void Refresh(StatusTarget target, float duration)
        {
            this.duration = Mathf.Max(0f, duration);
            remainingDuration = this.duration;
            remainingAmount = GetAmount(definition);
            LastLifecycleEvent = StatusLifecycleEvent.Refreshed;
            definition?.OnRefresh(target, this);
        }

        public void Stack(StatusTarget target, float incomingDuration)
        {
            float stackedDuration = Mathf.Max(0f, incomingDuration);
            duration += stackedDuration;
            remainingDuration += stackedDuration;
            remainingAmount += GetAmount(definition);
            LastLifecycleEvent = StatusLifecycleEvent.Refreshed;
            definition?.OnRefresh(target, this);
        }

        public void KeepLongerDuration(StatusTarget target, float incomingDuration)
        {
            float refreshedDuration = Mathf.Max(0f, incomingDuration);
            if (refreshedDuration <= remainingDuration)
            {
                return;
            }

            duration = refreshedDuration;
            remainingDuration = refreshedDuration;
            remainingAmount = GetAmount(definition);
            LastLifecycleEvent = StatusLifecycleEvent.Refreshed;
            definition?.OnRefresh(target, this);
        }

        public void Apply(StatusTarget target)
        {
            if (wasApplied || definition == null)
            {
                return;
            }

            wasApplied = true;
            LastLifecycleEvent = StatusLifecycleEvent.Applied;
            definition.OnApply(target, this);
        }

        public void ApplyInstant(StatusTarget target)
        {
            Apply(target);
            definition?.Tick(target, 0f);
            TickAmountStatus(target, 0f, remainingAmount);
            TickPeriodicStatus(target, 0f);
            remainingAmount = 0f;
            remainingDuration = 0f;
            Remove(target, StatusLifecycleEvent.Expired);
        }

        public void Tick(float deltaTime, StatusTarget target)
        {
            if (IsFinished)
            {
                return;
            }

            if (definition == null || definition.IsFinished(target))
            {
                remainingDuration = 0f;
                return;
            }

            float previousRemainingDuration = remainingDuration;
            remainingDuration = Mathf.Max(0f, remainingDuration - Mathf.Max(0f, deltaTime));

            float elapsedDelta = previousRemainingDuration - remainingDuration;
            float amount = previousRemainingDuration > 0f
                ? remainingAmount * (elapsedDelta / previousRemainingDuration)
                : remainingAmount;

            definition.Tick(target, elapsedDelta);
            TickAmountStatus(target, elapsedDelta, amount);
            TickPeriodicStatus(target, elapsedDelta);
            remainingAmount = Mathf.Max(0f, remainingAmount - amount);

            if (remainingDuration <= 0f || definition.IsFinished(target))
            {
                remainingDuration = 0f;
            }
        }

        public void Remove(StatusTarget target, StatusLifecycleEvent lifecycleEvent)
        {
            if (wasRemoved || definition == null)
            {
                return;
            }

            wasRemoved = true;
            LastLifecycleEvent = lifecycleEvent;
            definition.OnRemove(target, this, lifecycleEvent);
            RunCleanupActions();
        }

        private void TickAmountStatus(StatusTarget target, float deltaTime, float amount)
        {
            if (definition is IAmountStatus amountStatus)
            {
                amountStatus.Tick(target, deltaTime, amount);
            }
        }

        private void TickPeriodicStatus(StatusTarget target, float deltaTime)
        {
            if (definition is not IPeriodicStatus periodicStatus || deltaTime <= 0f)
            {
                return;
            }

            float tickInterval = periodicStatus.TickInterval;
            if (tickInterval <= 0f)
            {
                return;
            }

            periodicTickTimer += deltaTime;
            while (periodicTickTimer >= tickInterval)
            {
                periodicStatus.Tick(target);
                periodicTickTimer -= tickInterval;
            }
        }

        private static float GetAmount(StatusDefinition definition)
        {
            if (definition is not IAmountStatus amountStatus)
            {
                return 0f;
            }

            return Mathf.Max(0f, amountStatus.Amount);
        }

        private void RunCleanupActions()
        {
            for (int i = 0; i < cleanupActions.Count; i++)
            {
                cleanupActions[i]?.Invoke();
            }

            cleanupActions.Clear();
        }
    }
}
