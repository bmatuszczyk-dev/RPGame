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
        public void StackAndRefresh_IncrementsOnlyStackableStatusesAndRespectsTheirLimit()
        {
            TestStackableStatusDefinition definition =
                ScriptableObject.CreateInstance<TestStackableStatusDefinition>();
            StatusContainer container = new(target);
            StatusContext context = new(new StatusSourceId("Stackable"), firstSource);

            container.Add(definition, 2f, context);
            container.Tick(1f);
            container.Add(definition, 3f, context.WithReapplyPolicyOverride(ReapplyPolicy.Refresh));

            Assert.AreEqual(1, container.GetFirstStatus(definition).CurrentStacks);
            Assert.AreEqual(3f, container.GetFirstStatus(definition).RemainingDuration, 0.0001f);

            container.Add(definition, 3f, context.WithReapplyPolicyOverride(ReapplyPolicy.StackAndRefresh));
            container.Add(definition, 3f, context.WithReapplyPolicyOverride(ReapplyPolicy.StackAndRefresh));

            Assert.AreEqual(2, container.GetFirstStatus(definition).CurrentStacks);
            Assert.AreEqual(3f, container.GetFirstStatus(definition).RemainingDuration, 0.0001f);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void RestoreResourceStatus_UsesStackReapplyPolicy()
        {
            RestoreHealthStatusDefinition definition =
                ScriptableObject.CreateInstance<RestoreHealthStatusDefinition>();

            Assert.AreEqual(ReapplyPolicy.Accumulate, definition.ReapplyPolicy);
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
                    ReapplyPolicy.Accumulate));

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
        public void Burn_FirstApplicationStartsAtIntensityOne()
        {
            BurningStatusDefinition burn = CreateBurningStatus(1.5f);
            StatusContainer container = new(new StatusTarget(null, null, damageable));

            container.Add(burn, 3f, new StatusContext(new StatusSourceId("FireZone"), firstSource));

            Assert.AreEqual(1, container.Statuses.Count);
            Assert.AreEqual(1, container.GetFirstStatus(burn).CurrentStacks);
            Object.DestroyImmediate(burn);
        }

        [Test]
        public void Burn_ReapplyWithoutIntensify_OnlyRefreshesDuration()
        {
            BurningStatusDefinition burn = CreateBurningStatus(1.5f);
            StatusContainer container = new(new StatusTarget(null, null, damageable));
            StatusContext context = new(new StatusSourceId("FireZone"), firstSource);

            container.Add(burn, 3f, context);
            container.Tick(1f);
            container.Add(burn, 3f, context);

            Assert.AreEqual(1, container.GetFirstStatus(burn).CurrentStacks);
            Assert.AreEqual(3f, container.GetFirstStatus(burn).RemainingDuration, 0.0001f);
            Object.DestroyImmediate(burn);
        }

        [Test]
        public void Burn_IntensifyStacksToFiveAndScalesPeriodicDamage()
        {
            BurningStatusDefinition burn = CreateBurningStatus(1.5f);
            StatusContainer container = new(new StatusTarget(null, null, damageable));

            container.Add(burn, 3f, new StatusContext(new StatusSourceId("FireZone"), firstSource));
            for (int i = 0; i < 5; i++)
            {
                container.Add(
                    burn,
                    3f,
                    new StatusContext(
                        new StatusSourceId("FireZone"),
                        secondSource,
                        ReapplyPolicy.StackAndRefresh));
            }

            container.Tick(1f);

            Assert.AreEqual(1, container.Statuses.Count);
            Assert.AreEqual(5, container.GetFirstStatus(burn).CurrentStacks);
            Assert.AreEqual(7.5f, damageable.TotalDamage, 0.0001f);
            Object.DestroyImmediate(burn);
        }

        [Test]
        public void Burn_AtMaximumIntensity_RefreshesAndExpiresBeforeNewApplication()
        {
            BurningStatusDefinition burn = CreateBurningStatus(1.5f);
            StatusContainer container = new(new StatusTarget(null, null, damageable));
            StatusContext intensified = new(
                new StatusSourceId("FireZone"),
                firstSource,
                ReapplyPolicy.StackAndRefresh);

            container.Add(burn, 3f, intensified);
            for (int i = 0; i < 5; i++)
            {
                container.Add(burn, 3f, intensified);
            }

            container.Tick(1f);
            container.Add(burn, 3f, intensified);
            Assert.AreEqual(5, container.GetFirstStatus(burn).CurrentStacks);
            Assert.AreEqual(3f, container.GetFirstStatus(burn).RemainingDuration, 0.0001f);

            container.Tick(3f);
            Assert.IsFalse(container.HasStatus(burn));
            container.Add(burn, 3f, intensified);
            Assert.AreEqual(1, container.GetFirstStatus(burn).CurrentStacks);
            Object.DestroyImmediate(burn);
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

        [Test]
        public void HandleOwnerDeath_InvokesActiveApplicationHandlerOnceAndResetsAfterClear()
        {
            TestStatusDefinition definition = CreateTestStatus(
                ReapplyPolicy.Refresh,
                ConcurrentStatusPolicy.SingleInstance);
            StatusContainer container = new(target);
            TestOwnerDeathHandler handler = new();
            StatusContext context = new(
                new StatusSourceId("Detonation"),
                firstSource,
                applicationData: handler);

            container.Add(definition, 2f, context);
            container.HandleOwnerDeath();
            container.HandleOwnerDeath();

            Assert.AreEqual(1, handler.InvocationCount);

            container.Clear();
            container.Add(definition, 2f, context);
            container.HandleOwnerDeath();

            Assert.AreEqual(2, handler.InvocationCount);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void HandleOwnerDeath_SkipsStatusRemovedByEarlierHandler()
        {
            TestStatusDefinition first = CreateTestStatus(
                ReapplyPolicy.Refresh,
                ConcurrentStatusPolicy.Independent);
            TestStatusDefinition second = CreateTestStatus(
                ReapplyPolicy.Refresh,
                ConcurrentStatusPolicy.Independent);
            StatusContainer container = new(target);
            TestOwnerDeathHandler secondHandler = new();
            TestOwnerDeathHandler firstHandler = new()
            {
                OnHandle = () => container.TryConsumeStatus(second)
            };

            container.Add(
                first,
                2f,
                new StatusContext(
                    new StatusSourceId("First"),
                    firstSource,
                    applicationData: firstHandler));
            container.Add(
                second,
                2f,
                new StatusContext(
                    new StatusSourceId("Second"),
                    secondSource,
                    applicationData: secondHandler));

            container.HandleOwnerDeath();

            Assert.AreEqual(1, firstHandler.InvocationCount);
            Assert.AreEqual(0, secondHandler.InvocationCount);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
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

        private static BurningStatusDefinition CreateBurningStatus(float damagePerSecond)
        {
            BurningStatusDefinition definition = ScriptableObject.CreateInstance<BurningStatusDefinition>();
            SerializedObject serializedDefinition = new(definition);
            serializedDefinition.FindProperty("baseDamagePerSecond").floatValue = damagePerSecond;
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

        public override void OnRefresh(
            StatusTarget target,
            StatusInstance instance,
            StatusContext context)
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
        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Accumulate;

        public void Tick(StatusTarget target, float deltaTime, float amount)
        {
            AppliedAmount += amount;
        }

        public override void OnRefresh(
            StatusTarget target,
            StatusInstance instance,
            StatusContext context)
        {
            RefreshCount++;
        }

        public override string ToString()
        {
            return "Test Amount Status";
        }
    }

    public sealed class TestStackableStatusDefinition : StatusDefinition, IStackableStatus
    {
        public int MaxStacks => 2;

        public override string ToString()
        {
            return "Test Stackable Status";
        }
    }

    public sealed class TestOwnerDeathHandler : IStatusOwnerDeathHandler
    {
        public int InvocationCount { get; private set; }
        public System.Action OnHandle { get; set; }

        public void HandleOwnerDied(StatusTarget target, StatusInstance instance)
        {
            InvocationCount++;
            OnHandle?.Invoke();
        }
    }
}
