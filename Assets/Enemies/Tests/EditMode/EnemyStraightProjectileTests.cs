using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Projectiles;
using RPGame.Core.Buildup;
using RPGame.Core.Damage;
using UnityEditor;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class EnemyStraightProjectileTests
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
        public void EnemyStraightProjectile_RequiresStraightProjectileMover()
        {
            bool requiresMover = typeof(EnemyStraightProjectile)
                .GetCustomAttributes<RequireComponent>()
                .Any(attribute =>
                    attribute.m_Type0 == typeof(StraightProjectileMover)
                    || attribute.m_Type1 == typeof(StraightProjectileMover)
                    || attribute.m_Type2 == typeof(StraightProjectileMover));

            Assert.IsTrue(requiresMover);
        }

        [Test]
        public void EnemyStraightProjectile_ImplementsSharedProjectileInterface()
        {
            Assert.IsInstanceOf<IEnemyProjectile>(CreateProjectile(Vector3.zero, Quaternion.identity));
        }

        [Test]
        public void Tick_MovesWithConfiguredSpeed()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            ConfigureProjectile(projectile, 4f, 5f);
            TestDamageable target = CreateDamageableTarget("Target", new Vector3(10f, 0f, 0f));
            projectile.Initialize(target.transform.position, target, CreateDamageParts(10f), null);

            projectile.Tick(0.5f);

            Assert.AreEqual(new Vector3(0f, 0f, 2f), projectile.transform.position);
        }

        [Test]
        public void Tick_WhenLifetimeExpires_FinishesProjectile()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            ConfigureProjectile(projectile, 4f, 0.5f);
            TestDamageable target = CreateDamageableTarget("Target", Vector3.one);
            projectile.Initialize(target.transform.position, target, CreateDamageParts(10f), null);

            projectile.Tick(0.5f);

            Assert.IsTrue(projectile.IsFinished);
        }

        [Test]
        public void Tick_UsesSweepCollisionBetweenFrames()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            ConfigureProjectile(projectile, 10f, 5f);
            TestDamageable target = CreateDamageableTarget("Target", new Vector3(0f, 0f, 10f));
            CreateEnvironment("Wall", new Vector3(0f, 0f, 2.5f));
            projectile.Initialize(target.transform.position, target, CreateDamageParts(10f), null);
            Physics.SyncTransforms();

            projectile.Tick(0.5f);

            Assert.IsTrue(projectile.IsFinished);
            Assert.AreEqual(0, target.ApplyDamageCount);
        }

        [Test]
        public void HandleHit_WhenHitIsTarget_AppliesDamageAndFinishesProjectile()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            TestDamageable target = CreateDamageableTarget("Target", Vector3.one);
            projectile.Initialize(target.transform.position, target, CreateDamageParts(10f), null);

            projectile.HandleHit(target.GetComponent<Collider>());

            Assert.IsTrue(projectile.IsFinished);
            Assert.AreEqual(1, target.ApplyDamageCount);
            Assert.AreEqual(10f, target.LastDamageData.Amount);
        }

        [Test]
        public void HandleHit_WhenBuildupIsConfigured_AddsBuildupAfterDamage()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            TestDamageable target = CreateDamageableTarget("Target", Vector3.one);
            TestBuildupReceiver receiver = target.gameObject.AddComponent<TestBuildupReceiver>();
            BuildupDefinition definition = ScriptableObject.CreateInstance<BuildupDefinition>();
            GameObject source = CreateObject("Source");

            projectile.Initialize(
                target.transform.position,
                target,
                CreateDamageParts(10f),
                source,
                new BuildupData(definition, 30f));
            projectile.HandleHit(target.GetComponent<Collider>());

            Assert.AreEqual(1, target.ApplyDamageCount);
            Assert.AreEqual(1, receiver.AddCount);
            Assert.AreSame(definition, receiver.Definition);
            Assert.That(receiver.Amount, Is.EqualTo(30f));
            Assert.AreSame(source, receiver.Context.Source);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void HandleHit_WhenHitIsNotTarget_DoesNotAddBuildup()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            TestDamageable target = CreateDamageableTarget("Target", Vector3.one);
            TestDamageable other = CreateDamageableTarget("Other", Vector3.one);
            TestBuildupReceiver receiver = other.gameObject.AddComponent<TestBuildupReceiver>();
            BuildupDefinition definition = ScriptableObject.CreateInstance<BuildupDefinition>();

            projectile.Initialize(
                target.transform.position,
                target,
                CreateDamageParts(10f),
                null,
                new BuildupData(definition, 30f));
            projectile.HandleHit(other.GetComponent<Collider>());

            Assert.AreEqual(0, receiver.AddCount);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void HandleHit_WhenHitIsEnvironment_FinishesWithoutDamage()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            TestDamageable target = CreateDamageableTarget("Target", Vector3.one);
            Collider environmentCollider = CreateEnvironment("Wall", Vector3.one).GetComponent<Collider>();
            projectile.Initialize(target.transform.position, target, CreateDamageParts(10f), null);

            projectile.HandleHit(environmentCollider);

            Assert.IsTrue(projectile.IsFinished);
            Assert.AreEqual(0, target.ApplyDamageCount);
        }

        [Test]
        public void HandleHit_WhenHitIsOtherDamageable_FinishesWithoutDamage()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            TestDamageable target = CreateDamageableTarget("Target", Vector3.one);
            TestDamageable other = CreateDamageableTarget("Other", Vector3.one);
            projectile.Initialize(target.transform.position, target, CreateDamageParts(10f), null);

            projectile.HandleHit(other.GetComponent<Collider>());

            Assert.IsTrue(projectile.IsFinished);
            Assert.AreEqual(0, target.ApplyDamageCount);
            Assert.AreEqual(0, other.ApplyDamageCount);
        }

        [Test]
        public void HandleHit_PassesDamageThroughExistingPipeline()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            TestDamageable target = CreateDamageableTarget("Target", Vector3.one);
            GameObject source = CreateObject("Source");
            projectile.Initialize(target.transform.position, target, CreateDamageParts(10f), source);

            projectile.HandleHit(target.GetComponent<Collider>());

            Assert.AreSame(source, target.LastDamageData.Source);
            Assert.AreEqual(1, target.LastDamageData.Parts.Count);
            Assert.AreEqual(DamageType.Physical, target.LastDamageData.Parts[0].DamageType);
        }

        [Test]
        public void HandleHit_WhenHitIsOwner_IgnoresOwner()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            TestDamageable target = CreateDamageableTarget("Target", Vector3.one);
            GameObject source = CreateEnvironment("Source", Vector3.one);
            projectile.Initialize(target.transform.position, target, CreateDamageParts(10f), source);

            projectile.HandleHit(source.GetComponent<Collider>());

            Assert.IsFalse(projectile.IsFinished);
            Assert.AreEqual(0, target.ApplyDamageCount);
        }

        [Test]
        public void HandleHit_FinishesOnlyOnce()
        {
            EnemyStraightProjectile projectile = CreateProjectile(Vector3.zero, Quaternion.identity);
            TestDamageable target = CreateDamageableTarget("Target", Vector3.one);
            Collider targetCollider = target.GetComponent<Collider>();
            projectile.Initialize(target.transform.position, target, CreateDamageParts(10f), null);

            projectile.HandleHit(targetCollider);
            projectile.HandleHit(targetCollider);

            Assert.AreEqual(1, projectile.FinishCount);
            Assert.AreEqual(1, target.ApplyDamageCount);
        }

        [Test]
        public void EnemyStraightProjectile_DoesNotDeclareRedundantCollisionCallbacks()
        {
            MethodInfo[] methods = typeof(EnemyStraightProjectile)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            Assert.IsFalse(methods.Any(method => method.Name == "OnCollisionEnter"));
            Assert.IsFalse(methods.Any(method => method.Name == "OnTriggerEnter"));
        }

        [Test]
        public void EnemyStraightProjectile_DoesNotDuplicateSharedLifecycleOrSweepLogic()
        {
            MethodInfo[] methods = typeof(EnemyStraightProjectile)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            Assert.IsFalse(methods.Any(method => method.Name == "Tick"));
            Assert.IsFalse(methods.Any(method => method.Name == "HandleHit"));
        }

        [Test]
        public void EnemyProjectile_KeepsImpactPoint()
        {
            TestProjectile projectile = CreateTestProjectile(Vector3.zero);
            CreateEnvironment("Wall", new Vector3(0f, 0f, 2.5f));
            projectile.InitializeForTest(null, 5f);
            Physics.SyncTransforms();

            projectile.Tick(0.5f);

            Assert.IsTrue(projectile.HasImpact);
            Assert.AreNotEqual(Vector3.zero, projectile.LastHit.Point);
        }

        private EnemyStraightProjectile CreateProjectile(Vector3 position, Quaternion rotation)
        {
            GameObject projectileObject = CreateObject("Projectile");
            projectileObject.transform.SetPositionAndRotation(position, rotation);
            projectileObject.AddComponent<StraightProjectileMover>();
            return projectileObject.AddComponent<EnemyStraightProjectile>();
        }

        private static void ConfigureProjectile(
            EnemyStraightProjectile projectile,
            float projectileSpeed,
            float projectileLifetime)
        {
            SerializedObject serializedProjectile = new(projectile);
            serializedProjectile.FindProperty("projectileSpeed").floatValue = projectileSpeed;
            serializedProjectile.FindProperty("projectileLifetime").floatValue = projectileLifetime;
            serializedProjectile.ApplyModifiedPropertiesWithoutUndo();
        }

        private TestDamageable CreateDamageableTarget(string objectName, Vector3 position)
        {
            GameObject targetObject = CreateEnvironment(objectName, position);
            return targetObject.AddComponent<TestDamageable>();
        }

        private GameObject CreateEnvironment(string objectName, Vector3 position)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = objectName;
            gameObject.transform.position = position;
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private TestProjectile CreateTestProjectile(Vector3 position)
        {
            GameObject projectileObject = CreateObject("TestProjectile");
            projectileObject.transform.position = position;
            return projectileObject.AddComponent<TestProjectile>();
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
            public int ApplyDamageCount { get; private set; }
            public DamageData LastDamageData { get; private set; }
            public bool CanReceiveDamage => true;

            public DamageResult ApplyDamage(DamageData data)
            {
                ApplyDamageCount++;
                LastDamageData = data;
                return DamageResult.Applied(data, data.Amount, 100f, 100f - data.Amount, false);
            }
        }

        private sealed class TestBuildupReceiver : MonoBehaviour, IBuildupReceiver
        {
            public int AddCount { get; private set; }
            public BuildupDefinition Definition { get; private set; }
            public float Amount { get; private set; }
            public RPGame.Core.Statuses.StatusContext Context { get; private set; }

            public void AddBuildup(
                BuildupDefinition definition,
                float amount,
                RPGame.Core.Statuses.StatusContext context)
            {
                AddCount++;
                Definition = definition;
                Amount = amount;
                Context = context;
            }
        }

        public sealed class TestProjectile : EnemyProjectile
        {
            public bool HasImpact { get; private set; }
            public EnemyProjectileHit LastHit { get; private set; }

            public void InitializeForTest(GameObject source, float projectileLifetime)
            {
                InitializeProjectile(CreateDamageParts(1f), source, projectileLifetime);
            }

            protected override void Move(float deltaTime)
            {
                transform.position += Vector3.forward * 10f * deltaTime;
            }

            protected override void OnImpact(EnemyProjectileHit hit, IDamageable damageable)
            {
                HasImpact = true;
                LastHit = hit;
            }
        }
    }
}
