using System;
using System.Collections.Generic;
using RPGame.Combat.Damage;
using RPGame.Core.Damage;
using RPGame.Core.Pooling;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using RPGame.Core.Targeting;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Enemies.Tests
{
    public sealed class PooledEnemyTests
    {
        private GameObject gameObject;
        private PooledEnemy pooledEnemy;
        private readonly List<ScriptableObject> createdAssets = new();

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("PooledEnemy");
            pooledEnemy = gameObject.AddComponent<PooledEnemy>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            for (int i = 0; i < createdAssets.Count; i++)
            {
                if (createdAssets[i] != null)
                {
                    Object.DestroyImmediate(createdAssets[i]);
                }
            }

            createdAssets.Clear();
        }

        [Test]
        public void IsSpawned_WhenCreated_IsFalse()
        {
            Assert.IsFalse(pooledEnemy.IsSpawned);
        }

        [Test]
        public void OnSpawned_WhenNotSpawned_SetsIsSpawnedTrue()
        {
            pooledEnemy.OnSpawned();

            Assert.IsTrue(pooledEnemy.IsSpawned);
        }

        [Test]
        public void OnDespawned_WhenSpawned_SetsIsSpawnedFalse()
        {
            pooledEnemy.OnSpawned();

            pooledEnemy.OnDespawned();

            Assert.IsFalse(pooledEnemy.IsSpawned);
        }

        [Test]
        public void OnSpawned_WhenAlreadySpawned_ThrowsClearly()
        {
            pooledEnemy.OnSpawned();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => pooledEnemy.OnSpawned());

            StringAssert.Contains("already spawned", exception.Message);
        }

        [Test]
        public void OnDespawned_WhenNotSpawned_ThrowsClearly()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => pooledEnemy.OnDespawned());

            StringAssert.Contains("not spawned", exception.Message);
        }

        [Test]
        public void PooledEnemy_WhenSpawnedDespawnedTwice_Works()
        {
            pooledEnemy.OnSpawned();
            pooledEnemy.OnDespawned();
            pooledEnemy.OnSpawned();
            pooledEnemy.OnDespawned();

            Assert.IsFalse(pooledEnemy.IsSpawned);
        }

        [Test]
        public void OnSpawnedAndOnDespawned_InvokeResettableComponents()
        {
            Object.DestroyImmediate(pooledEnemy);
            TestResettable resettable = gameObject.AddComponent<TestResettable>();
            CreatePooledEnemy();

            pooledEnemy.OnSpawned();
            pooledEnemy.OnDespawned();

            Assert.AreEqual(1, resettable.ResetForSpawnCount);
            Assert.AreEqual(1, resettable.ResetForDespawnCount);
        }

        [Test]
        public void OnSpawned_WhenResettableIsAddedAfterAwake_DoesNotDiscoverIt()
        {
            TestResettable lateResettable = gameObject.AddComponent<TestResettable>();

            pooledEnemy.OnSpawned();

            Assert.AreEqual(0, lateResettable.ResetForSpawnCount);
        }

        [Test]
        public void StatisticsController_WhenPooledEnemySpawns_ReturnsVitalsToConfig()
        {
            Object.DestroyImmediate(pooledEnemy);
            StatisticsController statistics = gameObject.AddComponent<StatisticsController>();
            SetPrivateField(statistics, "config", CreateStatisticsConfig());
            statistics.ResetToConfig();
            statistics.TakeDamage(40f);
            statistics.TrySpendStamina(20f);
            statistics.TrySpendMana(30f);
            CreatePooledEnemy();

            pooledEnemy.OnSpawned();

            Assert.AreEqual(statistics.MaxHealth, statistics.CurrentHealth);
            Assert.AreEqual(statistics.MaxStamina, statistics.CurrentStamina);
            Assert.AreEqual(statistics.MaxMana, statistics.CurrentMana);
        }

        [Test]
        public void StatusAggregator_WhenPooledEnemyRespawns_ClearsStatuses()
        {
            Object.DestroyImmediate(pooledEnemy);
            StatusAggregator statuses = gameObject.AddComponent<StatusAggregator>();
            typeof(StatusAggregator).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(statuses, null);
            TestStatusDefinition status = CreateAsset<TestStatusDefinition>();
            statuses.ApplyStatus(status, 5f, new StatusContext(new StatusSourceId("Test"), gameObject));
            CreatePooledEnemy();

            pooledEnemy.OnSpawned();

            Assert.AreEqual(0, statuses.Statuses.Count);
            Assert.AreEqual(1, status.CleanupCount);
        }

        [Test]
        public void Death_WhenPooledEnemyRespawns_CanHandleDeathAgain()
        {
            Object.DestroyImmediate(pooledEnemy);
            StatisticsController statistics = gameObject.AddComponent<StatisticsController>();
            SetPrivateField(statistics, "config", CreateStatisticsConfig());
            statistics.ResetToConfig();
            Movement movement = gameObject.AddComponent<Movement>();
            typeof(Movement).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(movement, null);
            typeof(Movement).GetMethod(
                    "Start",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(movement, null);
            Detection detection = gameObject.AddComponent<Detection>();
            EnemyTargetable targetable = gameObject.AddComponent<EnemyTargetable>();
            gameObject.AddComponent<DamageReceiver>();
            Death death = gameObject.AddComponent<Death>();
            SetPrivateField(death, "deathSource", statistics);
            SetPrivateField(death, "movement", movement);
            SetPrivateField(death, "detection", detection);
            SetPrivateField(death, "targetable", targetable);
            CreatePooledEnemy();
            Controller controller = gameObject.AddComponent<Controller>();
            SetPrivateField(death, "controller", controller);

            pooledEnemy.OnSpawned();
            statistics.TakeDamage(statistics.MaxHealth);
            Assert.IsTrue(death.IsDead);
            Assert.IsFalse(controller.enabled);

            pooledEnemy.OnDespawned();
            pooledEnemy.OnSpawned();
            statistics.TakeDamage(statistics.MaxHealth);

            Assert.IsTrue(death.IsDead);
            Assert.IsFalse(controller.enabled);
        }

        private StatisticsConfig CreateStatisticsConfig()
        {
            StatisticsConfig config = CreateAsset<StatisticsConfig>();
            SetPrivateField(config, "maxHealth", 100f);
            SetPrivateField(config, "maxStamina", 50f);
            SetPrivateField(config, "maxMana", 80f);
            return config;
        }

        private void CreatePooledEnemy()
        {
            pooledEnemy = gameObject.AddComponent<PooledEnemy>();
            typeof(PooledEnemy).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(pooledEnemy, null);
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdAssets.Add(asset);
            return asset;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class TestResettable : MonoBehaviour, IPooledEnemyResettable
        {
            public int ResetForSpawnCount { get; private set; }
            public int ResetForDespawnCount { get; private set; }

            public void ResetForSpawn()
            {
                ResetForSpawnCount++;
            }

            public void ResetForDespawn()
            {
                ResetForDespawnCount++;
            }
        }

        private sealed class TestStatusDefinition : StatusDefinition
        {
            public int CleanupCount { get; private set; }

            public override void OnApply(StatusTarget target, StatusInstance instance)
            {
                instance.RegisterCleanup(() => CleanupCount++);
            }

            public override string ToString()
            {
                return "Test Status";
            }
        }
    }
}
