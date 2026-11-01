using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Core.Statuses
{
    [MovedFrom(true, null, null, "StatusEffectDefinition")]
    public abstract class StatusDefinition : ScriptableObject
    {
        public virtual ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Refresh;
        public virtual ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.SingleInstance;
        public virtual bool UsesCustomSingleInstanceReapply => false;

        public virtual bool CanApply(StatusTarget target)
        {
            return true;
        }

        public virtual void OnApply(StatusTarget target, StatusInstance instance)
        {
        }

        public virtual void OnRefresh(
            StatusTarget target,
            StatusInstance instance,
            StatusContext context)
        {
        }

        public virtual void Tick(StatusTarget target, float deltaTime)
        {
        }

        public virtual void OnRemove(
            StatusTarget target,
            StatusInstance instance,
            StatusLifecycleEvent lifecycleEvent)
        {
        }

        public virtual bool IsFinished(StatusTarget target)
        {
            return false;
        }

        public virtual void ApplySingleInstanceReapply(
            StatusTarget target,
            StatusInstance existingInstance,
            float incomingDuration,
            StatusContext incomingContext)
        {
            existingInstance.Refresh(target, incomingDuration, incomingContext);
        }

        public abstract override string ToString();
    }
}
