using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Effects;
using RPGame.Core.Statistics;
using RPGame.Core.Statistics.Attributes;
using UnityEditor;
using UnityEngine;

namespace RPGame.Core.Tests
{
    public sealed class StatisticsControllerTests
    {
        private GameObject gameObject;
        private StatisticsConfig config;
        private CharacterAttributesConfig attributesConfig;
        private StatisticsController controller;
        private EffectAggregator effects;
        private readonly List<StatEffectDefinition> effectDefinitions = new();

        [SetUp]
        public void SetUp()
        {
            config = CreateConfig(
                maxHealth: 100f,
                maxStamina: 50f,
                maxMana: 80f,
                healthRegenerationPerSecond: 5f,
                staminaRegenerationPerSecond: 10f,
                staminaRegenerationDelay: 0.5f,
                manaRegenerationPerSecond: 8f,
                manaRegenerationDelay: 0.5f);

            gameObject = new GameObject("StatisticsControllerTests");
            effects = gameObject.AddComponent<EffectAggregator>();
            controller = gameObject.AddComponent<StatisticsController>();
            SetControllerConfig(controller, config);
            controller.ResetToConfig();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(config);
            if (attributesConfig != null)
            {
                Object.DestroyImmediate(attributesConfig);
            }

            foreach (StatEffectDefinition definition in effectDefinitions)
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void ResetToConfig_SetsCurrentValuesToMax()
        {
            Assert.AreEqual(100f, controller.CurrentHealth);
            Assert.AreEqual(50f, controller.CurrentStamina);
            Assert.AreEqual(80f, controller.CurrentMana);
            Assert.AreEqual(1f, controller.HealthNormalized);
            Assert.AreEqual(1f, controller.StaminaNormalized);
            Assert.AreEqual(1f, controller.ManaNormalized);
        }

        [Test]
        public void MaxHealth_AddsFiveForEachVitalityPoint()
        {
            AddAttributes(vitality: 7, endurance: 0, intelligence: 0);
            controller.ResetToConfig();

            Assert.AreEqual(135f, controller.MaxHealth);
            Assert.AreEqual(135f, controller.CurrentHealth);
        }

        [Test]
        public void MaxStamina_AddsFiveForEachEndurancePoint()
        {
            AddAttributes(vitality: 0, endurance: 6, intelligence: 0);
            controller.ResetToConfig();

            Assert.AreEqual(80f, controller.MaxStamina);
            Assert.AreEqual(80f, controller.CurrentStamina);
        }

        [Test]
        public void MaxMana_AddsFiveForEachIntelligencePoint()
        {
            AddAttributes(vitality: 0, endurance: 0, intelligence: 4);
            controller.ResetToConfig();

            Assert.AreEqual(100f, controller.MaxMana);
            Assert.AreEqual(100f, controller.CurrentMana);
        }

        [Test]
        public void MaxMana_AddsFlatEffectBonus()
        {
            AddEffect(EffectStat.MaxMana, EffectModifierType.Flat, 50f);

            Assert.AreEqual(130f, controller.MaxMana);
        }

        [Test]
        public void MaxMana_AppliesPercentEffectBonus()
        {
            AddEffect(EffectStat.MaxMana, EffectModifierType.Percent, 0.2f);

            Assert.AreEqual(96f, controller.MaxMana);
        }

        [Test]
        public void MaxMana_AppliesIntelligenceFlatAndPercentInOrder()
        {
            AddAttributes(vitality: 0, endurance: 0, intelligence: 5);
            AddEffect(EffectStat.MaxMana, EffectModifierType.Flat, 50f);
            AddEffect(EffectStat.MaxMana, EffectModifierType.Percent, 0.2f);

            Assert.AreEqual(186f, controller.MaxMana);
        }

        [Test]
        public void MaxManaChange_DoesNotRestoreManaAndNotifiesHud()
        {
            controller.TrySpendMana(30f);
            int notificationCount = 0;
            float currentMana = 0f;
            float maxMana = 0f;
            controller.OnManaChanged += (current, max) =>
            {
                notificationCount++;
                currentMana = current;
                maxMana = max;
            };

            AddEffect(EffectStat.MaxMana, EffectModifierType.Flat, 50f);

            Assert.AreEqual(1, notificationCount);
            Assert.AreEqual(50f, currentMana);
            Assert.AreEqual(130f, maxMana);
            Assert.AreEqual(50f, controller.CurrentMana);
        }

        [Test]
        public void MaxManaChange_ClampsCurrentManaToNewMaximum()
        {
            AddEffect(EffectStat.MaxMana, EffectModifierType.Percent, -0.5f);

            Assert.AreEqual(40f, controller.MaxMana);
            Assert.AreEqual(40f, controller.CurrentMana);
        }

        [Test]
        public void TakeDamage_ReducesHealthAndClampsAtZero()
        {
            controller.TakeDamage(25f);
            Assert.AreEqual(75f, controller.CurrentHealth);

            controller.TakeDamage(500f);
            Assert.AreEqual(0f, controller.CurrentHealth);
            Assert.IsFalse(controller.IsAlive);
        }

        [Test]
        public void Heal_IncreasesHealthAndClampsAtMax()
        {
            controller.TakeDamage(40f);
            controller.Heal(15f);

            Assert.AreEqual(75f, controller.CurrentHealth);

            controller.Heal(500f);
            Assert.AreEqual(100f, controller.CurrentHealth);
        }

        [Test]
        public void TrySpendStamina_WhenEnoughStamina_ReducesStaminaAndReturnsTrue()
        {
            bool spent = controller.TrySpendStamina(20f);

            Assert.IsTrue(spent);
            Assert.AreEqual(30f, controller.CurrentStamina);
        }

        [Test]
        public void TrySpendStamina_WhenNotEnoughStamina_DoesNotChangeStaminaAndReturnsFalse()
        {
            bool spent = controller.TrySpendStamina(60f);

            Assert.IsFalse(spent);
            Assert.AreEqual(50f, controller.CurrentStamina);
        }

        [Test]
        public void TrySpendStamina_WhenAmountIsNotPositive_DoesNotChangeStaminaAndReturnsTrue()
        {
            bool spent = controller.TrySpendStamina(-10f);

            Assert.IsTrue(spent);
            Assert.AreEqual(50f, controller.CurrentStamina);
        }

        [Test]
        public void RestoreStamina_IncreasesStaminaAndClampsAtMax()
        {
            controller.TrySpendStamina(30f);
            controller.RestoreStamina(15f);

            Assert.AreEqual(35f, controller.CurrentStamina);

            controller.RestoreStamina(500f);
            Assert.AreEqual(50f, controller.CurrentStamina);
        }

        [Test]
        public void TrySpendMana_WhenEnoughMana_ReducesManaAndReturnsTrue()
        {
            bool spent = controller.TrySpendMana(25f);

            Assert.IsTrue(spent);
            Assert.AreEqual(55f, controller.CurrentMana);
        }

        [Test]
        public void TrySpendMana_WhenNotEnoughMana_DoesNotChangeManaAndReturnsFalse()
        {
            bool spent = controller.TrySpendMana(90f);

            Assert.IsFalse(spent);
            Assert.AreEqual(80f, controller.CurrentMana);
        }

        [Test]
        public void TrySpendMana_WhenAmountIsNotPositive_DoesNotChangeManaAndReturnsTrue()
        {
            bool spent = controller.TrySpendMana(-10f);

            Assert.IsTrue(spent);
            Assert.AreEqual(80f, controller.CurrentMana);
        }

        [Test]
        public void RestoreMana_IncreasesManaAndClampsAtMax()
        {
            controller.TrySpendMana(30f);
            controller.RestoreMana(15f);

            Assert.AreEqual(65f, controller.CurrentMana);

            controller.RestoreMana(500f);
            Assert.AreEqual(80f, controller.CurrentMana);
        }

        [Test]
        public void ManaRegeneration_RestoresManaOverTime()
        {
            controller.TrySpendMana(30f);

            controller.Tick(0.5f);
            controller.Tick(2f);

            Assert.AreEqual(66f, controller.CurrentMana);
        }

        [Test]
        public void ManaRegeneration_DoesNotExceedMaxMana()
        {
            controller.TrySpendMana(5f);

            controller.Tick(0.5f);
            controller.Tick(2f);

            Assert.AreEqual(80f, controller.CurrentMana);
        }

        [Test]
        public void TrySpendMana_DelaysManaRegeneration()
        {
            controller.TrySpendMana(20f);

            controller.Tick(0.25f);
            Assert.AreEqual(60f, controller.CurrentMana);

            controller.Tick(0.25f);
            Assert.AreEqual(60f, controller.CurrentMana);

            controller.Tick(1f);
            Assert.AreEqual(68f, controller.CurrentMana);
        }

        [Test]
        public void ManaChanged_IsRaisedWhenManaChanges()
        {
            int changedCount = 0;
            float currentMana = 0f;
            float maxMana = 0f;
            controller.OnManaChanged += (current, max) =>
            {
                changedCount++;
                currentMana = current;
                maxMana = max;
            };

            controller.TrySpendMana(20f);

            Assert.AreEqual(1, changedCount);
            Assert.AreEqual(60f, currentMana);
            Assert.AreEqual(80f, maxMana);
        }

        [Test]
        public void Died_IsRaisedOnceWhenHealthReachesZero()
        {
            int diedCount = 0;
            controller.Died += () => diedCount++;

            controller.TakeDamage(100f);
            controller.TakeDamage(100f);

            Assert.AreEqual(1, diedCount);
        }

        [Test]
        public void TrySpendStamina_DelaysStaminaRegeneration()
        {
            controller.TrySpendStamina(20f);

            controller.Tick(0.25f);
            Assert.AreEqual(30f, controller.CurrentStamina);

            controller.Tick(0.25f);
            Assert.AreEqual(30f, controller.CurrentStamina);

            controller.Tick(1f);
            Assert.AreEqual(40f, controller.CurrentStamina);
        }

        [Test]
        public void HealthRegeneration_RestoresHealthOverTime()
        {
            SetRegeneration(controller, regenerateHealth: true, regenerateStamina: false);

            controller.TakeDamage(20f);
            controller.Tick(2f);

            Assert.AreEqual(90f, controller.CurrentHealth);
        }

        private static StatisticsConfig CreateConfig(
            float maxHealth,
            float maxStamina,
            float maxMana,
            float healthRegenerationPerSecond,
            float staminaRegenerationPerSecond,
            float staminaRegenerationDelay,
            float manaRegenerationPerSecond,
            float manaRegenerationDelay)
        {
            StatisticsConfig statisticsConfig = ScriptableObject.CreateInstance<StatisticsConfig>();
            SerializedObject serializedConfig = new SerializedObject(statisticsConfig);
            serializedConfig.FindProperty("maxHealth").floatValue = maxHealth;
            serializedConfig.FindProperty("maxStamina").floatValue = maxStamina;
            serializedConfig.FindProperty("maxMana").floatValue = maxMana;
            serializedConfig.FindProperty("healthRegenerationPerSecond").floatValue = healthRegenerationPerSecond;
            serializedConfig.FindProperty("staminaRegenerationPerSecond").floatValue = staminaRegenerationPerSecond;
            serializedConfig.FindProperty("staminaRegenerationDelay").floatValue = staminaRegenerationDelay;
            serializedConfig.FindProperty("manaRegenerationPerSecond").floatValue = manaRegenerationPerSecond;
            serializedConfig.FindProperty("manaRegenerationDelay").floatValue = manaRegenerationDelay;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return statisticsConfig;
        }

        private void AddAttributes(int vitality, int endurance, int intelligence)
        {
            attributesConfig = ScriptableObject.CreateInstance<CharacterAttributesConfig>();
            SerializedObject serializedConfig = new SerializedObject(attributesConfig);
            serializedConfig.FindProperty("strength").intValue = 0;
            serializedConfig.FindProperty("dexterity").intValue = 0;
            serializedConfig.FindProperty("endurance").intValue = endurance;
            serializedConfig.FindProperty("vitality").intValue = vitality;
            serializedConfig.FindProperty("intelligence").intValue = intelligence;
            serializedConfig.FindProperty("power").intValue = 0;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            CharacterAttributes attributes = gameObject.AddComponent<CharacterAttributes>();
            SerializedObject serializedAttributes = new SerializedObject(attributes);
            serializedAttributes.FindProperty("config").objectReferenceValue = attributesConfig;
            serializedAttributes.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetControllerConfig(StatisticsController statisticsController, StatisticsConfig statisticsConfig)
        {
            SerializedObject serializedController = new SerializedObject(statisticsController);
            serializedController.FindProperty("config").objectReferenceValue = statisticsConfig;
            serializedController.FindProperty("initializeOnAwake").boolValue = false;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRegeneration(StatisticsController statisticsController, bool regenerateHealth, bool regenerateStamina)
        {
            SerializedObject serializedController = new SerializedObject(statisticsController);
            serializedController.FindProperty("regenerateHealth").boolValue = regenerateHealth;
            serializedController.FindProperty("regenerateStamina").boolValue = regenerateStamina;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static StatEffectDefinition CreateStatEffect(
            EffectStat stat,
            EffectModifierType modifierType,
            float value)
        {
            StatEffectDefinition definition = ScriptableObject.CreateInstance<StatEffectDefinition>();
            SerializedObject serialized = new(definition);
            serialized.FindProperty("stat").enumValueIndex = (int)stat;
            serialized.FindProperty("modifierType").enumValueIndex = (int)modifierType;
            serialized.FindProperty("value").floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private void AddEffect(EffectStat stat, EffectModifierType modifierType, float value)
        {
            StatEffectDefinition definition = CreateStatEffect(stat, modifierType, value);
            effectDefinitions.Add(definition);
            effects.Add(definition);
        }
    }
}
