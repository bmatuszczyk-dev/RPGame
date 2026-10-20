using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Damage;
using RPGame.Core.Damage;
using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace RPGame.Enemies.Tests
{
    public sealed class DeathTests
    {
        private readonly List<GameObject> createdObjects = new();
        private readonly List<ScriptableObject> createdAssets = new();

        private GameObject enemyObject;
        private StatisticsController enemyStatistics;
        private Movement movement;
        private Detection detection;
        private RPGame.Enemies.Controller controller;
        private EnemyTargetable targetable;
        private Death death;

        [SetUp]
        public void SetUp()
        {
            ClearTargetRegistry();

            enemyObject = CreateObject("Enemy");
            enemyObject.transform.position = Vector3.zero;
            enemyStatistics = enemyObject.AddComponent<StatisticsController>();
            SetControllerConfig(enemyStatistics, CreateConfig(100f));
            enemyStatistics.ResetToConfig();

            enemyObject.AddComponent<NavMeshAgent>();
            movement = enemyObject.AddComponent<Movement>();
            InvokeAwake(movement);
            detection = enemyObject.AddComponent<Detection>();
            controller = enemyObject.AddComponent<RPGame.Enemies.Controller>();
            targetable = enemyObject.GetComponent<EnemyTargetable>();
            death = enemyObject.GetComponent<Death>();
            InvokeStart(death);
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

            createdObjects.Clear();
            createdAssets.Clear();
            ClearTargetRegistry();
        }

        [Test]
        public void DeathEvent_DisablesController()
        {
            KillEnemy();

            Assert.IsFalse(controller.enabled);
        }

        [Test]
        public void DeathEvent_PreventsControllerFromRunningFurtherLogic()
        {
            TargetFixture target = CreateDamageablePlayerTarget("PlayerTarget", new Vector3(1f, 0f, 0f));

            KillEnemy();
            controller.Tick();

            Assert.AreEqual(100f, target.Statistics.CurrentHealth);
        }

        [Test]
        public void DeathEvent_DisablesDetection()
        {
            KillEnemy();

            Assert.IsFalse(detection.enabled);
        }

        [Test]
        public void DeathEvent_DisablesTargetable()
        {
            KillEnemy();

            Assert.IsFalse(targetable.enabled);
        }

        [Test]
        public void DeathEvent_RemovesEnemyFromTargetRegistry()
        {
            Assert.Contains(targetable, (System.Collections.ICollection)TargetRegistry.EnemyTargets);

            KillEnemy();

            CollectionAssert.DoesNotContain(TargetRegistry.EnemyTargets, targetable);
        }

        [Test]
        public void DeathEvent_IsHandledOnlyOnce()
        {
            KillEnemy();

            Assert.DoesNotThrow(InvokeHandleDeathDirectly);

            Assert.IsTrue(death.IsDead);
            Assert.IsFalse(controller.enabled);
            Assert.IsFalse(detection.enabled);
            Assert.IsFalse(targetable.enabled);
        }

        [Test]
        public void DeathEvent_FiresLifecycleCallbackOnceAfterDeathCleanup()
        {
            int callbackCount = 0;
            bool controllerDisabledWhenCallbackFired = false;
            death.OnDeathCleanupEnd += () =>
            {
                callbackCount++;
                controllerDisabledWhenCallbackFired = !controller.enabled;
            };

            KillEnemy();
            InvokeHandleDeathDirectly();

            Assert.AreEqual(1, callbackCount);
            Assert.IsTrue(controllerDisabledWhenCallbackFired);
        }

        [Test]
        public void DeathEvent_DisablesDetectionAndClearsCurrentTarget()
        {
            CreateDamageablePlayerTarget("PlayerTarget", new Vector3(1f, 0f, 0f));
            detection.RefreshDetection();

            Assert.IsTrue(detection.HasTarget);

            KillEnemy();

            Assert.IsFalse(detection.enabled);
            Assert.IsNull(detection.CurrentTarget);
            Assert.IsNull(detection.CurrentTargetPoint);
        }

        [Test]
        public void Start_WhenStatisticsHasNoHealthConfig_DoesNotTriggerDeathCleanup()
        {
            GameObject unconfiguredEnemy = CreateObject("UnconfiguredEnemy");
            unconfiguredEnemy.AddComponent<StatisticsController>();
            unconfiguredEnemy.AddComponent<NavMeshAgent>();
            unconfiguredEnemy.AddComponent<Detection>();

            RPGame.Enemies.Controller unconfiguredController = unconfiguredEnemy.AddComponent<RPGame.Enemies.Controller>();
            Movement unconfiguredMovement = unconfiguredEnemy.GetComponent<Movement>();
            Detection unconfiguredDetection = unconfiguredEnemy.GetComponent<Detection>();
            EnemyTargetable unconfiguredTargetable = unconfiguredEnemy.GetComponent<EnemyTargetable>();
            Death unconfiguredDeath = unconfiguredEnemy.GetComponent<Death>();

            InvokeStart(unconfiguredDeath);

            Assert.IsFalse(unconfiguredDeath.IsDead);
            Assert.IsTrue(unconfiguredController.enabled);
            Assert.IsTrue(unconfiguredDetection.enabled);
            Assert.IsTrue(unconfiguredTargetable.enabled);
            Assert.IsTrue(unconfiguredMovement.enabled);
        }

        [Test]
        public void Death_DoesNotContainDamageMovementOrAttackLogic()
        {
            bool referencesForbiddenAssembly = typeof(Death).Assembly
                .GetReferencedAssemblies()
                .Any(assemblyName => assemblyName.Name == "RPGame.Player");

            bool hasForbiddenField = typeof(Death)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field =>
                    field.FieldType == typeof(DamageData)
                    || field.FieldType == typeof(Attack)
                    || field.FieldType == typeof(NavMeshAgent)
                    || field.FieldType.Namespace == "UnityEngine.AI");

            Assert.IsFalse(referencesForbiddenAssembly);
            Assert.IsFalse(hasForbiddenField);
        }

        [Test]
        public void Death_StopsMovementOnDeath()
        {
            string source = System.IO.File.ReadAllText(System.IO.Path.Combine(
                Application.dataPath,
                "Enemies",
                "Scripts",
                "Death.cs"));

            Assert.IsTrue(source.Contains("movement.Stop();", System.StringComparison.Ordinal));
        }

        [Test]
        public void DeathEvent_WhenLeaping_ClearsLeapState()
        {
            SetIsLeaping(movement, true);

            KillEnemy();

            Assert.IsFalse(movement.IsLeaping);
        }

        [Test]
        public void DeathEvent_WhenCharging_ClearsChargeState()
        {
            SetIsCharging(movement, true);

            KillEnemy();

            Assert.IsFalse(movement.IsCharging);
        }

        [Test]
        public void CoreAndCombat_DoNotReferenceEnemiesOrLoot()
        {
            bool coreReferencesEnemies = typeof(StatisticsController).Assembly
                .GetReferencedAssemblies()
                .Any(assemblyName => assemblyName.Name == "RPGame.Enemies" || assemblyName.Name == "RPGame.Loot");

            bool combatReferencesEnemies = typeof(DamageReceiver).Assembly
                .GetReferencedAssemblies()
                .Any(assemblyName => assemblyName.Name == "RPGame.Enemies" || assemblyName.Name == "RPGame.Loot");

            Assert.IsFalse(coreReferencesEnemies);
            Assert.IsFalse(combatReferencesEnemies);
        }

        private void KillEnemy()
        {
            enemyStatistics.TakeDamage(100f);
        }

        private void InvokeHandleDeathDirectly()
        {
            MethodInfo method = typeof(Death).GetMethod("HandleDeath", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(death, null);
        }

        private static void InvokeStart(Death enemyDeath)
        {
            MethodInfo method = typeof(Death).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(enemyDeath, null);
        }

        private static void InvokeAwake(Movement enemyMovement)
        {
            MethodInfo method = typeof(Movement).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(enemyMovement, null);
        }

        private TargetFixture CreateDamageablePlayerTarget(string objectName, Vector3 position)
        {
            GameObject targetObject = CreateObject(objectName);
            targetObject.transform.position = position;

            StatisticsController statistics = targetObject.AddComponent<StatisticsController>();
            SetControllerConfig(statistics, CreateConfig(100f));
            statistics.ResetToConfig();
            targetObject.AddComponent<DamageReceiver>();

            PlayerTargetable playerTargetable = targetObject.AddComponent<PlayerTargetable>();
            return new TargetFixture(playerTargetable, statistics);
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private StatisticsConfig CreateConfig(float maxHealth)
        {
            StatisticsConfig statisticsConfig = ScriptableObject.CreateInstance<StatisticsConfig>();
            createdAssets.Add(statisticsConfig);

            SerializedObject serializedConfig = new(statisticsConfig);
            serializedConfig.FindProperty("maxHealth").floatValue = maxHealth;
            serializedConfig.FindProperty("maxStamina").floatValue = 50f;
            serializedConfig.FindProperty("maxMana").floatValue = 50f;
            serializedConfig.FindProperty("healthRegenerationPerSecond").floatValue = 0f;
            serializedConfig.FindProperty("staminaRegenerationPerSecond").floatValue = 0f;
            serializedConfig.FindProperty("staminaRegenerationDelay").floatValue = 0f;
            serializedConfig.FindProperty("manaRegenerationPerSecond").floatValue = 0f;
            serializedConfig.FindProperty("manaRegenerationDelay").floatValue = 0f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return statisticsConfig;
        }

        private static void SetControllerConfig(StatisticsController controller, StatisticsConfig statisticsConfig)
        {
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("config").objectReferenceValue = statisticsConfig;
            serializedController.FindProperty("initializeOnAwake").boolValue = false;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetIsLeaping(Movement enemyMovement, bool isLeaping)
        {
            FieldInfo movementField = typeof(Movement).GetField("leap", BindingFlags.Instance | BindingFlags.NonPublic);
            LeapMovement leap = (LeapMovement)movementField.GetValue(enemyMovement);
            FieldInfo activeField = typeof(LeapMovement).GetField("isActive", BindingFlags.Instance | BindingFlags.NonPublic);
            activeField.SetValue(leap, isLeaping);
        }

        private static void SetIsCharging(Movement enemyMovement, bool isCharging)
        {
            FieldInfo movementField = typeof(Movement).GetField("charge", BindingFlags.Instance | BindingFlags.NonPublic);
            ChargeMovement charge = (ChargeMovement)movementField.GetValue(enemyMovement);
            FieldInfo activeField = typeof(ChargeMovement).GetField("isActive", BindingFlags.Instance | BindingFlags.NonPublic);
            activeField.SetValue(charge, isCharging);
        }

        private static void ClearTargetRegistry()
        {
            MethodInfo method = typeof(TargetRegistry).GetMethod("Clear", BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, null);
        }

        private readonly struct TargetFixture
        {
            public TargetFixture(PlayerTargetable targetable, StatisticsController statistics)
            {
                Targetable = targetable;
                Statistics = statistics;
            }

            public PlayerTargetable Targetable { get; }
            public StatisticsController Statistics { get; }
        }
    }
}
