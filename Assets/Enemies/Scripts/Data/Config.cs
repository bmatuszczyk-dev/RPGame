using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGame.Enemies
{
    [CreateAssetMenu(fileName = "Config", menuName = "RPGame/Enemies/Config")]
    public sealed class Config : ScriptableObject
    {
        [SerializeField] private EnemyBehaviourConfigBase behaviourConfig;
        [SerializeField] private List<AttackEntry> attacks = new();
        [SerializeField, Min(0)] private int experienceReward;
        [SerializeField, Range(0f, 1f)] private float knockbackResistance;

        private Dictionary<AttackType, AttackConfig> attackLookup;

        public EnemyBehaviourConfigBase BehaviourConfig => behaviourConfig;
        public int ExperienceReward => experienceReward;
        public float KnockbackResistance => knockbackResistance;

        public T GetAttack<T>(AttackType type) where T : AttackConfig
        {
            EnsureAttackLookup();

            if (!attackLookup.TryGetValue(type, out AttackConfig config) || config == null)
            {
                throw new InvalidOperationException($"Missing attack config for '{type}'.");
            }

            if (config is not T typedConfig)
            {
                throw new InvalidOperationException(
                    $"Attack config for '{type}' is '{config.GetType().Name}', expected '{typeof(T).Name}'.");
            }

            return typedConfig;
        }

        internal void ValidateAttacks()
        {
            HashSet<AttackType> registeredTypes = new();
            if (attacks == null)
            {
                return;
            }

            for (int i = 0; i < attacks.Count; i++)
            {
                AttackEntry entry = attacks[i];
                if (entry == null)
                {
                    continue;
                }

                if (!registeredTypes.Add(entry.Type))
                {
                    throw new InvalidOperationException($"Duplicate attack config for '{entry.Type}'.");
                }

                if (entry.Config != null && !IsConfigMatchingType(entry.Type, entry.Config))
                {
                    throw new InvalidOperationException(
                        $"Attack '{entry.Type}' cannot use config '{entry.Config.GetType().Name}'.");
                }
            }
        }

        private static bool IsConfigMatchingType(AttackType type, AttackConfig config)
        {
            return type switch
            {
                AttackType.Melee => config is MeleeAttackConfig,
                AttackType.StraightProjectile => config is StraightProjectileAttackConfig,
                AttackType.ParabolicProjectile => config is ParabolicProjectileAttackConfig,
                AttackType.AssassinSlash => config is AssassinSlashAttackConfig,
                _ => false
            };
        }

        private void OnValidate()
        {
            attackLookup = null;
            knockbackResistance = Mathf.Clamp01(knockbackResistance);
        }

        private void EnsureAttackLookup()
        {
            if (attackLookup != null)
            {
                return;
            }

            ValidateAttacks();
            attackLookup = new Dictionary<AttackType, AttackConfig>();
            if (attacks == null)
            {
                return;
            }

            for (int i = 0; i < attacks.Count; i++)
            {
                AttackEntry entry = attacks[i];
                if (entry != null)
                {
                    attackLookup.Add(entry.Type, entry.Config);
                }
            }
        }
    }
}
