using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class EmpoweredSurgeBehaviorTests
    {
        private readonly List<Object> createdObjects = new();
        private GameObject caster;
        private GameObject target;
        private StatisticsController statistics;
        private StatusAggregator casterStatuses;
        private ManaSurgeStatusDefinition manaSurgeStatus;

        [SetUp]
        public void SetUp()
        {
            caster = CreateGameObject("Caster");
            statistics = caster.AddComponent<StatisticsController>();
            StatisticsConfig config = CreateObject<StatisticsConfig>();
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("maxMana").floatValue = 100f;
            serializedConfig.FindProperty("manaRegenerationPerSecond").floatValue = 5f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject serializedStatistics = new(statistics);
            serializedStatistics.FindProperty("config").objectReferenceValue = config;
            serializedStatistics.FindProperty("initializeOnAwake").boolValue = false;
            serializedStatistics.ApplyModifiedPropertiesWithoutUndo();
            statistics.ResetToConfig();

            casterStatuses = caster.AddComponent<StatusAggregator>();
            InvokeAwake(casterStatuses);
            target = CreateGameObject("Target");
            StatusAggregator targetStatuses = target.AddComponent<StatusAggregator>();
            InvokeAwake(targetStatuses);

            manaSurgeStatus = CreateObject<ManaSurgeStatusDefinition>();
            SetManaRegenerationPerStack(manaSurgeStatus, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        [TestCase(1, 6.5f)]
        [TestCase(2, 8f)]
        [TestCase(3, 9.5f)]
        [TestCase(4, 11f)]
        [TestCase(5, 12.5f)]
        public void EmpoweredSurge_IncreasesManaRegenerationForOneToFiveStacks(
            int activationCount,
            float expectedRegeneration)
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                includeManaSurge: true,
                includeEmpoweredSurge: true);

            for (int i = 0; i < activationCount; i++)
            {
                TriggerManaSurge(behaviors);
            }

            Assert.AreEqual(activationCount, GetManaSurge().CurrentStacks);
            Assert.AreEqual(expectedRegeneration, statistics.ManaRegenerationPerSecond, 0.0001f);
        }

        [Test]
        public void EmpoweredSurge_DoesNotModifyBaseManaRegeneration()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                includeManaSurge: true,
                includeEmpoweredSurge: true);

            Assert.AreEqual(5f, statistics.ManaRegenerationPerSecond);

            TriggerManaSurge(behaviors);
            Assert.AreEqual(6.5f, statistics.ManaRegenerationPerSecond, 0.0001f);

            casterStatuses.ClearStatuses();
            Assert.AreEqual(5f, statistics.ManaRegenerationPerSecond);
        }

        [Test]
        public void EmpoweredSurge_WithoutManaSurge_DoesNotApplyAStatus()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                includeManaSurge: false,
                includeEmpoweredSurge: true);

            TriggerManaSurge(behaviors);

            Assert.AreEqual(0, behaviors.Count);
            Assert.IsNull(GetManaSurge());
            Assert.AreEqual(5f, statistics.ManaRegenerationPerSecond);
        }

        [Test]
        public void EmpoweredSurge_AtStackLimit_RefreshesDurationWithoutIncreasingBonus()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                includeManaSurge: true,
                includeEmpoweredSurge: true);

            for (int i = 0; i < 5; i++)
            {
                TriggerManaSurge(behaviors);
            }

            TriggerManaSurge(behaviors);

            StatusInstance manaSurge = GetManaSurge();
            Assert.AreEqual(5, manaSurge.CurrentStacks);
            Assert.AreEqual(5f, manaSurge.RemainingDuration, 0.0001f);
            Assert.AreEqual(12.5f, statistics.ManaRegenerationPerSecond, 0.0001f);
        }

        [Test]
        public void EmpoweredSurge_IsolatedBetweenRuntimeBehaviorCreations()
        {
            EffectAggregator effects = CreateEffects(
                includeManaSurge: true,
                includeEmpoweredSurge: true);
            LightningZoneSpell spell = CreateObject<LightningZoneSpell>();
            IReadOnlyList<IRuntimeSpellBehavior> first = effects.CreateRuntimeBehaviors(spell, caster);
            IReadOnlyList<IRuntimeSpellBehavior> second = effects.CreateRuntimeBehaviors(spell, caster);

            TriggerManaSurge(first);
            Assert.AreEqual(6.5f, statistics.ManaRegenerationPerSecond, 0.0001f);

            casterStatuses.ClearStatuses();
            TriggerManaSurge(second);

            Assert.AreNotSame(GetManaSurgeBehavior(first), GetManaSurgeBehavior(second));
            Assert.AreEqual(6.5f, statistics.ManaRegenerationPerSecond, 0.0001f);
            Assert.AreEqual(1f, manaSurgeStatus.ManaRegenerationPerStack);
        }

        [Test]
        public void EmpoweredSurge_SupportsOnlyLightningZoneSpell()
        {
            EmpoweredSurgeEffectDefinition empoweredSurge = CreateObject<EmpoweredSurgeEffectDefinition>();
            LightningZoneSpell lightningZone = CreateObject<LightningZoneSpell>();
            EarthZoneSpell earthZone = CreateObject<EarthZoneSpell>();

            Assert.IsTrue(empoweredSurge.Supports(lightningZone));
            Assert.IsFalse(empoweredSurge.Supports(earthZone));
        }

        private IReadOnlyList<IRuntimeSpellBehavior> CreateRuntimeBehaviors(
            bool includeManaSurge,
            bool includeEmpoweredSurge)
        {
            EffectAggregator effects = CreateEffects(includeManaSurge, includeEmpoweredSurge);
            return effects.CreateRuntimeBehaviors(CreateObject<LightningZoneSpell>(), caster);
        }

        private EffectAggregator CreateEffects(bool includeManaSurge, bool includeEmpoweredSurge)
        {
            EffectAggregator effects = caster.AddComponent<EffectAggregator>();
            if (includeEmpoweredSurge)
            {
                effects.Add(CreateObject<EmpoweredSurgeEffectDefinition>());
            }

            if (includeManaSurge)
            {
                RuntimeBehaviorEffectDefinition manaSurge =
                    CreateObject<RuntimeBehaviorEffectDefinition>();
                SetBehavior(manaSurge, new ManaSurgeBehavior(manaSurgeStatus, 5f, caster));
                effects.Add(manaSurge);
            }

            return effects;
        }

        private void TriggerManaSurge(IReadOnlyList<IRuntimeSpellBehavior> behaviors)
        {
            CasterData casterData = new CasterDataBuilder(caster, caster.transform, target.transform)
                .WithSpellId(new SpellId("lightning_zone"))
                .WithRuntimeBehaviors(behaviors)
                .Build();

            SpellBehaviorPipelineExecutor.Resolve(casterData, target, new SuccessfulStunResolveBehavior());
        }

        private StatusInstance GetManaSurge()
        {
            for (int i = 0; i < casterStatuses.Statuses.Count; i++)
            {
                StatusInstance status = casterStatuses.Statuses[i];
                if (status.HasDefinition(manaSurgeStatus))
                {
                    return status;
                }
            }

            return null;
        }

        private static ManaSurgeBehavior GetManaSurgeBehavior(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors)
        {
            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is ManaSurgeBehavior manaSurge)
                {
                    return manaSurge;
                }
            }

            return null;
        }

        private T CreateObject<T>() where T : ScriptableObject
        {
            T createdObject = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(createdObject);
            return createdObject;
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetManaRegenerationPerStack(
            ManaSurgeStatusDefinition target,
            float manaRegenerationPerStack)
        {
            SerializedObject serializedStatus = new(target);
            serializedStatus.FindProperty("manaRegenerationPerStack").floatValue = manaRegenerationPerStack;
            serializedStatus.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBehavior(
            RuntimeBehaviorEffectDefinition target,
            IRuntimeSpellBehavior behavior)
        {
            SerializedObject serializedEffect = new(target);
            serializedEffect.FindProperty("behavior").managedReferenceValue = behavior;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokeAwake(StatusAggregator target)
        {
            typeof(StatusAggregator).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }

        private sealed class SuccessfulStunResolveBehavior : ISpellResolveBehavior
        {
            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;

            public bool Resolve(SpellBehaviorContext context)
            {
                context.SetExecutionFlag(SpellExecutionFlag.StunApplied);
                return true;
            }
        }
    }
}
