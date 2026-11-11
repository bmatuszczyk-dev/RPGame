using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class LingeringSurgeBehaviorTests
    {
        private readonly List<Object> createdObjects = new();
        private GameObject caster;
        private GameObject target;
        private StatusAggregator casterStatuses;
        private ManaSurgeStatusDefinition manaSurgeStatus;

        [SetUp]
        public void SetUp()
        {
            caster = CreateGameObject("Caster");
            casterStatuses = caster.AddComponent<StatusAggregator>();
            InvokeAwake(casterStatuses);
            target = CreateGameObject("Target");
            StatusAggregator targetStatuses = target.AddComponent<StatusAggregator>();
            InvokeAwake(targetStatuses);
            manaSurgeStatus = CreateObject<ManaSurgeStatusDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        [Test]
        public void LingeringSurge_ExtendsManaSurgeDurationToSevenPointFiveSeconds()
        {
            TriggerManaSurge(CreateRuntimeBehaviors(
                includeManaSurge: true,
                includeLingeringSurge: true));

            Assert.AreEqual(7.5f, GetManaSurge().RemainingDuration, 0.0001f);
            Assert.AreEqual(1f, GetManaSurge().Value, 0.0001f);
        }

        [Test]
        public void LingeringSurge_ReapplyAddsStackAndRefreshesDuration()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                includeManaSurge: true,
                includeLingeringSurge: true);
            TriggerManaSurge(behaviors);
            TickStatuses(2f);

            TriggerManaSurge(behaviors);

            Assert.AreEqual(2, GetManaSurge().CurrentStacks);
            Assert.AreEqual(7.5f, GetManaSurge().RemainingDuration, 0.0001f);
        }

        [Test]
        public void LingeringSurge_AtStackLimit_RefreshesDurationWithoutAddingAStack()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                includeManaSurge: true,
                includeLingeringSurge: true);
            for (int i = 0; i < 5; i++)
            {
                TriggerManaSurge(behaviors);
            }

            TickStatuses(2f);
            TriggerManaSurge(behaviors);

            Assert.AreEqual(5, GetManaSurge().CurrentStacks);
            Assert.AreEqual(7.5f, GetManaSurge().RemainingDuration, 0.0001f);
        }

        [Test]
        public void LingeringSurge_ExpiresAfterSevenPointFiveSeconds()
        {
            TriggerManaSurge(CreateRuntimeBehaviors(
                includeManaSurge: true,
                includeLingeringSurge: true));

            TickStatuses(7.5f);

            Assert.IsNull(GetManaSurge());
        }

        [Test]
        public void LingeringSurge_WithoutManaSurge_DoesNotApplyAStatus()
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviors(
                includeManaSurge: false,
                includeLingeringSurge: true);

            TriggerManaSurge(behaviors);

            Assert.AreEqual(0, behaviors.Count);
            Assert.IsNull(GetManaSurge());
        }

        [Test]
        public void LingeringSurge_CombinesWithEmpoweredSurge()
        {
            TriggerManaSurge(CreateRuntimeBehaviors(
                includeManaSurge: true,
                includeLingeringSurge: true,
                includeEmpoweredSurge: true));

            StatusInstance manaSurge = GetManaSurge();
            Assert.AreEqual(7.5f, manaSurge.RemainingDuration, 0.0001f);
            Assert.AreEqual(1.5f, manaSurge.Value, 0.0001f);
        }

        [Test]
        public void LingeringSurge_IsolatedBetweenRuntimeBehaviorCreations()
        {
            EffectAggregator effects = CreateEffects(
                includeManaSurge: true,
                includeLingeringSurge: true,
                includeEmpoweredSurge: false);
            LightningZoneSpell spell = CreateObject<LightningZoneSpell>();
            IReadOnlyList<IRuntimeSpellBehavior> first = effects.CreateRuntimeBehaviors(spell, caster);
            IReadOnlyList<IRuntimeSpellBehavior> second = effects.CreateRuntimeBehaviors(spell, caster);

            TriggerManaSurge(first);
            Assert.AreEqual(7.5f, GetManaSurge().RemainingDuration, 0.0001f);

            casterStatuses.ClearStatuses();
            TriggerManaSurge(second);

            Assert.AreNotSame(GetManaSurgeBehavior(first), GetManaSurgeBehavior(second));
            Assert.AreEqual(7.5f, GetManaSurge().RemainingDuration, 0.0001f);
            Assert.AreEqual(1f, manaSurgeStatus.ManaRegenerationPerStack);
        }

        [Test]
        public void LingeringSurge_DoesNotModifyTheRuntimeBehaviorTemplate()
        {
            EffectAggregator effects = caster.AddComponent<EffectAggregator>();
            effects.Add(CreateObject<LingeringSurgeEffectDefinition>());
            ManaSurgeBehavior template = new(manaSurgeStatus, 5f, caster);
            RuntimeBehaviorEffectDefinition manaSurge =
                CreateObject<RuntimeBehaviorEffectDefinition>();
            SetBehavior(manaSurge, template);
            effects.Add(manaSurge);

            effects.CreateRuntimeBehaviors(CreateObject<LightningZoneSpell>(), caster);

            Assert.AreEqual(5f, GetDuration(template), 0.0001f);
            Assert.AreEqual(1f, manaSurgeStatus.ManaRegenerationPerStack);
        }

        private IReadOnlyList<IRuntimeSpellBehavior> CreateRuntimeBehaviors(
            bool includeManaSurge,
            bool includeLingeringSurge,
            bool includeEmpoweredSurge = false)
        {
            EffectAggregator effects = CreateEffects(
                includeManaSurge,
                includeLingeringSurge,
                includeEmpoweredSurge);
            return effects.CreateRuntimeBehaviors(CreateObject<LightningZoneSpell>(), caster);
        }

        private EffectAggregator CreateEffects(
            bool includeManaSurge,
            bool includeLingeringSurge,
            bool includeEmpoweredSurge)
        {
            EffectAggregator effects = caster.AddComponent<EffectAggregator>();
            if (includeLingeringSurge)
            {
                effects.Add(CreateObject<LingeringSurgeEffectDefinition>());
            }

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

        private void TickStatuses(float deltaTime)
        {
            FieldInfo field = typeof(StatusAggregator).GetField(
                "statusContainer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            ((StatusContainer)field.GetValue(casterStatuses)).Tick(deltaTime);
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

        private static float GetDuration(ManaSurgeBehavior behavior)
        {
            FieldInfo field = typeof(ManaSurgeBehavior).GetField(
                "duration",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (float)field.GetValue(behavior);
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
