using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Damage;
using RPGame.Core.Damage;
using RPGame.Core.Progression;
using RPGame.Core.Statistics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Enemies.Tests
{
    public sealed class EnemyExperienceRewardTests
    {
        private readonly List<GameObject> createdObjects = new();
        private readonly List<ScriptableObject> createdAssets = new();

        private GameObject enemyObject;
        private StatisticsController statistics;
        private DamageReceiver damageReceiver;

        [SetUp]
        public void SetUp()
        {
            enemyObject = CreateObject("Enemy");
            statistics = enemyObject.AddComponent<StatisticsController>();
            SetStatisticsConfig(statistics, CreateStatisticsConfig(100f));
            statistics.ResetToConfig();

            enemyObject.AddComponent<Attack>();
            MeleeBehaviourCreator behaviourCreator = enemyObject.AddComponent<MeleeBehaviourCreator>();
            Controller controller = enemyObject.AddComponent<Controller>();
            SetEnemyConfig(controller, CreateEnemyConfig(10), behaviourCreator);
            InvokeAwake(enemyObject.GetComponent<Movement>());
            damageReceiver = enemyObject.GetComponent<DamageReceiver>();

            EnemyExperienceReward reward = enemyObject.AddComponent<EnemyExperienceReward>();
            InvokeAwake(reward);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            for (int i = createdAssets.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdAssets[i]);
            }
        }

        [Test]
        public void FatalHit_WhenDeathDeactivatesEnemy_AwardsExperienceOnce()
        {
            ExperienceReceiver receiver = CreateExperienceReceiverInSourceHierarchy();
            statistics.Died += () => enemyObject.SetActive(false);

            damageReceiver.ApplyDamage(CreateDamage(100f, receiver.gameObject.transform.GetChild(0).gameObject));

            Assert.AreEqual(10, receiver.Experience);
            Assert.AreEqual(1, receiver.AddExperienceCallCount);
        }

        [Test]
        public void NonFatalHit_DoesNotAwardExperience()
        {
            ExperienceReceiver receiver = CreateExperienceReceiverInSourceHierarchy();

            damageReceiver.ApplyDamage(CreateDamage(50f, receiver.gameObject.transform.GetChild(0).gameObject));

            Assert.AreEqual(0, receiver.Experience);
        }

        [Test]
        public void FatalHit_WithoutExperienceReceiver_DoesNotAwardExperience()
        {
            GameObject source = CreateObject("Source");
            NonExperienceReceiver nonReceiver = source.AddComponent<NonExperienceReceiver>();

            damageReceiver.ApplyDamage(CreateDamage(100f, source));

            Assert.AreEqual(0, nonReceiver.AddExperienceCallCount);
        }

        [Test]
        public void PooledEnemy_WhenSpawnedAndKilledTwice_AwardsExperienceForBothKills()
        {
            ExperienceReceiver receiver = CreateExperienceReceiverInSourceHierarchy();
            PooledEnemy pooledEnemy = enemyObject.GetComponent<PooledEnemy>();
            InvokeAwake(pooledEnemy);
            GameObject source = receiver.gameObject.transform.GetChild(0).gameObject;

            pooledEnemy.OnSpawned();
            damageReceiver.ApplyDamage(CreateDamage(100f, source));
            pooledEnemy.OnDespawned();

            pooledEnemy.OnSpawned();
            damageReceiver.ApplyDamage(CreateDamage(100f, source));

            Assert.AreEqual(20, receiver.Experience);
            Assert.AreEqual(2, receiver.AddExperienceCallCount);
        }

        private ExperienceReceiver CreateExperienceReceiverInSourceHierarchy()
        {
            GameObject receiverObject = CreateObject("ExperienceReceiver");
            ExperienceReceiver receiver = receiverObject.AddComponent<ExperienceReceiver>();
            GameObject source = CreateObject("DamageSource");
            source.transform.SetParent(receiverObject.transform);
            return receiver;
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private Config CreateEnemyConfig(int experienceReward)
        {
            Config config = ScriptableObject.CreateInstance<Config>();
            createdAssets.Add(config);
            MeleeEnemyBehaviourConfig behaviourConfig = ScriptableObject.CreateInstance<MeleeEnemyBehaviourConfig>();
            createdAssets.Add(behaviourConfig);
            MeleeAttackConfig attackConfig = ScriptableObject.CreateInstance<MeleeAttackConfig>();
            createdAssets.Add(attackConfig);

            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("experienceReward").intValue = experienceReward;
            serializedConfig.FindProperty("behaviourConfig").objectReferenceValue = behaviourConfig;
            SerializedProperty attacks = serializedConfig.FindProperty("attacks");
            attacks.arraySize = 1;
            SerializedProperty attack = attacks.GetArrayElementAtIndex(0);
            attack.FindPropertyRelative("type").enumValueIndex = (int)AttackType.Melee;
            attack.FindPropertyRelative("config").objectReferenceValue = attackConfig;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private StatisticsConfig CreateStatisticsConfig(float maxHealth)
        {
            StatisticsConfig config = ScriptableObject.CreateInstance<StatisticsConfig>();
            createdAssets.Add(config);

            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("maxHealth").floatValue = maxHealth;
            serializedConfig.FindProperty("maxStamina").floatValue = 50f;
            serializedConfig.FindProperty("maxMana").floatValue = 50f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static DamageData CreateDamage(float amount, GameObject source)
        {
            PartialDamage[] damage =
            {
                new PartialDamage(amount, DamageType.Physical, DamageElement.None)
            };

            return new DamageData(damage, source);
        }

        private static void SetEnemyConfig(
            Controller controller,
            Config config,
            EnemyBehaviourCreator behaviourCreator)
        {
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("config").objectReferenceValue = config;
            serializedController.FindProperty("behaviourCreator").objectReferenceValue = behaviourCreator;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStatisticsConfig(StatisticsController controller, StatisticsConfig config)
        {
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("config").objectReferenceValue = config;
            serializedController.FindProperty("initializeOnAwake").boolValue = false;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokeAwake(MonoBehaviour behaviour)
        {
            MethodInfo awake = behaviour.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake?.Invoke(behaviour, null);
        }

        private sealed class ExperienceReceiver : MonoBehaviour, IExperienceReceiver
        {
            public int Experience { get; private set; }
            public int AddExperienceCallCount { get; private set; }

            public void AddExperience(int amount)
            {
                Experience += amount;
                AddExperienceCallCount++;
            }
        }

        private sealed class NonExperienceReceiver : MonoBehaviour
        {
            public int AddExperienceCallCount { get; private set; }

            public void AddExperience(int amount)
            {
                AddExperienceCallCount++;
            }
        }
    }
}
