using RPGame.Core.Damage;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "BurningEffect", menuName = "RPGame/Statuses/Burning")]
    [MovedFrom(true, null, null, "BurningEffectDefinition")]
    public sealed class BurningStatusDefinition :
        StatusDefinition,
        IStackablePeriodicStatus,
        IStackableStatus
    {
        private const float MinimumTickInterval = 0.0001f;

        [FormerlySerializedAs("amountPerInterval")]
        [SerializeField, Min(0f)] private float baseDamagePerSecond = 1.5f;
        [SerializeField] private float tickInterval = 1f;
        [SerializeField, Min(1)] private int maxIntensity = 5;

        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Refresh;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.SingleInstance;
        public float BaseDamagePerSecond => Mathf.Max(0f, baseDamagePerSecond);
        public int MaxStacks => Mathf.Max(1, maxIntensity);
        public int MaxIntensity => MaxStacks;
        public float TickInterval => Mathf.Max(MinimumTickInterval, tickInterval);

        public override bool CanApply(StatusTarget target)
        {
            return target.Damageable != null;
        }

        public void Tick(StatusTarget target, int currentStacks)
        {
            IDamageable damageable = target.Damageable;
            float damage = BaseDamagePerSecond * TickInterval * Mathf.Max(1, currentStacks);
            if (damageable == null || !damageable.CanReceiveDamage || damage <= 0f)
            {
                return;
            }

            damageable.ApplyDamage(new DamageData(new[]
            {
                new PartialDamage(damage, DamageType.Magical, DamageElement.Fire)
            }));
        }

        public override string ToString()
        {
            return $"Burning {BaseDamagePerSecond:0.##}/s x{MaxIntensity}";
        }

        private void OnValidate()
        {
            baseDamagePerSecond = Mathf.Max(0f, baseDamagePerSecond);
            tickInterval = Mathf.Max(MinimumTickInterval, tickInterval);
            maxIntensity = Mathf.Max(1, maxIntensity);
        }
    }
}
