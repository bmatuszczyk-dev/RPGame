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
            if (existingInstance != null)
            {
                ApplyExistingInstance(definition, existingInstance, duration, context);
                return true;
            }

            if (definition.ConcurrentStatusPolicy == ConcurrentStatusPolicy.SingleInstance)
            {
                existingInstance = FindInstance(definition);
                if (existingInstance != null)
                {
                    ApplySingleInstance(definition, existingInstance, duration, context);
                    return true;
                }
            }

            if (!instance.IsFinished)
            {
                statuses.Add(instance);
                instance.Apply(target);
            }

            return true;
        }

        private void ApplyExistingInstance(
            StatusDefinition definition,
            StatusInstance existingInstance,
            float duration,
            StatusContext context)
        {
            ApplyReapplyPolicy(
                context.ReapplyPolicyOverride ?? definition.ReapplyPolicy,
                existingInstance,
                duration);
        }

        private void ApplySingleInstance(
            StatusDefinition definition,
            StatusInstance existingInstance,
            float duration,
            StatusContext context)
        {
            if (context.ReapplyPolicyOverride.HasValue)
            {
                ApplyReapplyPolicy(
                    context.ReapplyPolicyOverride.Value,
                    existingInstance,
                    duration);
                return;
            }

            definition.ApplySingleInstanceReapply(target, existingInstance, duration, context);
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
            float duration)
        {
            switch (reapplyPolicy)
            {
                case ReapplyPolicy.Refresh:
                    existingInstance.Refresh(target, duration);
                    break;
                case ReapplyPolicy.Ignore:
                    break;
                case ReapplyPolicy.Stack:
                    existingInstance.Stack(target, duration);
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
