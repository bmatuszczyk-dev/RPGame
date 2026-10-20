using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class ConfigTests
    {
        private readonly List<ScriptableObject> createdAssets = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdAssets.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(createdAssets[i]);
            }

            createdAssets.Clear();
        }

        [Test]
        public void GetAttack_ReturnsConfigByAttackType()
        {
            MeleeAttackConfig meleeConfig = CreateAsset<MeleeAttackConfig>();
            Config config = CreateConfig((AttackType.Melee, meleeConfig));

            MeleeAttackConfig result = config.GetAttack<MeleeAttackConfig>(AttackType.Melee);

            Assert.AreSame(meleeConfig, result);
        }

        [Test]
        public void GetAttack_WhenAttackTypeIsDuplicated_Throws()
        {
            MeleeAttackConfig firstConfig = CreateAsset<MeleeAttackConfig>();
            MeleeAttackConfig secondConfig = CreateAsset<MeleeAttackConfig>();
            Config config = CreateConfig(
                (AttackType.Melee, firstConfig),
                (AttackType.Melee, secondConfig));

            Assert.Throws<InvalidOperationException>(config.ValidateAttacks);
        }

        [Test]
        public void ValidateAttacks_WhenConfigTypeDoesNotMatch_Throws()
        {
            StraightProjectileAttackConfig projectileConfig = CreateAsset<StraightProjectileAttackConfig>();
            Config config = CreateConfig((AttackType.Melee, projectileConfig));

            Assert.Throws<InvalidOperationException>(config.ValidateAttacks);
        }

        [Test]
        public void GetAttack_WhenAttackIsMissing_Throws()
        {
            Config config = CreateConfig();

            Assert.Throws<InvalidOperationException>(() => config.GetAttack<MeleeAttackConfig>(AttackType.Melee));
        }

        [Test]
        public void KnockbackResistance_DefaultsToZero()
        {
            Config config = CreateConfig();

            Assert.AreEqual(0f, config.KnockbackResistance);
        }

        [Test]
        public void AttackConfigs_DoNotContainAttackExecutionLogic()
        {
            Type[] configTypes =
            {
                typeof(AttackConfig),
                typeof(MeleeAttackConfig),
                typeof(RangedAttackConfig),
                typeof(StraightProjectileAttackConfig),
                typeof(ParabolicProjectileAttackConfig)
            };

            foreach (Type configType in configTypes)
            {
                bool containsExecutionMethod = configType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(method => !method.IsSpecialName)
                    .Any(method => method.Name.Contains("TryAttack", StringComparison.Ordinal)
                        || method.Name.Contains("ApplyDamage", StringComparison.Ordinal)
                        || method.Name.Contains("BuildDamage", StringComparison.Ordinal));

                Assert.IsFalse(containsExecutionMethod, configType.Name);
            }
        }

        [Test]
        public void ProjectileAttackConfigs_DoNotContainProjectileMovementData()
        {
            Type[] configTypes =
            {
                typeof(RangedAttackConfig),
                typeof(StraightProjectileAttackConfig),
                typeof(ParabolicProjectileAttackConfig)
            };

            string[] forbiddenFieldNames =
            {
                "projectileSpeed",
                "projectileLifetime",
                "arcHeight",
                "ascentDuration",
                "descentDuration"
            };

            foreach (Type configType in configTypes)
            {
                FieldInfo[] fields = configType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (string forbiddenFieldName in forbiddenFieldNames)
                {
                    Assert.IsFalse(fields.Any(field => field.Name == forbiddenFieldName), configType.Name);
                }
            }
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdAssets.Add(asset);
            return asset;
        }

        private Config CreateConfig(params (AttackType Type, AttackConfig Config)[] attacks)
        {
            Config config = CreateAsset<Config>();
            SerializedObject serializedConfig = new(config);
            SerializedProperty attacksProperty = serializedConfig.FindProperty("attacks");
            attacksProperty.arraySize = attacks.Length;

            for (int i = 0; i < attacks.Length; i++)
            {
                SerializedProperty attackEntry = attacksProperty.GetArrayElementAtIndex(i);
                attackEntry.FindPropertyRelative("type").enumValueIndex = (int)attacks[i].Type;
                attackEntry.FindPropertyRelative("config").objectReferenceValue = attacks[i].Config;
            }

            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }
    }
}
