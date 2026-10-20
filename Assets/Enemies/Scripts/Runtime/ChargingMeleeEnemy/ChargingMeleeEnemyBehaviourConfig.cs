using System.Collections.Generic;
using RPGame.Core.Damage;
using UnityEngine;

namespace RPGame.Enemies
{
    [CreateAssetMenu(
        fileName = "ChargingMeleeEnemyBehaviourConfig",
        menuName = "RPGame/Enemies/Charging Melee Enemy Behaviour Config")]
    public sealed class ChargingMeleeEnemyBehaviourConfig : EnemyBehaviourConfigBase
    {
        [SerializeField] private float minTriggerDistance = 3f;
        [SerializeField] private float maxTriggerDistance = 7f;
        [SerializeField, Range(0f, 1f)] private float triggerChance = 0.35f;
        [SerializeField] private float triggerCheckInterval = 0.5f;
        [SerializeField] private float windupDuration = 0.5f;
        [SerializeField] private float chargeSpeed = 8f;
        [SerializeField] private float maxDistance = 6f;
        [SerializeField] private float recoveryDuration = 0.5f;
        [SerializeField] private float cooldown = 3f;
        [SerializeField, Range(0f, 1f)] private float knockbackResistance = 0.5f;
        [SerializeField] private List<PartialDamageRange> chargeDamage = new();

        public float MinTriggerDistance => minTriggerDistance;
        public float MaxTriggerDistance => maxTriggerDistance;
        public float TriggerChance => triggerChance;
        public float TriggerCheckInterval => triggerCheckInterval;
        public float WindupDuration => windupDuration;
        public float ChargeSpeed => chargeSpeed;
        public float MaxDistance => maxDistance;
        public float RecoveryDuration => recoveryDuration;
        public float Cooldown => cooldown;
        public float KnockbackResistance => knockbackResistance;
        public IReadOnlyList<PartialDamageRange> ChargeDamage => chargeDamage;

        private void OnValidate()
        {
            minTriggerDistance = Mathf.Max(0f, minTriggerDistance);
            maxTriggerDistance = Mathf.Max(minTriggerDistance, maxTriggerDistance);
            triggerChance = Mathf.Clamp01(triggerChance);
            triggerCheckInterval = Mathf.Max(0.01f, triggerCheckInterval);
            windupDuration = Mathf.Max(0f, windupDuration);
            chargeSpeed = Mathf.Max(0.01f, chargeSpeed);
            maxDistance = Mathf.Max(0.01f, maxDistance);
            recoveryDuration = Mathf.Max(0f, recoveryDuration);
            cooldown = Mathf.Max(0f, cooldown);
            knockbackResistance = Mathf.Clamp01(knockbackResistance);
        }
    }
}
