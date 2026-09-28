using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Damage;
using RPGame.Core.Movement;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Core.Tests.Statuses
{
    public sealed class StatusContainerTests
    {
        private GameObject firstSource;
        private GameObject secondSource;
        private TestMovement movement;
        private TestDamageable damageable;
        private StatusTarget target;

        [SetUp]
        public void SetUp()
        {
            firstSource = new GameObject("First Source");
            secondSource = new GameObject("Second Source");
            movement = new TestMovement();
            damageable = new TestDamageable();
            target = new StatusTarget(null, movement, damageable);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(firstSource);
            Object.DestroyImmediate(secondSource);
        }

        [Test]
        public void Add_WhenSameIdentityRefreshes_KeepsOneInstanceAndRefreshesDuration()
        {
            TestStatusDefinition definition = CreateTestStatus(ReapplyPolicy.Refresh, ConcurrentStatusPolicy.Independent);
            StatusContainer container = new(target);
            StatusContext context = new(new StatusSourceId("SlowZone"), firstSource);

            container.Add(definition, 1f, context);
            container.Tick(0.5f);
            container.Add(definition, 2f, context);

            Assert.AreEqual(1, container.Statuses.Count);
            Assert.AreEqual(2f, container.Statuses[0].RemainingDuration, 0.0001f);
            Assert.AreEqual(1, definition.RefreshCount);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Add_WhenSameIdentityIgnores_KeepsDuration()
        {
            TestStatusDefinition definition = CreateTestStatus(ReapplyPolicy.Ignore, ConcurrentStatusPolicy.Independent);
            StatusContainer container = new(target);
            StatusContext context = new(new StatusSourceId("StunSource"), firstSource);

            container.Add(definition, 3f, context);
            container.Tick(1f);
            container.Add(definition, 5f, context);

            Assert.AreEqual(1, container.Statuses.Count);
            Assert.AreEqual(2f, container.Statuses[0].RemainingDuration, 0.0001f);
            Assert.AreEqual(0, definition.RefreshCount);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Add_WhenSameIdentityStacks_AddsRemainingAmountAndDuration()
        {
            TestAmountStatusDefinition definition =
                ScriptableObject.CreateInstance<TestAmountStatusDefinition>();
            definition.AmountValue = 25f;
            StatusContainer container = new(target);
            StatusContext context = new(new StatusSourceId("HealthPotion"), firstSource);

            container.Add(definition, 5f, context);
            container.Tick(2f);

            StatusInstance instance = container.GetFirstStatus(definition);
            float remainingAmountBeforeReapply = instance.RemainingAmount;
            float remainingDurationBeforeReapply = instance.RemainingDuration;

            Assert.Less(remainingAmountBeforeReapply, 25f);
            Assert.Less(remainingDurationBeforeReapply, 5f);

            container.Add(definition, 5f, context);

            Assert.AreEqual(1, container.CountStatusInstances(definition));
            Assert.AreEqual(remainingAmountBeforeReapply + 25f, instance.RemainingAmount, 0.0001f);
            Assert.AreEqual(remainingDurationBeforeReapply + 5f, instance.RemainingDuration, 0.0001f);
            Assert.AreEqual(10f, instance.Duration, 0.0001f);
            Assert.AreEqual(StatusLifecycleEvent.Refreshed, instance.LastLifecycleEvent);
            Assert.AreEqual(1, definition.RefreshCount);

            container.Tick(1f);

            Assert.AreEqual(15f, definition.AppliedAmount, 0.0001f);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void RestoreResourceStatus_UsesStackReapplyPolicy()
        {
            RestoreHealthStatusDefinition definition =
                ScriptableObject.CreateInstance<RestoreHealthStatusDefinition>();

            Assert.AreEqual(ReapplyPolicy.Stack, definition.ReapplyPolicy);
            Assert.AreEqual(ConcurrentStatusPolicy.SingleInstance, definition.ConcurrentStatusPolicy);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Add_WhenIndependentSameDefinitionDifferentSourceId_CreatesTwoInstances()
        {
            TestStatusDefinition definition = CreateTestStatus(ReapplyPolicy.Refresh, ConcurrentStatusPolicy.Independent);
            StatusContainer container = new(target);

            container.Add(definition, 1f, new StatusContext(new StatusSourceId("SlowZone"), firstSource));
            container.Add(definition, 1f, new StatusContext(new StatusSourceId("FrostBolt"), firstSource));

            Assert.AreEqual(2, container.Statuses.Count);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Add_WhenIndependentSameDefinitionDifferentCaster_CreatesTwoInstances()
        {
            TestStatusDefinition definition = CreateTestStatus(ReapplyPolicy.Refresh, ConcurrentStatusPolicy.Independent);
            StatusContainer container = new(target);
            StatusSourceId sourceId = new("SlowZone");

            container.Add(definition, 1f, new StatusContext(sourceId, firstSource));
            container.Add(definition, 1f, new StatusContext(sourceId, secondSource));

            Assert.AreEqual(2, container.Statuses.Count);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Stun_WhenSameSourceReapplies_IgnoresDuration()
        {
            StunStatusDefinition stun = ScriptableObject.CreateInstance<StunStatusDefinition>();
            StatusContainer container = new(target);
            StatusContext context = new(
                new StatusSourceId(nameof(StatusContainerTests)),
                firstSource);

            container.Add(stun, 3f, context);
            container.Tick(1f);
            container.Add(stun, 5f, context);

            Assert.AreEqual(1, container.CountStatusInstances(stun));
            Assert.AreEqual(2f, container.GetFirstStatus(stun).RemainingDuration, 0.0001f);
            Object.DestroyImmediate(stun);
        }

        [Test]
        public void Stun_WhenDifferentSourceLonger_ReplacesRemainingWithIncomingDuration()
        {
            StunStatusDefinition stun = ScriptableObject.CreateInstance<StunStatusDefinition>();
            StatusContainer container = new(target);

            container.Add(stun, 2f, new StatusContext(new StatusSourceId("LightningZone"), firstSource));
            container.Tick(0.5f);
            container.Add(stun, 3f, new StatusContext(new StatusSourceId("ShockTrap"), firstSource));

            Assert.AreEqual(1, container.CountStatusInstances(stun));
            Assert.AreEqual(3f, container.GetFirstStatus(stun).RemainingDuration, 0.0001f);
            Object.DestroyImmediate(stun);
        }

        [Test]
        public void Stun_WhenDifferentSourceShorter_KeepsCurrentRemainingDuration()
        {
            StunStatusDefinition stun = ScriptableObject.CreateInstance<StunStatusDefinition>();
            StatusContainer container = new(target);

            container.Add(stun, 4f, new StatusContext(new StatusSourceId("LightningZone"), firstSource));
            container.Tick(0.5f);
            container.Add(stun, 1f, new StatusContext(new StatusSourceId("ShockTrap"), firstSource));

            Assert.AreEqual(1, container.CountStatusInstances(stun));
            Assert.AreEqual(3.5f, container.GetFirstStatus(stun).RemainingDuration, 0.0001f);
            Object.DestroyImmediate(stun);
        }

        [Test]
        public void Add_WhenContextOverridesReapplyPolicyToStack_AddsIncomingDurationToSingleInstance()
        {
            StunStatusDefinition stun = ScriptableObject.CreateInstance<StunStatusDefinition>();
            StatusContainer container = new(target);

            container.Add(stun, 2f, new StatusContext(new StatusSourceId("Concussion"), firstSource));
            container.Add(
                stun,
                3f,
                new StatusContext(
                    new StatusSourceId("HeavyConcussion"),
                    firstSource,
                    ReapplyPolicy.Stack));

            Assert.AreEqual(1, container.CountStatusInstances(stun));
            Assert.AreEqual(5f, container.GetFirstStatus(stun).RemainingDuration, 0.0001f);
            Object.DestroyImmediate(stun);
        }

        [Test]
        public void Slow_WhenTwoIndependentStatusesAreActive_MultipliesMovementModifiers()
        {
            SlowStatusDefinition slow20 = CreateSlowStatus(0.8f);
            SlowStatusDefinition slow40 = CreateSlowStatus(0.6f);
            StatusContainer container = new(target);

            container.Add(slow20, 2f, new StatusContext(new StatusSourceId("SlowZone"), firstSource));
            container.Add(slow40, 2f, new StatusContext(new StatusSourceId("FrostBolt"), firstSource));

            Assert.AreEqual(0.48f, movement.Multiplier, 0.0001f);
            Object.DestroyImmediate(slow20);
            Object.DestroyImmediate(slow40);
        }

        [Test]
        public void Burn_WhenTwoIndependentStatusesTick_DealsAdditiveDamage()
        {
            BurningStatusDefinition burn5 = CreateBurningStatus(5f);
            BurningStatusDefinition burn10 = CreateBurningStatus(10f);
            StatusContainer container = new(new StatusTarget(null, null, damageable));

            container.Add(burn5, 2f, new StatusContext(new StatusSourceId("FireZone"), firstSource));
            container.Add(burn10, 2f, new StatusContext(new StatusSourceId("FireProjectile"), firstSource));
            container.Tick(1f);

            Assert.AreEqual(15f, damageable.TotalDamage, 0.0001f);
            Object.DestroyImmediate(burn5);
            Object.DestroyImmediate(burn10);
        }

        [Test]
        public void TryConsumeStatus_WhenStatusExists_ReturnsTrueRemovesStatusAndRunsCleanup()
        {
            TestStatusDefinition definition = CreateTestStatus(ReapplyPolicy.Refresh, ConcurrentStatusPolicy.SingleInstance);
            StatusContainer container = new(target);
            StatusContext context = new(new StatusSourceId("Marked"), firstSource);

            container.Add(definition, 2f, context);
            bool consumed = container.TryConsumeStatus(definition);

            Assert.IsTrue(consumed);
            Assert.IsFalse(container.HasStatus(definition));
            Assert.AreEqual(1, definition.CleanupCount);
            Assert.AreEqual(StatusLifecycleEvent.Consumed, definition.LastRemoveEvent);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void TryConsumeStatus_WhenStatusDoesNotExist_ReturnsFalse()
        {
            TestStatusDefinition definition = CreateTestStatus(ReapplyPolicy.Refresh, ConcurrentStatusPolicy.SingleInstance);
            StatusContainer container = new(target);

            bool consumed = container.TryConsumeStatus(definition);

            Assert.IsFalse(consumed);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Tick_WhenStatusExpires_ReportsExpiredInsteadOfConsumed()
        {
            TestStatusDefinition definition = CreateTestStatus(ReapplyPolicy.Refresh, ConcurrentStatusPolicy.SingleInstance);
            StatusContainer container = new(target);

            container.Add(definition, 1f, new StatusContext(new StatusSourceId("Temporary"), firstSource));
            container.Tick(1f);

            Assert.AreEqual(StatusLifecycleEvent.Expired, definition.LastRemoveEvent);
            Assert.AreEqual(1, definition.CleanupCount);
            Object.DestroyImmediate(definition);
        }

        private static TestStatusDefinition CreateTestStatus(
            ReapplyPolicy reapplyPolicy,
            ConcurrentStatusPolicy concurrentStatusPolicy)
        {
            TestStatusDefinition definition = ScriptableObject.CreateInstance<TestStatusDefinition>();
            definition.Reapply = reapplyPolicy;
            definition.Concurrent = concurrentStatusPolicy;
            return definition;
        }

        private static SlowStatusDefinition CreateSlowStatus(float multiplier)
        {
            SlowStatusDefinition definition = ScriptableObject.CreateInstance<SlowStatusDefinition>();
            SerializedObject serializedDefinition = new(definition);
            serializedDefinition.FindProperty("movementSpeedMultiplier").floatValue = multiplier;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static BurningStatusDefinition CreateBurningStatus(float amount)
        {
            BurningStatusDefinition definition = ScriptableObject.CreateInstance<BurningStatusDefinition>();
            SerializedObject serializedDefinition = new(definition);
            serializedDefinition.FindProperty("amountPerInterval").floatValue = amount;
            serializedDefinition.FindProperty("tickInterval").floatValue = 1f;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private sealed class TestMovement : IMovement
        {
            private readonly Dictionary<int, float> modifiers = new();
            private int nextModifierId;

            public float Multiplier
            {
                get
                {
                    float multiplier = 1f;
                    foreach (float modifier in modifiers.Values)
                    {
                        multiplier *= modifier;
                    }

                    return multiplier;
                }
            }

            public void BlockMovement()
            {
            }

            public void UnblockMovement()
            {
            }

            public int AddMovementSpeedModifier(float multiplier)
            {
                nextModifierId++;
                modifiers.Add(nextModifierId, multiplier);
                return nextModifierId;
            }

            public void RemoveMovementSpeedModifier(int modifierId)
            {
                modifiers.Remove(modifierId);
            }
        }

        private sealed class TestDamageable : IDamageable
        {
            public bool CanReceiveDamage => true;
            public float TotalDamage { get; private set; }

            public DamageResult ApplyDamage(DamageData data)
            {
                TotalDamage += data.Amount;
                return DamageResult.Applied(data, data.Amount, 100f, 100f - data.Amount, false);
            }
        }
    }

    public sealed class TestStatusDefinition : StatusDefinition
    {
        public ReapplyPolicy Reapply { get; set; }
        public ConcurrentStatusPolicy Concurrent { get; set; }
        public int RefreshCount { get; private set; }
        public int CleanupCount { get; private set; }
        public StatusLifecycleEvent LastRemoveEvent { get; private set; }

        public override ReapplyPolicy ReapplyPolicy => Reapply;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => Concurrent;

        public override void OnApply(StatusTarget target, StatusInstance instance)
        {
            instance.RegisterCleanup(() => CleanupCount++);
        }

        public override void OnRefresh(StatusTarget target, StatusInstance instance)
        {
            RefreshCount++;
        }

        public override void OnRemove(
            StatusTarget target,
            StatusInstance instance,
            StatusLifecycleEvent lifecycleEvent)
        {
            LastRemoveEvent = lifecycleEvent;
        }

        public override string ToString()
        {
            return "Test Status";
        }
    }

    public sealed class TestAmountStatusDefinition : StatusDefinition, IAmountStatus
    {
        public float AmountValue { get; set; }
        public float AppliedAmount { get; private set; }
        public int RefreshCount { get; private set; }

        public float Amount => AmountValue;
        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Stack;

        public void Tick(StatusTarget target, float deltaTime, float amount)
        {
            AppliedAmount += amount;
        }

        public override void OnRefresh(StatusTarget target, StatusInstance instance)
        {
            RefreshCount++;
        }

        public override string ToString()
        {
            return "Test Amount Status";
        }
    }
}
