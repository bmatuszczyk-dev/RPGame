using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "StunEffect", menuName = "RPGame/Statuses/Stun")]
    [MovedFrom(true, null, null, "StunEffectDefinition")]
    public sealed class StunStatusDefinition : StatusDefinition
    {
        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Ignore;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.SingleInstance;

        public override bool CanApply(StatusTarget target)
        {
            return target.Movement != null;
        }

        public override void OnApply(StatusTarget target, StatusInstance instance)
        {
            target.Movement.BlockMovement();
            instance.RegisterCleanup(target.Movement.UnblockMovement);
        }

        public override void ApplySingleInstanceReapply(
            StatusTarget target,
            StatusInstance existingInstance,
            float incomingDuration,
            StatusContext incomingContext)
        {
            existingInstance.KeepLongerDuration(target, incomingDuration);
        }

        public override string ToString()
        {
            return "Stun";
        }
    }
}
