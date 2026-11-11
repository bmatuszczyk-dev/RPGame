using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "SlowEffect", menuName = "RPGame/Statuses/Slow")]
    [MovedFrom(true, null, null, "SlowEffectDefinition")]
    public sealed class SlowStatusDefinition : StatusDefinition
    {
        [SerializeField] private float movementSpeedMultiplier = 0.7f;

        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Refresh;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.Independent;

        public override bool CanApply(StatusTarget target)
        {
            return target.Movement != null;
        }

        public override void OnApply(StatusTarget target, StatusInstance instance)
        {
            int modifierId = target.Movement.AddMovementSpeedModifier(MovementSpeedMultiplier);
            instance.RegisterCleanup(() => target.Movement.RemoveMovementSpeedModifier(modifierId));
        }

        public override string ToString()
        {
            return $"Slow {MovementSpeedMultiplier:0.##}x";
        }

        private float MovementSpeedMultiplier => Mathf.Max(0f, movementSpeedMultiplier);

        private void OnValidate()
        {
            movementSpeedMultiplier = Mathf.Max(0f, movementSpeedMultiplier);
        }
    }
}
