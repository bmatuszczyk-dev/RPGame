using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Damage;
using RPGame.Combat.Projectiles;
using RPGame.Core.Damage;
using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace RPGame.Enemies.Tests
{
    public sealed class ControllerTests
    {
        private readonly List<GameObject> createdObjects = new();
        private readonly List<ScriptableObject> createdAssets = new();

        private GameObject enemyObject;
        private Detection detection;
        private RPGame.Enemies.Controller controller;

        [SetUp]
        public void SetUp()
        {
            ClearTargetRegistry();

            enemyObject = CreateEnemy("Enemy");
            detection = enemyObject.GetComponent<Detection>();
            controller = enemyObject.GetComponent<RPGame.Enemies.Controller>();
            ConfigureMeleeEnemy(controller, 1.5f, 0.1f, 10f);
            InvokeStart(controller);
        }

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
            ClearTargetRegistry();
        }

        [Test]
        public void Start_WithMeleeBehaviourConfig_CreatesMeleeEnemyBehaviour()
        {
            Assert.IsInstanceOf<MeleeEnemyBehaviour>(controller.Behaviour);
        }

        [Test]
        public void Start_PassesConfigKnockbackResistanceToMovement()
        {
            SerializedObject serializedConfig = new(controller.Config);
            serializedConfig.FindProperty("knockbackResistance").floatValue = 0.75f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            InvokeStart(controller);

            Assert.That(GetKnockbackResistance(controller.GetComponent<Movement>()), Is.EqualTo(0.75f));
        }

        [Test]
        public void ResetForSpawn_RecreatesBehaviourAfterDespawn()
        {
            controller.ResetForDespawn();

            Assert.IsNull(controller.Behaviour);

            controller.ResetForSpawn();

            Assert.IsInstanceOf<MeleeEnemyBehaviour>(controller.Behaviour);
        }

        [Test]
        public void Start_WithoutBehaviourCreator_DisablesController()
        {
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("behaviourCreator").objectReferenceValue = null;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            LogAssert.Expect(LogType.Error, "Missing field behaviourCreator.");

            InvokeStart(controller);

            Assert.IsFalse(controller.enabled);
            Assert.IsNull(controller.Behaviour);
        }

        [Test]
        public void Tick_WhenTargetIsInAttackRange_TriesAttack()
        {
            TargetFixture target = CreateDamageableTarget("Target", new Vector3(1f, 0f, 0f));
            detection.RefreshDetection();

            controller.Tick();

            Assert.AreEqual(90f, target.Statistics.CurrentHealth);
        }

        [Test]
        public void Tick_WhenTargetIsOutsideAttackRange_DoesNotAttack()
        {
            TargetFixture target = CreateDamageableTarget("Target", new Vector3(3f, 0f, 0f));
            detection.RefreshDetection();

            controller.Tick();

            Assert.AreEqual(100f, target.Statistics.CurrentHealth);
        }

        [Test]
        public void Tick_WhenTargetIsLost_DoesNotAttack()
        {
            TargetFixture target = CreateDamageableTarget("Target", new Vector3(1f, 0f, 0f));
            detection.RefreshDetection();

            target.Targetable.gameObject.SetActive(false);
            detection.RefreshDetection();
            controller.Tick();

            Assert.AreEqual(100f, target.Statistics.CurrentHealth);
        }

        [Test]
        public void Start_WhenMeleeAttackIsMissing_DisablesController()
        {
            GameObject enemy = CreateEnemy("MissingMeleeAttackEnemy");
            RPGame.Enemies.Controller enemyController = enemy.GetComponent<RPGame.Enemies.Controller>();
            Config config = CreateEnemyConfig(CreateAsset<MeleeEnemyBehaviourConfig>());
            ConfigureController(enemyController, config);

            LogAssert.Expect(LogType.Error, "Missing attack config for 'Melee'.");
            LogAssert.Expect(LogType.Error, "Attack 'Melee' failed to initialize.");

            InvokeStart(enemyController);

            Assert.IsFalse(enemyController.enabled);
            Assert.IsNull(enemyController.Behaviour);
        }

        [Test]
        public void Start_WithRangedBehaviourConfig_CreatesRangedEnemyBehaviour()
        {
            RPGame.Enemies.Controller rangedController = CreateConfiguredRangedController("RangedEnemy");

            InvokeStart(rangedController);

            Assert.IsInstanceOf<RangedEnemyBehaviour>(rangedController.Behaviour);
        }

        [Test]
        public void Tick_WithRangedBehaviourInRetreat_UsesStraightAttack()
        {
            RPGame.Enemies.Controller rangedController = CreateConfiguredRangedController("RangedEnemyRetreat");

            InvokeStart(rangedController);
            RangedEnemyBehaviour rangedBehaviour = (RangedEnemyBehaviour)rangedController.Behaviour;
            TargetFixture target = CreateDamageableTarget("RangedTarget", new Vector3(0f, 0f, 1f));
            rangedController.GetComponent<Detection>().RefreshDetection();

            rangedBehaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Retreat, rangedBehaviour.State);
            EnemyStraightProjectile spawnedStraightProjectile = UnityEngine.Object
                .FindObjectsByType<EnemyStraightProjectile>()
                .FirstOrDefault(projectile => !createdObjects.Contains(projectile.gameObject));
            if (spawnedStraightProjectile != null)
            {
                createdObjects.Add(spawnedStraightProjectile.gameObject);
            }

            Assert.IsNotNull(spawnedStraightProjectile);
            Assert.IsFalse(UnityEngine.Object.FindObjectsByType<EnemyParabolicProjectile>()
                .Any(projectile => !createdObjects.Contains(projectile.gameObject)));
        }

        [Test]
        public void Tick_WithRangedBehaviourInHold_UsesParabolicAttack()
        {
            RPGame.Enemies.Controller rangedController = CreateConfiguredRangedController("RangedEnemyHold");
            CreateGround();
            InvokeStart(rangedController);
            RangedEnemyBehaviour rangedBehaviour = (RangedEnemyBehaviour)rangedController.Behaviour;
            TargetFixture target = CreateDamageableTarget("HoldTarget", new Vector3(0f, 0f, 3f));
            rangedController.GetComponent<Detection>().RefreshDetection();

            rangedBehaviour.Tick(0.1f);

            EnemyParabolicProjectile spawnedParabolicProjectile = UnityEngine.Object
                .FindObjectsByType<EnemyParabolicProjectile>()
                .FirstOrDefault(projectile => !createdObjects.Contains(projectile.gameObject));
            if (spawnedParabolicProjectile != null)
            {
                createdObjects.Add(spawnedParabolicProjectile.gameObject);
            }

            Assert.AreEqual(RangedBehaviourState.Hold, rangedBehaviour.State);
            Assert.IsNotNull(spawnedParabolicProjectile);
            Assert.IsFalse(UnityEngine.Object.FindObjectsByType<EnemyStraightProjectile>()
                .Any(projectile => !createdObjects.Contains(projectile.gameObject)));
        }

        [Test]
        public void Start_WhenRangedStraightAttackIsMissing_DisablesController()
        {
            RPGame.Enemies.Controller rangedController = CreateConfiguredRangedController(
                "MissingStraightEnemy",
                AttackType.ParabolicProjectile);

            LogAssert.Expect(LogType.Error, "Missing attack config for 'StraightProjectile'.");
            LogAssert.Expect(LogType.Error, "Attack 'StraightProjectile' failed to initialize.");

            InvokeStart(rangedController);

            Assert.IsFalse(rangedController.enabled);
            Assert.IsNull(rangedController.Behaviour);
        }

        [Test]
        public void Start_WhenRangedParabolicAttackIsMissing_DisablesController()
        {
            RPGame.Enemies.Controller rangedController = CreateConfiguredRangedController(
                "MissingParabolicEnemy",
                AttackType.StraightProjectile);

            LogAssert.Expect(LogType.Error, "Missing attack config for 'ParabolicProjectile'.");
            LogAssert.Expect(LogType.Error, "Attack 'ParabolicProjectile' failed to initialize.");

            InvokeStart(rangedController);

            Assert.IsFalse(rangedController.enabled);
            Assert.IsNull(rangedController.Behaviour);
        }

        [Test]
        public void Start_WhenRequiredRangedAttackDependencyIsMissing_DisablesController()
        {
            GameObject enemy = CreateEnemy("MissingLineOfSightEnemy");
            enemy.AddComponent<GroundProjection>();
            enemy.AddComponent<ProjectileLauncher>();
            RPGame.Enemies.Controller rangedController = enemy.GetComponent<RPGame.Enemies.Controller>();
            Config config = CreateRangedEnemyConfig();
            ConfigureController(rangedController, config);

            LogAssert.Expect(LogType.Error, "Missing field lineOfSight.");
            LogAssert.Expect(LogType.Error, "Attack 'StraightProjectile' failed to initialize.");

            InvokeStart(rangedController);

            Assert.IsFalse(rangedController.enabled);
            Assert.IsNull(rangedController.Behaviour);
        }

        [Test]
        public void Start_WhenBehaviourConfigIsMissing_DisablesControllerWithoutMeleeFallback()
        {
            GameObject enemy = CreateEnemy("MissingBehaviourConfigEnemy");
            RPGame.Enemies.Controller enemyController = enemy.GetComponent<RPGame.Enemies.Controller>();
            Config config = CreateEnemyConfig(null);
            ConfigureController(enemyController, config);

            LogAssert.Expect(LogType.Error, "Missing field behaviourConfig.");

            InvokeStart(enemyController);

            Assert.IsFalse(enemyController.enabled);
            Assert.IsNull(enemyController.Behaviour);
        }

        [Test]
        public void Start_WhenBehaviourConfigIsUnsupported_DisablesController()
        {
            GameObject enemy = CreateEnemy("UnsupportedBehaviourEnemy");
            RPGame.Enemies.Controller enemyController = enemy.GetComponent<RPGame.Enemies.Controller>();
            Config config = CreateEnemyConfig(CreateAsset<UnsupportedEnemyBehaviourConfig>());
            ConfigureController(enemyController, config);

            LogAssert.Expect(LogType.Error, "MeleeBehaviourCreator requires MeleeEnemyBehaviourConfig.");

            InvokeStart(enemyController);

            Assert.IsFalse(enemyController.enabled);
            Assert.IsNull(enemyController.Behaviour);
        }

        [Test]
        public void Controller_DoesNotStoreAttackCooldown()
        {
            bool hasCooldownField = typeof(RPGame.Enemies.Controller)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(float) || field.Name.ToLowerInvariant().Contains("cooldown"));

            Assert.IsFalse(hasCooldownField);
        }

        [Test]
        public void Controller_RequiresCoreEnemyComponents()
        {
            Assert.IsTrue(RequiresComponent<StatisticsController>());
            Assert.IsTrue(RequiresComponent<Detection>());
            Assert.IsTrue(RequiresComponent<Movement>());
            Assert.IsFalse(RequiresComponent<Attack>());
            Assert.IsTrue(RequiresComponent<EnemyTargetable>());
            Assert.IsTrue(RequiresComponent<DamageReceiver>());
            Assert.IsTrue(RequiresComponent<Death>());
        }

        [Test]
        public void Controller_DoesNotReferencePlayerDamageOrNavMeshLogic()
        {
            bool referencesForbiddenAssembly = typeof(RPGame.Enemies.Controller).Assembly
                .GetReferencedAssemblies()
                .Any(assemblyName => assemblyName.Name == "RPGame.Player" || assemblyName.Name == "RPGame.Loot");

            bool hasForbiddenField = typeof(RPGame.Enemies.Controller)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field =>
                    field.FieldType == typeof(DamageData)
                    || field.FieldType == typeof(NavMeshAgent)
                    || field.FieldType.Namespace == "UnityEngine.AI"
                    || field.FieldType == typeof(LineOfSight)
                    || field.FieldType == typeof(GroundProjection)
                    || field.FieldType == typeof(ProjectileLauncher));

            Assert.IsFalse(referencesForbiddenAssembly);
            Assert.IsFalse(hasForbiddenField);
        }

        [Test]
        public void Controller_DoesNotContainAttackRules()
        {
            string source = File.ReadAllText(Path.Combine(Application.dataPath, "Enemies", "Scripts", "Controller.cs"));

            Assert.IsFalse(source.Contains("HasLineOfSight", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("DamageData", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("ProjectileLaunchData", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("IsInRange(", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("TryAttack(", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("switch", StringComparison.Ordinal));
        }

        [Test]
        public void EnemyBehaviour_IsSelectedWithoutGlobalBehaviourEnum()
        {
            bool hasGlobalBehaviourEnum = typeof(RPGame.Enemies.Controller).Assembly
                .GetTypes()
                .Any(type => type.IsEnum
                    && (type.Name == "EnemyType" || type.Name == "EnemyBehaviourType"));

            Assert.IsFalse(hasGlobalBehaviourEnum);
        }

        [Test]
        public void MeleeEnemyBehaviourConfig_IsExplicitBehaviourConfigMarker()
        {
            Assert.IsTrue(typeof(EnemyBehaviourConfigBase).IsAssignableFrom(typeof(MeleeEnemyBehaviourConfig)));
            Assert.AreEqual(0, typeof(MeleeEnemyBehaviourConfig)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Length);
        }

        [Test]
        public void Config_IsSerializedOnlyOnController()
        {
            FieldInfo controllerConfig = typeof(RPGame.Enemies.Controller)
                .GetField("config", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo attackConfig = typeof(Attack)
                .GetField("config", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(controllerConfig);
            Assert.IsTrue(controllerConfig.IsDefined(typeof(SerializeField), false));
            Assert.IsNotNull(attackConfig);
            Assert.IsFalse(attackConfig.IsDefined(typeof(SerializeField), false));
        }

        private RPGame.Enemies.Controller CreateConfiguredRangedController(string objectName)
        {
            GameObject enemy = CreateEnemy(objectName);
            enemy.AddComponent<LineOfSight>();
            enemy.AddComponent<GroundProjection>();
            enemy.AddComponent<ProjectileLauncher>();

            RPGame.Enemies.Controller enemyController = enemy.GetComponent<RPGame.Enemies.Controller>();
            Config config = CreateRangedEnemyConfig();
            ConfigureController(enemyController, config);
            return enemyController;
        }

        private RPGame.Enemies.Controller CreateConfiguredRangedController(
            string objectName,
            AttackType attackType)
        {
            GameObject enemy = CreateEnemy(objectName);
            enemy.AddComponent<LineOfSight>();
            enemy.AddComponent<GroundProjection>();
            enemy.AddComponent<ProjectileLauncher>();

            RPGame.Enemies.Controller enemyController = enemy.GetComponent<RPGame.Enemies.Controller>();
            Config config = CreateRangedEnemyConfig(attackType);
            ConfigureController(enemyController, config);
            return enemyController;
        }

        private GameObject CreateEnemy(string objectName)
        {
            GameObject enemy = CreateObject(objectName);
            enemy.transform.position = Vector3.zero;
            enemy.AddComponent<NavMeshAgent>();
            enemy.AddComponent<Detection>();
            enemy.AddComponent<Attack>();
            enemy.AddComponent<MeleeBehaviourCreator>();
            enemy.AddComponent<RPGame.Enemies.Controller>();
            return enemy;
        }

        private TargetFixture CreateDamageableTarget(string objectName, Vector3 position)
        {
            GameObject targetObject = CreateObject(objectName);
            targetObject.transform.position = position;

            StatisticsConfig config = CreateStatisticsConfig(100f);
            StatisticsController statistics = targetObject.AddComponent<StatisticsController>();
            SetControllerConfig(statistics, config);
            statistics.ResetToConfig();

            DamageReceiver damageReceiver = targetObject.AddComponent<DamageReceiver>();
            SetDamageReceiverLogging(damageReceiver, false);

            PlayerTargetable targetable = targetObject.AddComponent<PlayerTargetable>();
            return new TargetFixture(targetable, statistics);
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private StatisticsConfig CreateStatisticsConfig(float maxHealth)
        {
            StatisticsConfig statisticsConfig = CreateAsset<StatisticsConfig>();

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

        private void ConfigureMeleeEnemy(
            RPGame.Enemies.Controller enemyController,
            float attackRange,
            float attackInterval,
            float damageAmount)
        {
            MeleeAttackConfig meleeAttackConfig = CreateMeleeAttackConfig(attackRange, attackInterval, damageAmount);
            Config config = CreateEnemyConfig(
                CreateAsset<MeleeEnemyBehaviourConfig>(),
                (AttackType.Melee, meleeAttackConfig));

            ConfigureController(enemyController, config);
        }

        private Config CreateRangedEnemyConfig(params AttackType[] attackTypes)
        {
            EnemyStraightProjectile straightProjectilePrefab = CreateStraightProjectilePrefab("StraightProjectilePrefab");
            EnemyParabolicProjectile parabolicProjectilePrefab = CreateParabolicProjectilePrefab("ParabolicProjectilePrefab");
            AttackType[] configuredTypes = attackTypes.Length > 0
                ? attackTypes
                : new[] { AttackType.StraightProjectile, AttackType.ParabolicProjectile };
            List<(AttackType Type, AttackConfig Config)> attacks = new();

            for (int i = 0; i < configuredTypes.Length; i++)
            {
                AttackType type = configuredTypes[i];
                attacks.Add(type switch
                {
                    AttackType.StraightProjectile => (
                        AttackType.StraightProjectile,
                        CreateStraightProjectileAttackConfig(straightProjectilePrefab)),
                    AttackType.ParabolicProjectile => (
                        AttackType.ParabolicProjectile,
                        CreateParabolicProjectileAttackConfig(parabolicProjectilePrefab)),
                    _ => throw new InvalidOperationException($"Unsupported ranged test attack type '{type}'.")
                });
            }

            return CreateEnemyConfig(CreateRangedBehaviourConfig(), attacks.ToArray());
        }

        private Config CreateEnemyConfig(
            EnemyBehaviourConfigBase behaviourConfig,
            params (AttackType Type, AttackConfig Config)[] attacks)
        {
            Config config = CreateAsset<Config>();
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("behaviourConfig").objectReferenceValue = behaviourConfig;
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

        private MeleeAttackConfig CreateMeleeAttackConfig(float attackRange, float attackInterval, float damageAmount)
        {
            MeleeAttackConfig meleeAttackConfig = CreateAsset<MeleeAttackConfig>();
            SerializedObject serializedMeleeConfig = new(meleeAttackConfig);
            serializedMeleeConfig.FindProperty("attackInterval").floatValue = attackInterval;
            serializedMeleeConfig.FindProperty("attackRange").floatValue = attackRange;
            ConfigureDamage(serializedMeleeConfig, damageAmount);
            serializedMeleeConfig.ApplyModifiedPropertiesWithoutUndo();
            return meleeAttackConfig;
        }

        private StraightProjectileAttackConfig CreateStraightProjectileAttackConfig(EnemyStraightProjectile projectilePrefab)
        {
            StraightProjectileAttackConfig config = CreateAsset<StraightProjectileAttackConfig>();
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("attackInterval").floatValue = 0.1f;
            serializedConfig.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab.gameObject;
            ConfigureDamage(serializedConfig, 10f);
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private ParabolicProjectileAttackConfig CreateParabolicProjectileAttackConfig(EnemyParabolicProjectile projectilePrefab)
        {
            ParabolicProjectileAttackConfig config = CreateAsset<ParabolicProjectileAttackConfig>();
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("attackInterval").floatValue = 0.1f;
            serializedConfig.FindProperty("targetRandomRadius").floatValue = 0f;
            serializedConfig.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab.gameObject;
            ConfigureDamage(serializedConfig, 10f);
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private RangedEnemyBehaviourConfig CreateRangedBehaviourConfig()
        {
            RangedEnemyBehaviourConfig config = CreateAsset<RangedEnemyBehaviourConfig>();
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("minRange").floatValue = 2f;
            serializedConfig.FindProperty("maxRange").floatValue = 5f;
            serializedConfig.FindProperty("rangeHysteresis").floatValue = 0.5f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private void ConfigureController(RPGame.Enemies.Controller enemyController, Config config)
        {
            SerializedObject serializedController = new(enemyController);
            serializedController.FindProperty("config").objectReferenceValue = config;
            serializedController.FindProperty("behaviourCreator").objectReferenceValue = GetCreator(enemyController.gameObject, config);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EnemyBehaviourCreator GetCreator(GameObject enemy, Config config)
        {
            return config.BehaviourConfig switch
            {
                RangedEnemyBehaviourConfig => enemy.GetComponent<RangedBehaviourCreator>()
                    ?? enemy.AddComponent<RangedBehaviourCreator>(),
                ChargingMeleeEnemyBehaviourConfig => enemy.GetComponent<ChargingMeleeBehaviourCreator>()
                    ?? enemy.AddComponent<ChargingMeleeBehaviourCreator>(),
                HealerEnemyBehaviourConfig => enemy.GetComponent<HealerBehaviourCreator>()
                    ?? enemy.AddComponent<HealerBehaviourCreator>(),
                AssassinEnemyBehaviourConfig => enemy.GetComponent<AssassinBehaviourCreator>()
                    ?? enemy.AddComponent<AssassinBehaviourCreator>(),
                _ => enemy.GetComponent<MeleeBehaviourCreator>()
                    ?? enemy.AddComponent<MeleeBehaviourCreator>()
            };
        }

        private static void ConfigureDamage(SerializedObject serializedConfig, float damageAmount)
        {
            SerializedProperty damageProperty = serializedConfig.FindProperty("damage");
            damageProperty.arraySize = 1;
            SerializedProperty damageEntry = damageProperty.GetArrayElementAtIndex(0);
            damageEntry.FindPropertyRelative("minDamage").floatValue = damageAmount;
            damageEntry.FindPropertyRelative("maxDamage").floatValue = damageAmount;
            damageEntry.FindPropertyRelative("damageType").enumValueIndex = (int)DamageType.Physical;
            damageEntry.FindPropertyRelative("damageElement").enumValueIndex = (int)DamageElement.None;
        }

        private static float GetKnockbackResistance(Movement movement)
        {
            FieldInfo field = typeof(Movement).GetField(
                "knockbackResistance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (float)field.GetValue(movement);
        }

        private EnemyStraightProjectile CreateStraightProjectilePrefab(string objectName)
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

        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            createdObjects.Add(ground);
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdAssets.Add(asset);
            return asset;
        }

        private static void SetControllerConfig(StatisticsController controller, StatisticsConfig statisticsConfig)
        {
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("config").objectReferenceValue = statisticsConfig;
            serializedController.FindProperty("initializeOnAwake").boolValue = false;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetDamageReceiverLogging(DamageReceiver damageReceiver, bool loggingEnabled)
        {
            SerializedObject serializedReceiver = new(damageReceiver);
            serializedReceiver.FindProperty("loggingEnabled").boolValue = loggingEnabled;
            serializedReceiver.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ClearTargetRegistry()
        {
            MethodInfo method = typeof(TargetRegistry).GetMethod("Clear", BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, null);
        }

        private static bool RequiresComponent<T>()
        {
            return typeof(RPGame.Enemies.Controller)
                .GetCustomAttributes<RequireComponent>()
                .Any(attribute =>
                    attribute.m_Type0 == typeof(T)
                    || attribute.m_Type1 == typeof(T)
                    || attribute.m_Type2 == typeof(T));
        }

        private static void InvokeStart(RPGame.Enemies.Controller enemyController)
        {
            MethodInfo method = typeof(RPGame.Enemies.Controller).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(enemyController, null);
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

        private sealed class UnsupportedEnemyBehaviourConfig : EnemyBehaviourConfigBase
        {
        }
    }
}
