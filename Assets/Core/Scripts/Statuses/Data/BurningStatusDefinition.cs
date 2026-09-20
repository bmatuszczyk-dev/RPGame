using RPGame.Core.Damage;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "BurningEffect", menuName = "RPGame/Statuses/Burning")]
    [MovedFrom(true, null, null, "BurningEffectDefinition")]
    public sealed class BurningStatusDefinition : StatusDefinition, IPeriodicStatus
    {
        private const float MinimumTickInterval = 0.0001f;

        [SerializeField] private float amountPerInterval = 1.5f;
        [SerializeField] private float tickInterval = 1f;

        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Refresh;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.Independent;
        public float Amount => Mathf.Max(0f, amountPerInterval);
        public float TickInterval => Mathf.Max(MinimumTickInterval, tickInterval);

        public override bool CanApply(StatusTarget target)
        {
            return target.Damageable != null;
        }

        public void Tick(StatusTarget target)
        {
            IDamageable damageable = target.Damageable;
            if (damageable == null || !damageable.CanReceiveDamage || Amount <= 0f)
            {
                return;
            }

            damageable.ApplyDamage(new DamageData(new[]
            {
                new PartialDamage(Amount, DamageType.Magical, DamageElement.Fire)
            }));
        }

        public override string ToString()
        {
            return $"Burning {Amount:0.##}";
        }

        private void OnValidate()
        {
            amountPerInterval = Mathf.Max(0f, amountPerInterval);
            tickInterval = Mathf.Max(MinimumTickInterval, tickInterval);
        }
    }
}
