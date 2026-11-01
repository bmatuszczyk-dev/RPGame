using System;
using System.Collections.Generic;

namespace RPGame.Core.Statuses
{
    public sealed class StatusContainer
    {
        private readonly List<StatusInstance> statuses = new();
        private readonly StatusTarget target;
        private bool isTicking;
        private bool isClearing;
        private bool clearRequested;

        public IReadOnlyList<StatusInstance> Statuses => statuses;
        public event Action<StatusInstance> StatusAdded;
        public event Action<StatusInstance> StatusUpdated;
        public event Action<StatusInstance> StatusRemoved;

        public StatusContainer(StatusTarget target)
        {
            this.target = target;
        }

        public bool Add(StatusDefinition definition, float duration, StatusContext context)
        {
            if (definition == null || !definition.CanApply(target))
            {
                return false;
            }

            StatusInstance instance = new StatusInstance(definition, duration, context);
            if (instance.IsInstant)
            {
                instance.ApplyInstant(target);
                return true;
            }

            StatusInstance existingInstance = FindMatchingInstance(definition, context);
            bool hasMatchingIdentity = existingInstance != null;
            if (existingInstance == null
                && definition.ConcurrentStatusPolicy == ConcurrentStatusPolicy.SingleInstance)
            {
                existingInstance = FindInstance(definition);
            }

            if (existingInstance != null)
            {
                ReapplyExisting(definition, existingInstance, duration, context, hasMatchingIdentity);
                StatusUpdated?.Invoke(existingInstance);
                return true;
            }

            if (!instance.IsFinished)
            {
                statuses.Add(instance);
                instance.Apply(target);
                StatusAdded?.Invoke(instance);
            }

            return true;
        }

        private void ReapplyExisting(
            StatusDefinition definition,
            StatusInstance existingInstance,
            float duration,
            StatusContext context,
            bool hasMatchingIdentity)
        {
            if (!hasMatchingIdentity
                && definition.ConcurrentStatusPolicy == ConcurrentStatusPolicy.SingleInstance
                && !context.ReapplyPolicyOverride.HasValue
                && definition.UsesCustomSingleInstanceReapply)
            {
                definition.ApplySingleInstanceReapply(target, existingInstance, duration, context);
                return;
            }

            ReapplyPolicy reapplyPolicy =
                context.ReapplyPolicyOverride ?? definition.ReapplyPolicy;
            if (reapplyPolicy != ReapplyPolicy.Ignore)
            {
                existingInstance.ApplyValueOverride(context);
            }

            ApplyReapplyPolicy(reapplyPolicy, existingInstance, duration, context);
        }

        public bool HasStatus(StatusDefinition definition)
        {
            return FindInstance(definition) != null;
        }

        public bool TryConsumeStatus(StatusDefinition definition)
        {
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                StatusInstance status = statuses[i];
                if (!status.IsFinished && status.HasDefinition(definition))
                {
                    status.Remove(target, StatusLifecycleEvent.Consumed);
                    StatusRemoved?.Invoke(status);
                    statuses.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public int CountStatusInstances(StatusDefinition definition)
        {
            int count = 0;
            foreach (StatusInstance status in statuses)
            {
                if (!status.IsFinished && status.HasDefinition(definition))
                {
                    count++;
                }
            }

            return count;
        }

        public StatusInstance GetFirstStatus(StatusDefinition definition)
        {
            return FindInstance(definition);
        }

        public void Tick(float deltaTime)
        {
            isTicking = true;

            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                StatusInstance status = statuses[i];
                status.Tick(deltaTime, target);

                if (clearRequested)
                {
                    break;
                }

                if (status.IsFinished)
                {
                    status.Remove(target, StatusLifecycleEvent.Expired);
                    StatusRemoved?.Invoke(status);
                    statuses.RemoveAt(i);
                }
            }

            isTicking = false;

            if (clearRequested)
            {
                clearRequested = false;
                ClearImmediately();
            }
        }

        public void Clear()
        {
            if (isTicking || isClearing)
            {
                clearRequested = true;
                return;
            }

            ClearImmediately();
        }

        private void ApplyReapplyPolicy(
            ReapplyPolicy reapplyPolicy,
            StatusInstance existingInstance,
            float duration,
            StatusContext context)
        {
            switch (reapplyPolicy)
            {
                case ReapplyPolicy.Refresh:
                    existingInstance.Refresh(target, duration, context);
                    break;
                case ReapplyPolicy.Ignore:
                    break;
                case ReapplyPolicy.Accumulate:
                    existingInstance.Accumulate(target, duration, context);
                    break;
                case ReapplyPolicy.StackAndRefresh:
                    existingInstance.StackAndRefresh(target, duration, context);
                    break;
            }
        }

        private void ClearImmediately()
        {
            if (isClearing)
            {
                clearRequested = true;
                return;
            }

            isClearing = true;
            clearRequested = false;

            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                statuses[i].Remove(target, StatusLifecycleEvent.Cleared);
                StatusRemoved?.Invoke(statuses[i]);
            }

            statuses.Clear();
            clearRequested = false;
            isClearing = false;
        }

        private StatusInstance FindInstance(StatusDefinition definition)
        {
            foreach (StatusInstance status in statuses)
            {
                if (!status.IsFinished && status.HasDefinition(definition))
                {
                    return status;
                }
            }

            return null;
        }

        private StatusInstance FindMatchingInstance(StatusDefinition definition, StatusContext context)
        {
            foreach (StatusInstance status in statuses)
            {
                if (!status.IsFinished && status.HasSameIdentity(definition, context))
                {
                    return status;
                }
            }

            return null;
        }
    }
}
