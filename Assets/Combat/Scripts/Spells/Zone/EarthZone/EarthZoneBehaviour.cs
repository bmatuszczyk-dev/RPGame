using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;
using UnityEngine.Serialization;

namespace RPGame.Combat.Spells
{
    public sealed class EarthZoneBehaviour : TriggerStatusZoneBehaviour
    {
        [FormerlySerializedAs("slowEffect")]
        [SerializeField] private SlowStatusDefinition slowStatus;
        [SerializeField] private float slowDuration = 2.5f;
        [SerializeField] private float reapplyInterval = 0.5f;

        protected override float ReapplyInterval => reapplyInterval;

        protected override void ApplyInitialTo(IStatusReceiver target, GameObject targetObject)
        {
            SpellBehaviorPipelineExecutor.Resolve(
                CasterData,
                targetObject,
                new EarthZoneResolveBehavior(this));
        }

        protected override void ReapplyTo(IStatusReceiver target)
        {
            ApplyStatusTo(target);
        }

        private bool ApplyStatusTo(IStatusReceiver target)
        {
            if (slowStatus == null || target == null)
            {
                return false;
            }

            return target.ApplyStatus(
                slowStatus,
                slowDuration,
                new StatusContext(
                    new StatusSourceId(nameof(EarthZoneBehaviour)),
                    StatusSource));
        }

        private void OnValidate()
        {
            slowDuration = Mathf.Max(0f, slowDuration);
            reapplyInterval = Mathf.Max(0f, reapplyInterval);
        }

        private sealed class EarthZoneResolveBehavior : ISpellResolveBehavior
        {
            private readonly EarthZoneBehaviour zone;

            public EarthZoneResolveBehavior(EarthZoneBehaviour zone)
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
