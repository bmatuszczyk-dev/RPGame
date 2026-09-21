using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;
using UnityEngine.Serialization;

namespace RPGame.Combat.Spells
{
    public sealed class FireZoneBehaviour : TriggerStatusZoneBehaviour
    {
        [FormerlySerializedAs("burningEffect")]
        [SerializeField] private BurningStatusDefinition burningStatus;
        [SerializeField] private float burningDuration = 3f;
        [SerializeField] private float reapplyInterval = 0.5f;

        protected override float ReapplyInterval => reapplyInterval;

        protected override void ApplyInitialTo(IStatusReceiver target, GameObject targetObject)
        {
            SpellBehaviorPipelineExecutor.Resolve(
                CasterData,
                targetObject,
                new FireZoneResolveBehavior(this));
        }

        protected override void ReapplyTo(IStatusReceiver target)
        {
            ApplyStatusTo(target);
        }

        private bool ApplyStatusTo(IStatusReceiver target)
        {
            if (burningStatus == null || target == null)
            {
                return false;
            }

            return target.ApplyStatus(
                burningStatus,
                burningDuration,
                new StatusContext(
                    new StatusSourceId(nameof(FireZoneBehaviour)),
                    StatusSource));
        }

        private void OnValidate()
        {
            burningDuration = Mathf.Max(0f, burningDuration);
            reapplyInterval = Mathf.Max(0f, reapplyInterval);
        }

        private sealed class FireZoneResolveBehavior : ISpellResolveBehavior
        {
            private readonly FireZoneBehaviour zone;

            public FireZoneResolveBehavior(FireZoneBehaviour zone)
            {
                this.zone = zone;
            }

            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;

            public bool Resolve(SpellBehaviorContext context)
            {
                return zone.ApplyStatusTo(context.StatusReceiver);
            }
        }
    }
}
