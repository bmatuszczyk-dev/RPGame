using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace RPGame.Enemies.Tests
{
    public sealed class EnemyBehaviourCreatorTests
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
        }

        [Test]
        public void MeleeCreator_CreatesMeleeBehaviour_AndInitializesResistance()
        {
            GameObject enemy = CreateBaseEnemy();
            MeleeBehaviourCreator creator = enemy.AddComponent<MeleeBehaviourCreator>();
            Config config = CreateConfig(CreateAsset<MeleeEnemyBehaviourConfig>(), (AttackType.Melee, CreateAsset<MeleeAttackConfig>()));
            SetKnockbackResistance(config, 0.75f);

            Assert.IsTrue(creator.TryCreate(config, out IEnemyBehaviour behaviour));
            Assert.IsInstanceOf<MeleeEnemyBehaviour>(behaviour);
            Assert.That(GetKnockbackResistance(enemy.GetComponent<Movement>()), Is.EqualTo(0.75f));
        }

        [Test]
        public void ChargingMeleeCreator_CreatesChargingMeleeBehaviour()
        {
            GameObject enemy = CreateBaseEnemy();
            ChargingMeleeBehaviourCreator creator = enemy.AddComponent<ChargingMeleeBehaviourCreator>();
            Config config = CreateConfig(
                CreateAsset<ChargingMeleeEnemyBehaviourConfig>(),
                (AttackType.Melee, CreateAsset<MeleeAttackConfig>()));

            Assert.IsTrue(creator.TryCreate(config, out IEnemyBehaviour behaviour));
            Assert.IsInstanceOf<ChargingMeleeEnemyBehaviour>(behaviour);
        }

        [Test]
        public void RangedCreator_CreatesRangedBehaviour()
        {
            GameObject enemy = CreateBaseEnemy();
            enemy.AddComponent<LineOfSight>();
            enemy.AddComponent<GroundProjection>();
            enemy.AddComponent<ProjectileLauncher>();
            RangedBehaviourCreator creator = enemy.AddComponent<RangedBehaviourCreator>();
            Config config = CreateConfig(
                CreateAsset<RangedEnemyBehaviourConfig>(),
                (AttackType.StraightProjectile, CreateAsset<StraightProjectileAttackConfig>()),
                (AttackType.ParabolicProjectile, CreateAsset<ParabolicProjectileAttackConfig>()));

            Assert.IsTrue(creator.TryCreate(config, out IEnemyBehaviour behaviour));
            Assert.IsInstanceOf<RangedEnemyBehaviour>(behaviour);
        }

        [Test]
        public void HealerCreator_CreatesHealerBehaviourWithoutAttack()
        {
            GameObject enemy = CreateBaseEnemy(false);
            enemy.AddComponent<LineOfSight>();
            enemy.AddComponent<RPGame.Core.Statistics.StatisticsController>();
            enemy.AddComponent<RPGame.Core.Targeting.EnemyTargetable>();
            GameObject sensor = CreateObject("Sensor");
            sensor.transform.SetParent(enemy.transform);
            sensor.AddComponent<HealerTargetSensor>();
            HealerBehaviourCreator creator = enemy.AddComponent<HealerBehaviourCreator>();
            Config config = CreateConfig(CreateAsset<HealerEnemyBehaviourConfig>());

            Assert.IsTrue(creator.TryCreate(config, out IEnemyBehaviour behaviour));
            Assert.IsInstanceOf<HealerEnemyBehaviour>(behaviour);
        }

        [Test]
        public void AssassinCreator_CreatesAssassinBehaviour()
        {
            GameObject enemy = CreateBaseEnemy();
            enemy.AddComponent<LineOfSight>();
            enemy.AddComponent<BladeSweepMotion>();
            AssassinBehaviourCreator creator = enemy.AddComponent<AssassinBehaviourCreator>();
            Config config = CreateConfig(
                CreateAsset<AssassinEnemyBehaviourConfig>(),
                (AttackType.AssassinSlash, CreateAsset<AssassinSlashAttackConfig>()),
                (AttackType.StraightProjectile, CreateAsset<StraightProjectileAttackConfig>()));

            Assert.IsTrue(creator.TryCreate(config, out IEnemyBehaviour behaviour));
            Assert.IsInstanceOf<AssassinEnemyBehaviour>(behaviour);
        }

        [Test]
        public void Creator_WithWrongConfig_ReturnsFalse()
        {
            GameObject enemy = CreateBaseEnemy();
            MeleeBehaviourCreator creator = enemy.AddComponent<MeleeBehaviourCreator>();
            Config config = CreateConfig(CreateAsset<RangedEnemyBehaviourConfig>());

            LogAssert.Expect(LogType.Error, "MeleeBehaviourCreator requires MeleeEnemyBehaviourConfig.");

            Assert.IsFalse(creator.TryCreate(config, out IEnemyBehaviour behaviour));
            Assert.IsNull(behaviour);
        }

        [Test]
        public void MeleeCreator_WithoutAttack_ReturnsFalse()
        {
            GameObject enemy = CreateBaseEnemy(false);
            MeleeBehaviourCreator creator = enemy.AddComponent<MeleeBehaviourCreator>();
            Config config = CreateConfig(CreateAsset<MeleeEnemyBehaviourConfig>(), (AttackType.Melee, CreateAsset<MeleeAttackConfig>()));

            LogAssert.Expect(LogType.Error, "Missing field attack.");

            Assert.IsFalse(creator.TryCreate(config, out IEnemyBehaviour behaviour));
            Assert.IsNull(behaviour);
        }

        [TestCase("Assets/Enemies/Prefabs/SimpleMeleeEnemy/MeleeEnemy.prefab", typeof(MeleeBehaviourCreator))]
        [TestCase("Assets/Enemies/Prefabs/SimpleMeleeEnemy/FastMelee/FastMeleeEnemy.prefab", typeof(MeleeBehaviourCreator))]
        [TestCase("Assets/Enemies/Prefabs/SimpleMeleeEnemy/SlowMelee/SlowMeleeEnemy.prefab", typeof(MeleeBehaviourCreator))]
        [TestCase("Assets/Enemies/Prefabs/BruteEnemy/BruteEnemy.prefab", typeof(MeleeBehaviourCreator))]
        [TestCase("Assets/Enemies/Prefabs/SlowChargeMeleeEnemy/ChargingSlowEnemy.prefab", typeof(ChargingMeleeBehaviourCreator))]
        [TestCase("Assets/Enemies/Prefabs/FastChargeMeleeEnemy/ChargingFastEnemy.prefab", typeof(ChargingMeleeBehaviourCreator))]
        [TestCase("Assets/Enemies/Prefabs/SimpleRangedEnemy/RangedEnemy.prefab", typeof(RangedBehaviourCreator))]
        [TestCase("Assets/Enemies/Prefabs/HealerEnemy/HealerEnemy.prefab", typeof(HealerBehaviourCreator))]
        [TestCase("Assets/Enemies/Prefabs/AssassinEnemy/AssassinEnemy.prefab", typeof(AssassinBehaviourCreator))]
        public void Prefab_IsStandaloneAndAssignsOnlyExpectedBehaviourCreator(string path, Type creatorType)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Controller controller = prefab.GetComponent<Controller>();
            EnemyBehaviourCreator[] creators = prefab.GetComponents<EnemyBehaviourCreator>();
            SerializedObject serializedController = new(controller);
            EnemyBehaviourCreator creator = serializedController
                .FindProperty("behaviourCreator")
                .objectReferenceValue as EnemyBehaviourCreator;

            Assert.AreEqual(PrefabAssetType.Regular, PrefabUtility.GetPrefabAssetType(prefab));
            Assert.AreEqual(1, creators.Length);
            Assert.IsNotNull(creator);
            Assert.AreEqual(creatorType, creator.GetType());
            Assert.AreSame(creators[0], creator);

            if (creatorType == typeof(HealerBehaviourCreator))
            {
                Assert.IsNull(prefab.GetComponent<RangedBehaviourCreator>());
            }
        }

        private GameObject CreateBaseEnemy(bool addAttack = true)
        {
            GameObject enemy = CreateObject("Enemy");
            enemy.AddComponent<NavMeshAgent>();
            enemy.AddComponent<Detection>();
            enemy.AddComponent<Movement>();
            if (addAttack)
            {
                enemy.AddComponent<Attack>();
            }

            return enemy;
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdAssets.Add(asset);
            return asset;
        }

        private Config CreateConfig(
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
                SerializedProperty entry = attacksProperty.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("type").enumValueIndex = (int)attacks[i].Type;
                entry.FindPropertyRelative("config").objectReferenceValue = attacks[i].Config;
            }

            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static void SetKnockbackResistance(Config config, float resistance)
        {
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("knockbackResistance").floatValue = resistance;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float GetKnockbackResistance(Movement movement)
        {
            FieldInfo field = typeof(Movement).GetField("knockbackResistance", BindingFlags.Instance | BindingFlags.NonPublic);
            return (float)field.GetValue(movement);
        }
    }
}
