using RPGame.Core.Damage;
using UnityEngine;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "Poison", menuName = "RPGame/Progression/Statuses/Poison")]
    public sealed class PoisonStatusDefinition : StatusDefinition, IPeriodicStatus
    {
        private const float MinimumTickInterval = 0.0001f;

        [SerializeField, Min(0f)] private float damagePerTick = 3f;
        [SerializeField, Min(MinimumTickInterval)] private float tickInterval = 1f;

        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Refresh;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.SingleInstance;
        public float DamagePerTick => damagePerTick;
        public float TickInterval => tickInterval;

        public override bool CanApply(StatusTarget target)
        {
            return target.Damageable != null;
        }

        public void Tick(StatusTarget target)
        {
            IDamageable damageable = target.Damageable;
            if (damageable == null || !damageable.CanReceiveDamage || damagePerTick <= 0f)
            {
                return;
            }

            damageable.ApplyDamage(new DamageData(new[]
            {
                new PartialDamage(damagePerTick, DamageType.Magical, DamageElement.None)
            }));
        }

        public override string ToString()
        {
            return $"Poison {damagePerTick:0.##}";
        }

        private void OnValidate()
        {
            damagePerTick = Mathf.Max(0f, damagePerTick);
            tickInterval = Mathf.Max(MinimumTickInterval, tickInterval);
        }
    }
}
