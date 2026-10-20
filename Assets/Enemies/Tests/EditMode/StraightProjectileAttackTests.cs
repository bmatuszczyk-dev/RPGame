using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Buildup;
using RPGame.Core.Damage;
using RPGame.Core.Targeting;
using UnityEditor;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class StraightProjectileAttackTests
    {
        private readonly List<GameObject> createdObjects = new();
        private readonly List<ScriptableObject> createdAssets = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }

            for (int i = createdAssets.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(createdAssets[i]);
            }

            createdObjects.Clear();
            createdAssets.Clear();
        }

        [Test]
        public void StraightProjectileAttack_ImplementsSharedAttackInterface()
        {
            StraightProjectileAttack attack = CreateAttack(1f, true, out _, out _);

            Assert.IsInstanceOf<IEnemyAttack>(attack);
        }

        [Test]
        public void TryAttack_FirstAttackIsAvailableImmediately()
        {
            StraightProjectileAttack attack = CreateAttack(1f, true, out FakeProjectileLauncher launcher, out _);

            bool attacked = attack.TryAttack(CreateSelectedTarget(Vector3.one));

            Assert.IsTrue(attacked);
            Assert.AreEqual(1, launcher.LaunchCount);
        }

        [Test]
        public void TryAttack_BeforeAttackIntervalExpires_IsBlocked()
        {
            StraightProjectileAttack attack = CreateAttack(1f, true, out FakeProjectileLauncher launcher, out _);
            SelectedTarget target = CreateSelectedTarget(Vector3.one);

            Assert.IsTrue(attack.TryAttack(target));
            Assert.IsFalse(attack.TryAttack(target));
            Assert.AreEqual(1, launcher.LaunchCount);
        }

        [Test]
        public void TryAttack_AfterAttackIntervalExpires_IsAllowedAgain()
        {
            StraightProjectileAttack attack = CreateAttack(1f, true, out FakeProjectileLauncher launcher, out _);
            SelectedTarget target = CreateSelectedTarget(Vector3.one);

            Assert.IsTrue(attack.TryAttack(target));
            attack.Tick(1f);

            Assert.IsTrue(attack.TryAttack(target));
            Assert.AreEqual(2, launcher.LaunchCount);
        }

        [Test]
        public void TryAttack_WhenLineOfSightIsBlocked_ReturnsFalse()
        {
            StraightProjectileAttack attack = CreateAttack(1f, false, out FakeProjectileLauncher launcher, out _);

            bool attacked = attack.TryAttack(CreateSelectedTarget(Vector3.one));

            Assert.IsFalse(attacked);
            Assert.AreEqual(0, launcher.LaunchCount);
        }

        [Test]
        public void TryAttack_BuildsDamageFromPartialDamageRanges()
        {
            StraightProjectileAttack attack = CreateAttack(1f, true, out FakeProjectileLauncher launcher, out _);

            attack.TryAttack(CreateSelectedTarget(Vector3.one));

            Assert.AreEqual(1, launcher.LastLaunchData.DamageParts.Count);
            Assert.AreEqual(10f, launcher.LastLaunchData.DamageParts[0].Amount);
            Assert.AreEqual(DamageType.Physical, launcher.LastLaunchData.DamageParts[0].DamageType);
            Assert.AreEqual(DamageElement.None, launcher.LastLaunchData.DamageParts[0].DamageElement);
        }

        [Test]
        public void TryAttack_HasCooldownIndependentFromMeleeAttack()
        {
            StraightProjectileAttack straightAttack = CreateAttack(1f, true, out _, out _);
            MeleeAttack meleeAttack = new(CreateMeleeConfig(), () => Vector3.zero);
            FakeDamageable damageable = new();
            SelectedTarget target = CreateSelectedTarget(Vector3.one);

            Assert.IsTrue(straightAttack.TryAttack(target));
            Assert.IsFalse(straightAttack.TryAttack(target));

            Assert.IsTrue(meleeAttack.TryAttack(target, damageable, null));
        }

        [Test]
        public void TryAttack_UsesProjectilePrefabFromConfig()
        {
            StraightProjectileAttack attack = CreateAttack(1f, true, out FakeProjectileLauncher launcher, out EnemyStraightProjectile prefab);

            attack.TryAttack(CreateSelectedTarget(Vector3.one));

            Assert.AreSame(prefab.gameObject, launcher.LastLaunchData.ProjectilePrefab);
        }

        [Test]
        public void TryAttack_PassesConfiguredBuildupToLaunchData()
        {
            BuildupDefinition buildupDefinition = ScriptableObject.CreateInstance<BuildupDefinition>();
            createdAssets.Add(buildupDefinition);
            StraightProjectileAttack attack = CreateAttack(
                1f,
                true,
                out FakeProjectileLauncher launcher,
                out _,
                buildupDefinition,
                25f);

            attack.TryAttack(CreateSelectedTarget(Vector3.one));

            Assert.AreSame(buildupDefinition, launcher.LastLaunchData.Buildup.Definition);
            Assert.That(launcher.LastLaunchData.Buildup.Amount, Is.EqualTo(25f));
        }

        [Test]
        public void TryAttack_WithoutBuildupConfig_UsesEmptyBuildupLaunchData()
        {
            StraightProjectileAttack attack = CreateAttack(1f, true, out FakeProjectileLauncher launcher, out _);

            attack.TryAttack(CreateSelectedTarget(Vector3.one));

            Assert.IsNull(launcher.LastLaunchData.Buildup);
        }

        private StraightProjectileAttack CreateAttack(
            float attackInterval,
            bool hasLineOfSight,
            out FakeProjectileLauncher launcher,
            out EnemyStraightProjectile prefab,
            BuildupDefinition buildupDefinition = null,
            float buildupAmount = 0f)
        {
            launcher = new FakeProjectileLauncher();
            FakeDamageable damageable = new();
            prefab = CreateProjectilePrefab();

            return new StraightProjectileAttack(
                CreateStraightConfig(attackInterval, 10f, prefab, buildupDefinition, buildupAmount),
                new FakeLineOfSight(hasLineOfSight),
                launcher,
                _ => damageable,
                () => null);
        }

        private StraightProjectileAttackConfig CreateStraightConfig(
            float attackInterval,
            float damageAmount,
            EnemyStraightProjectile projectilePrefab,
            BuildupDefinition buildupDefinition = null,
            float buildupAmount = 0f)
        {
            StraightProjectileAttackConfig config = ScriptableObject.CreateInstance<StraightProjectileAttackConfig>();
            createdAssets.Add(config);

            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab.gameObject;
            serializedConfig.FindProperty("attackInterval").floatValue = attackInterval;
            serializedConfig.FindProperty("buildupDefinition").objectReferenceValue = buildupDefinition;
            serializedConfig.FindProperty("buildupAmount").floatValue = buildupAmount;
            SetDamage(serializedConfig, damageAmount);
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private MeleeAttackConfig CreateMeleeConfig()
        {
            MeleeAttackConfig config = ScriptableObject.CreateInstance<MeleeAttackConfig>();
            createdAssets.Add(config);

            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("attackInterval").floatValue = 1f;
            serializedConfig.FindProperty("attackRange").floatValue = 2f;
            SetDamage(serializedConfig, 10f);
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private EnemyStraightProjectile CreateProjectilePrefab()
        {
            GameObject projectileObject = new("ProjectilePrefab");
            createdObjects.Add(projectileObject);
            projectileObject.AddComponent<RPGame.Combat.Projectiles.StraightProjectileMover>();
            return projectileObject.AddComponent<EnemyStraightProjectile>();
        }

        private static void SetDamage(SerializedObject serializedConfig, float damageAmount)
        {
            SerializedProperty damageProperty = serializedConfig.FindProperty("damage");
            damageProperty.arraySize = 1;
            SerializedProperty damageEntry = damageProperty.GetArrayElementAtIndex(0);
            damageEntry.FindPropertyRelative("minDamage").floatValue = damageAmount;
            damageEntry.FindPropertyRelative("maxDamage").floatValue = damageAmount;
            damageEntry.FindPropertyRelative("damageType").enumValueIndex = (int)DamageType.Physical;
            damageEntry.FindPropertyRelative("damageElement").enumValueIndex = (int)DamageElement.None;
        }

        private static SelectedTarget CreateSelectedTarget(Vector3 position)
        {
            return new SelectedTarget(new FakeTargetable(), position);
        }

        private sealed class FakeLineOfSight : IEnemyLineOfSight
        {
            private readonly bool hasLineOfSight;

            public FakeLineOfSight(bool hasLineOfSight)
            {
                this.hasLineOfSight = hasLineOfSight;
            }

            public bool HasLineOfSight(Vector3 targetPosition)
            {
                return hasLineOfSight;
            }

            public bool HasLineOfSightFrom(Vector3 origin, Vector3 targetPosition)
            {
                return hasLineOfSight;
            }
        }

        private sealed class FakeProjectileLauncher : IProjectileLauncher
        {
            public int LaunchCount { get; private set; }
            public ProjectileLaunchData LastLaunchData { get; private set; }

            public bool Launch(ProjectileLaunchData data)
            {
                LaunchCount++;
                LastLaunchData = data;
                return true;
            }
        }

        private sealed class FakeDamageable : IDamageable
        {
            public bool CanReceiveDamage => true;

            public DamageResult ApplyDamage(DamageData data)
            {
                return DamageResult.Applied(data, data.Amount, 100f, 100f - data.Amount, false);
            }
        }

        private sealed class FakeTargetable : ITargetable
        {
            public Transform TargetPoint => null;
        }
    }
}
