using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Effects;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;

namespace RPGame.Core.Tests.Statuses
{
    public sealed class StatusStatModifierTests
    {
        private GameObject gameObject;
        private StatisticsConfig config;
        private StatisticsController statistics;
        private StatusAggregator statuses;
        private ManaSurgeStatusDefinition manaSurge;
        private readonly List<StatEffectDefinition> createdEffects = new();

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Status Stat Modifiers");
            statistics = gameObject.AddComponent<StatisticsController>();
            config = ScriptableObject.CreateInstance<StatisticsConfig>();
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("maxMana").floatValue = 100f;
            serializedConfig.FindProperty("manaRegenerationPerSecond").floatValue = 5f;
            serializedConfig.FindProperty("manaRegenerationDelay").floatValue = 0.2f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject serializedStatistics = new(statistics);
            serializedStatistics.FindProperty("config").objectReferenceValue = config;
            serializedStatistics.FindProperty("initializeOnAwake").boolValue = false;
            serializedStatistics.ApplyModifiedPropertiesWithoutUndo();
            statistics.ResetToConfig();

            statuses = gameObject.AddComponent<StatusAggregator>();
            InvokeAwake(statuses);
            manaSurge = ScriptableObject.CreateInstance<ManaSurgeStatusDefinition>();
            ConfigureManaSurge(1f, 5);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdEffects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdEffects[i]);
            }

            createdEffects.Clear();
            Object.DestroyImmediate(manaSurge);
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void ManaSurge_ReapplyStacksAndUpdatesCachedFlatModifier()
        {
            ApplyManaSurge();
            ApplyManaSurge();

            Assert.AreEqual(2, GetManaSurge().CurrentStacks);
            Assert.AreEqual(2f, statuses.GetEffectValue(EffectStat.ManaRegeneration, EffectModifierType.Flat));
            Assert.AreEqual(7f, statistics.ManaRegenerationPerSecond);
        }

        [Test]
        public void ManaRegeneration_CombinesPermanentAndStatusFlatAndPercentBonuses()
        {
            EffectAggregator effects = gameObject.AddComponent<EffectAggregator>();
            effects.Add(CreateStatEffect(EffectModifierType.Flat, 2f));
            effects.Add(CreateStatEffect(EffectModifierType.Percent, 0.1f));
            ApplyManaSurge();

            Assert.AreEqual(8.8f, statistics.ManaRegenerationPerSecond, 0.0001f);
        }

        [Test]
        public void ManaSurge_ClearStatuses_RemovesCachedModifier()
        {
            ApplyManaSurge();
            statuses.ClearStatuses();

            Assert.AreEqual(0f, statuses.GetEffectValue(EffectStat.ManaRegeneration, EffectModifierType.Flat));
            Assert.AreEqual(5f, statistics.ManaRegenerationPerSecond);
        }

        [Test]
        public void ManaRegeneration_RespectsExistingManaDelay()
        {
            ApplyManaSurge();
            statistics.TrySpendMana(20f);

            statistics.Tick(0.2f);
            Assert.AreEqual(80f, statistics.CurrentMana);

            statistics.Tick(1f);
            Assert.AreEqual(86f, statistics.CurrentMana);
        }

        private void ApplyManaSurge()
        {
            statuses.ApplyStatus(
                manaSurge,
                5f,
                new StatusContext(new StatusSourceId("ManaSurge"), gameObject));
        }

        private StatusInstance GetManaSurge()
        {
            for (int i = 0; i < statuses.Statuses.Count; i++)
            {
                StatusInstance status = statuses.Statuses[i];
                if (status.HasDefinition(manaSurge))
                {
                    return status;
                }
            }

            return null;
        }

        private void ConfigureManaSurge(float value, int maxStacks)
        {
            SerializedObject serializedStatus = new(manaSurge);
            serializedStatus.FindProperty("manaRegenerationPerStack").floatValue = value;
            serializedStatus.FindProperty("maxStacks").intValue = maxStacks;
            serializedStatus.ApplyModifiedPropertiesWithoutUndo();
        }

        private StatEffectDefinition CreateStatEffect(EffectModifierType modifierType, float value)
        {
            StatEffectDefinition effect = ScriptableObject.CreateInstance<StatEffectDefinition>();
            SerializedObject serializedEffect = new(effect);
            serializedEffect.FindProperty("stat").enumValueIndex = (int)EffectStat.ManaRegeneration;
            serializedEffect.FindProperty("modifierType").enumValueIndex = (int)modifierType;
            serializedEffect.FindProperty("value").floatValue = value;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            createdEffects.Add(effect);
            return effect;
        }

        private static void InvokeAwake(StatusAggregator target)
        {
            typeof(StatusAggregator).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }
    }
}
