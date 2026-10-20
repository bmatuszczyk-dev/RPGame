using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Projectiles;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using UnityEditor;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class ProjectileInterceptionTests
    {
        private static readonly MethodInfo PlayerHandleHit = typeof(ProjectileController)
            .GetMethod("HandleHit", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PlayerIsDestroyed = typeof(ProjectileController)
            .GetField("isDestroyed", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void EnemyProjectile_WhenHitPlayerProjectile_DestroysBoth()
        {
            TestPlayerProjectile playerProjectile = CreateTestPlayerProjectile();
            EnemyStraightProjectile enemyProjectile = CreateStraightEnemyProjectile();
            enemyProjectile.Initialize(Vector3.forward, null, CreateDamageParts(), null);

            enemyProjectile.HandleHit(playerProjectile.GetComponent<Collider>());

            Assert.IsTrue(playerProjectile.IsDestroyed);
            Assert.IsTrue(enemyProjectile.IsFinished);
        }

        [Test]
        public void PlayerProjectile_WhenHitPlayerProjectile_KeepsBothActive()
        {
            ProjectileController firstProjectile = CreatePlayerProjectile();
            ProjectileController secondProjectile = CreatePlayerProjectile();

            HandlePlayerHit(firstProjectile, secondProjectile.GetComponent<Collider>());

            Assert.IsFalse(IsPlayerProjectileDestroyed(firstProjectile));
            Assert.IsFalse(IsPlayerProjectileDestroyed(secondProjectile));
        }

        [Test]
        public void EnemyProjectile_WhenHitEnemyProjectile_KeepsBothActive()
        {
            EnemyStraightProjectile firstProjectile = CreateStraightEnemyProjectile();
            EnemyStraightProjectile secondProjectile = CreateStraightEnemyProjectile();
            firstProjectile.Initialize(Vector3.forward, null, CreateDamageParts(), null);
            secondProjectile.Initialize(Vector3.forward, null, CreateDamageParts(), null);

            firstProjectile.HandleHit(secondProjectile.GetComponent<Collider>());

            Assert.IsFalse(firstProjectile.IsFinished);
            Assert.IsFalse(secondProjectile.IsFinished);
        }

        [Test]
        public void EnemyProjectile_WhenSameTeamProjectileIsCloser_InterceptsOpposingProjectileBehindIt()
        {
            EnemyStraightProjectile movingProjectile = CreateStraightEnemyProjectile();
            EnemyStraightProjectile sameTeamProjectile = CreateStraightEnemyProjectile();
            TestPlayerProjectile opposingProjectile = CreateTestPlayerProjectile();
            sameTeamProjectile.transform.position = Vector3.forward * 2f;
            opposingProjectile.transform.position = Vector3.forward * 4f;
            movingProjectile.Initialize(Vector3.forward * 10f, null, CreateDamageParts(), null);
            Physics.SyncTransforms();

            movingProjectile.Tick(0.5f);

            Assert.IsTrue(movingProjectile.IsFinished);
            Assert.IsFalse(sameTeamProjectile.IsFinished);
            Assert.IsTrue(opposingProjectile.IsDestroyed);
        }

        [Test]
        public void EnemyParabolicProjectile_WhenIntercepted_DoesNotApplyImpactAndCleansUpTelegraph()
        {
            TestPlayerProjectile playerProjectile = CreateTestPlayerProjectile();
            playerProjectile.transform.position = Vector3.back * 100f;
            Physics.SyncTransforms();
            EnemyParabolicProjectile enemyProjectile = CreateParabolicEnemyProjectile();
            GameObject telegraphPrefab = CreateObject("TelegraphPrefab");
            ConfigureParabolicProjectile(enemyProjectile, telegraphPrefab);
            enemyProjectile.Initialize(new Vector3(10f, 0f, 0f), null, CreateDamageParts(), null);

            enemyProjectile.Tick(1f);
            Assert.IsNotNull(enemyProjectile.ActiveTelegraph);

            enemyProjectile.HandleHit(playerProjectile.GetComponent<Collider>());

            Assert.IsTrue(playerProjectile.IsDestroyed);
            Assert.IsTrue(enemyProjectile.IsFinished);
            Assert.IsFalse(enemyProjectile.HasImpact);
            Assert.IsNull(enemyProjectile.ActiveTelegraph);
        }

        [Test]
        public void PlayerProjectile_WhenHitDamageable_AppliesDamage()
        {
            ProjectileController playerProjectile = CreatePlayerProjectile(includeMover: true);
            SetDestroyOnHit(playerProjectile, false);
            TestDamageable target = CreateDamageable("Target");
            playerProjectile.Initialize(new CasterData(
                null,
                null,
                null,
                damageRanges: new[] { new PartialDamageRange(5f, 5f, DamageType.Physical, DamageElement.None) }));

            HandlePlayerHit(playerProjectile, target.GetComponent<Collider>());

            Assert.AreEqual(1, target.ApplyDamageCount);
        }

        private ProjectileController CreatePlayerProjectile(bool includeMover = false)
        {
            GameObject projectileObject = CreateObject("PlayerProjectile");
            projectileObject.AddComponent<SphereCollider>();
            if (includeMover)
            {
                projectileObject.AddComponent<StraightProjectileMover>();
            }

            return projectileObject.AddComponent<ProjectileController>();
        }

        private TestPlayerProjectile CreateTestPlayerProjectile()
        {
            GameObject projectileObject = CreateObject("TestPlayerProjectile");
            projectileObject.AddComponent<SphereCollider>();
            return projectileObject.AddComponent<TestPlayerProjectile>();
        }

        private EnemyStraightProjectile CreateStraightEnemyProjectile()
        {
            GameObject projectileObject = CreateObject("EnemyStraightProjectile");
            projectileObject.AddComponent<SphereCollider>();
            projectileObject.AddComponent<StraightProjectileMover>();
            return projectileObject.AddComponent<EnemyStraightProjectile>();
        }

        private EnemyParabolicProjectile CreateParabolicEnemyProjectile()
        {
            GameObject projectileObject = CreateObject("EnemyParabolicProjectile");
            projectileObject.AddComponent<SphereCollider>();
            projectileObject.AddComponent<ParabolicProjectileMover>();
            return projectileObject.AddComponent<EnemyParabolicProjectile>();
        }

        private TestDamageable CreateDamageable(string objectName)
        {
            GameObject targetObject = CreateObject(objectName);
            targetObject.AddComponent<SphereCollider>();
            return targetObject.AddComponent<TestDamageable>();
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void HandlePlayerHit(ProjectileController projectile, Collider collider)
        {
            PlayerHandleHit.Invoke(projectile, new object[] { collider });
        }

        private static bool IsPlayerProjectileDestroyed(ProjectileController projectile)
        {
            return (bool)PlayerIsDestroyed.GetValue(projectile);
        }

        private static IReadOnlyList<PartialDamage> CreateDamageParts()
        {
            return new[]
            {
                new PartialDamage(5f, DamageType.Physical, DamageElement.None)
            };
        }

        private static void ConfigureParabolicProjectile(
            EnemyParabolicProjectile projectile,
            GameObject telegraphPrefab)
        {
            SerializedObject serializedProjectile = new(projectile);
            serializedProjectile.FindProperty("telegraphPrefab").objectReferenceValue = telegraphPrefab;
            serializedProjectile.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetDestroyOnHit(ProjectileController projectile, bool destroyOnHit)
        {
            SerializedObject serializedProjectile = new(projectile);
            serializedProjectile.FindProperty("destroyOnHit").boolValue = destroyOnHit;
            serializedProjectile.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public int ApplyDamageCount { get; private set; }
            public bool CanReceiveDamage => true;

            public DamageResult ApplyDamage(DamageData data)
            {
                ApplyDamageCount++;
                return DamageResult.Applied(data, data.Amount, 100f, 100f - data.Amount, false);
            }
        }

        private sealed class TestPlayerProjectile : MonoBehaviour, IProjectileDestructible
        {
            public bool IsDestroyed { get; private set; }
            public ProjectileTeam Team => ProjectileTeam.Player;

            public void DestroyProjectile()
            {
                IsDestroyed = true;
            }
        }
    }
}
