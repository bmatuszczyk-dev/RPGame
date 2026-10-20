using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Projectiles;
using RPGame.Core.Buildup;
using RPGame.Core.Damage;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace RPGame.Enemies.Tests
{
    public sealed class ProjectileLauncherTests
    {
        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void ProjectileLauncher_DoesNotOwnCooldownOrAttackDecision()
        {
            FieldInfo[] fields = typeof(ProjectileLauncher)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.IsFalse(fields.Any(field => field.Name.ToLowerInvariant().Contains("cooldown")));
            Assert.IsFalse(fields.Any(field => field.FieldType == typeof(Config)));
            Assert.IsFalse(fields.Any(field => field.FieldType == typeof(AttackConfig)));
            Assert.IsFalse(fields.Any(field => field.FieldType == typeof(EnemyStraightProjectile)));
            Assert.IsFalse(fields.Any(field => field.FieldType == typeof(EnemyParabolicProjectile)));
        }

        [Test]
        public void Launch_UsesProjectilePrefabFromLaunchData()
        {
            ProjectileLauncher launcher = CreateLauncher(out Transform spawnPoint);
            EnemyStraightProjectile projectilePrefab = CreateProjectilePrefab("ConfigProjectilePrefab");
            TestDamageable target = CreateTarget();

            bool launched = launcher.Launch(new ProjectileLaunchData(
                projectilePrefab.gameObject,
                new Vector3(0f, 0f, 10f),
                target,
                CreateDamageParts(10f),
                null));

            EnemyStraightProjectile spawnedProjectile = FindSpawnedProjectile(projectilePrefab);
            createdObjects.Add(spawnedProjectile.gameObject);

            Assert.IsTrue(launched);
            Assert.AreEqual(spawnPoint.position, spawnedProjectile.transform.position);
            Assert.AreEqual(6f, spawnedProjectile.CurrentSpeed);
            Assert.IsTrue(spawnedProjectile.IsInitialized);
        }

        [Test]
        public void Launch_WhenPrefabHasNoEnemyProjectile_ReturnsFalse()
        {
            ProjectileLauncher launcher = CreateLauncher(out _);
            GameObject projectilePrefab = CreateObject("InvalidProjectilePrefab");
            LogAssert.Expect(LogType.Error, "Projectile prefab is missing an IEnemyProjectile component.");

            bool launched = launcher.Launch(new ProjectileLaunchData(
                projectilePrefab,
                Vector3.one,
                null,
                CreateDamageParts(10f),
                null));

            Assert.IsFalse(launched);
        }

        [Test]
        public void Launch_InitializesProjectileThroughSharedInterface()
        {
            ProjectileLauncher launcher = CreateLauncher(out _);
            GameObject projectilePrefab = CreateObject("InterfaceProjectilePrefab");
            projectilePrefab.AddComponent<TestEnemyProjectile>();
            TestDamageable target = CreateTarget();
            GameObject source = CreateObject("Source");
            IReadOnlyList<PartialDamage> damageParts = CreateDamageParts(10f);

            bool launched = launcher.Launch(new ProjectileLaunchData(
                projectilePrefab,
                new Vector3(0f, 0f, 10f),
                target,
                damageParts,
                source));

            TestEnemyProjectile spawnedProjectile = UnityEngine.Object
                .FindObjectsByType<TestEnemyProjectile>()
                .First(projectile => projectile.gameObject != projectilePrefab);
            createdObjects.Add(spawnedProjectile.gameObject);

            Assert.IsTrue(launched);
            Assert.AreEqual(new Vector3(0f, 0f, 10f), spawnedProjectile.TargetPosition);
            Assert.AreSame(target, spawnedProjectile.TargetDamageable);
            Assert.AreSame(damageParts, spawnedProjectile.DamageParts);
            Assert.AreSame(source, spawnedProjectile.Source);
        }

        [Test]
        public void Launch_PassesBuildupDataThroughSharedInterface()
        {
            ProjectileLauncher launcher = CreateLauncher(out _);
            GameObject projectilePrefab = CreateObject("BuildupProjectilePrefab");
            projectilePrefab.AddComponent<TestEnemyProjectile>();
            BuildupDefinition buildupDefinition = ScriptableObject.CreateInstance<BuildupDefinition>();

            bool launched = launcher.Launch(new ProjectileLaunchData(
                projectilePrefab,
                Vector3.forward,
                null,
                CreateDamageParts(10f),
                null,
                new BuildupData(buildupDefinition, 25f)));

            TestEnemyProjectile spawnedProjectile = UnityEngine.Object
                .FindObjectsByType<TestEnemyProjectile>()
                .First(projectile => projectile.gameObject != projectilePrefab);
            createdObjects.Add(spawnedProjectile.gameObject);

            Assert.IsTrue(launched);
            Assert.AreSame(buildupDefinition, spawnedProjectile.Buildup.Definition);
            Assert.That(spawnedProjectile.Buildup.Amount, Is.EqualTo(25f));
            UnityEngine.Object.DestroyImmediate(buildupDefinition);
        }

        [Test]
        public void Launch_UsesParabolicProjectilePrefabFromLaunchData()
        {
            ProjectileLauncher launcher = CreateLauncher(out Transform spawnPoint);
            EnemyParabolicProjectile projectilePrefab = CreateParabolicProjectilePrefab("ParabolicProjectilePrefab");
            Vector3 impactPoint = new(0f, 0f, 10f);

            bool launched = launcher.Launch(new ProjectileLaunchData(
                projectilePrefab.gameObject,
                impactPoint,
                null,
                CreateDamageParts(10f),
                null));

            EnemyParabolicProjectile spawnedProjectile = FindSpawnedProjectile(projectilePrefab);
            createdObjects.Add(spawnedProjectile.gameObject);

            Assert.IsTrue(launched);
            Assert.AreEqual(spawnPoint.position, spawnedProjectile.transform.position);
            Assert.AreEqual(impactPoint, spawnedProjectile.ImpactPoint);
            Assert.AreEqual(3f, spawnedProjectile.ApexPoint.y - Vector3.Lerp(spawnPoint.position, impactPoint, 0.5f).y);
            Assert.IsTrue(spawnedProjectile.IsInitialized);
        }

        private ProjectileLauncher CreateLauncher(out Transform spawnPoint)
        {
            GameObject launcherObject = CreateObject("Launcher");
            ProjectileLauncher launcher = launcherObject.AddComponent<ProjectileLauncher>();
            spawnPoint = CreateObject("SpawnPoint").transform;
            spawnPoint.position = new Vector3(1f, 2f, 3f);

            SerializedObject serializedLauncher = new(launcher);
            serializedLauncher.FindProperty("projectileSpawnPoint").objectReferenceValue = spawnPoint;
            serializedLauncher.ApplyModifiedPropertiesWithoutUndo();
            return launcher;
        }

        private EnemyStraightProjectile CreateProjectilePrefab(string objectName)
        {
            GameObject projectileObject = CreateObject(objectName);
            projectileObject.AddComponent<StraightProjectileMover>();
            EnemyStraightProjectile projectile = projectileObject.AddComponent<EnemyStraightProjectile>();
            SerializedObject serializedProjectile = new(projectile);
            serializedProjectile.FindProperty("projectileSpeed").floatValue = 6f;
            serializedProjectile.FindProperty("projectileLifetime").floatValue = 5f;
            serializedProjectile.ApplyModifiedPropertiesWithoutUndo();
            return projectile;
        }

        private EnemyParabolicProjectile CreateParabolicProjectilePrefab(string objectName)
        {
            GameObject projectileObject = CreateObject(objectName);
            projectileObject.AddComponent<ParabolicProjectileMover>();
            EnemyParabolicProjectile projectile = projectileObject.AddComponent<EnemyParabolicProjectile>();
            SerializedObject serializedProjectile = new(projectile);
            serializedProjectile.FindProperty("projectileLifetime").floatValue = 5f;
            serializedProjectile.FindProperty("arcHeight").floatValue = 3f;
            serializedProjectile.FindProperty("ascentDuration").floatValue = 0.75f;
            serializedProjectile.FindProperty("descentDuration").floatValue = 0.5f;
            serializedProjectile.ApplyModifiedPropertiesWithoutUndo();
            return projectile;
        }

        private TestDamageable CreateTarget()
        {
            GameObject targetObject = CreateObject("Target");
            return targetObject.AddComponent<TestDamageable>();
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static EnemyStraightProjectile FindSpawnedProjectile(EnemyStraightProjectile projectilePrefab)
        {
            return UnityEngine.Object
                .FindObjectsByType<EnemyStraightProjectile>()
                .First(projectile => projectile != projectilePrefab);
        }

        private static EnemyParabolicProjectile FindSpawnedProjectile(EnemyParabolicProjectile projectilePrefab)
        {
            return UnityEngine.Object
                .FindObjectsByType<EnemyParabolicProjectile>()
                .First(projectile => projectile != projectilePrefab);
        }

        private static IReadOnlyList<PartialDamage> CreateDamageParts(float amount)
        {
            return new[]
            {
                new PartialDamage(amount, DamageType.Physical, DamageElement.None)
            };
        }

        public sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public bool CanReceiveDamage => true;

            public DamageResult ApplyDamage(DamageData data)
            {
                return DamageResult.Applied(data, data.Amount, 100f, 100f - data.Amount, false);
            }
        }

        private sealed class TestEnemyProjectile : MonoBehaviour, IEnemyProjectile
        {
            public Vector3 TargetPosition { get; private set; }
            public IDamageable TargetDamageable { get; private set; }
            public IReadOnlyList<PartialDamage> DamageParts { get; private set; }
            public GameObject Source { get; private set; }
            public BuildupData Buildup { get; private set; }

            public void Initialize(
                Vector3 targetPosition,
                IDamageable targetDamageable,
                IReadOnlyList<PartialDamage> damageParts,
                GameObject source,
                BuildupData buildup = null)
            {
                TargetPosition = targetPosition;
                TargetDamageable = targetDamageable;
                DamageParts = damageParts;
                Source = source;
                Buildup = buildup;
            }
        }
    }
}
