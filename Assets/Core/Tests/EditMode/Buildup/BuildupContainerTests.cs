using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Buildup;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Core.Tests.Buildup
{
    public sealed class BuildupContainerTests
    {
        private readonly List<ScriptableObject> createdAssets = new();
        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdAssets.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdAssets[i]);
            }

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        [Test]
        public void Add_AccumulatesBuildup()
        {
            BuildupDefinition definition = CreateDefinition();
            BuildupContainer container = new(new FakeStatusReceiver());

            container.Add(definition, 30f, CreateContext("A"));
            container.Add(definition, 40f, CreateContext("B"));

            Assert.That(container.GetCurrentAmount(definition), Is.EqualTo(70f));
        }

        [Test]
        public void Add_UsesOneProgressPerDefinitionAcrossSources()
        {
            BuildupDefinition definition = CreateDefinition();
            BuildupContainer container = new(new FakeStatusReceiver());

            container.Add(definition, 30f, CreateContext("FirstSource"));
            container.Add(definition, 40f, CreateContext("SecondSource"));

            Assert.That(container.GetCurrentAmount(definition), Is.EqualTo(70f));
        }

        [Test]
        public void Add_AtThresholdAppliesStatusAndActivatesEffect()
        {
            StatusDefinition status = CreateStatus();
            BuildupDefinition definition = CreateDefinition(status);
            FakeStatusReceiver receiver = new();
            BuildupContainer container = new(receiver);

            container.Add(definition, 70f, CreateContext("First"));
            StatusContext thresholdContext = CreateContext("Threshold");
            container.Add(definition, 30f, thresholdContext);

            Assert.That(container.GetCurrentAmount(definition), Is.EqualTo(100f));
            Assert.That(container.ActiveBuildups[0].State, Is.EqualTo(BuildupState.EffectActive));
            Assert.That(receiver.AppliedStatuses.Count, Is.EqualTo(1));
            Assert.That(receiver.AppliedStatuses[0].Duration, Is.EqualTo(8f));
            Assert.That(receiver.AppliedStatuses[0].Context.SourceId, Is.EqualTo(thresholdContext.SourceId));
            Assert.That(receiver.AppliedStatuses[0].Context.Source, Is.EqualTo(thresholdContext.Source));
        }

        [Test]
        public void Add_AtThresholdDiscardsOverflow()
        {
            BuildupDefinition definition = CreateDefinition(CreateStatus());
            BuildupContainer container = new(new FakeStatusReceiver());

            container.Add(definition, 90f, CreateContext("First"));
            container.Add(definition, 25f, CreateContext("Second"));

            Assert.That(container.GetCurrentAmount(definition), Is.EqualTo(100f));
        }

        [Test]
        public void Tick_EffectActiveDecreasesLinearlyAndRemovesAtZero()
        {
            BuildupDefinition definition = CreateDefinition(CreateStatus(), triggeredStatusDuration: 8f);
            BuildupContainer container = new(new FakeStatusReceiver());
            container.Add(definition, 100f, CreateContext("Source"));

            container.Tick(2f);

            Assert.That(container.GetCurrentAmount(definition), Is.EqualTo(75f));
            Assert.That(container.ActiveBuildups[0].State, Is.EqualTo(BuildupState.EffectActive));

            container.Tick(6f);

            Assert.That(container.GetCurrentAmount(definition), Is.Zero);
            Assert.That(container.ActiveBuildups, Is.Empty);
        }

        [Test]
        public void Add_DuringEffectActiveIsIgnored()
        {
            BuildupDefinition definition = CreateDefinition(CreateStatus());
            FakeStatusReceiver receiver = new();
            BuildupContainer container = new(receiver);
            container.Add(definition, 100f, CreateContext("Trigger"));

            container.Add(definition, 50f, CreateContext("Ignored"));

            Assert.That(container.GetCurrentAmount(definition), Is.EqualTo(100f));
            Assert.That(receiver.AppliedStatuses.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_AfterEffectActiveEndsAccumulatesAgain()
        {
            StatusDefinition status = CreateStatus();
            BuildupDefinition definition = CreateDefinition(status, triggeredStatusDuration: 2f);
            FakeStatusReceiver receiver = new();
            BuildupContainer container = new(receiver);
            container.Add(definition, 100f, CreateContext("Trigger"));
            container.Tick(2f);
            receiver.ActiveStatuses.Remove(status);

            container.Add(definition, 30f, CreateContext("Next"));

            Assert.That(container.GetCurrentAmount(definition), Is.EqualTo(30f));
            Assert.That(container.ActiveBuildups[0].State, Is.EqualTo(BuildupState.Accumulating));
        }

        [Test]
        public void EffectActive_DoesNotAffectOtherDefinitions()
        {
            BuildupDefinition active = CreateDefinition(CreateStatus());
            BuildupDefinition accumulating = CreateDefinition(decayDelay: 0f, decayPerSecond: 10f);
            BuildupContainer container = new(new FakeStatusReceiver());
            container.Add(active, 100f, CreateContext("Active"));
            container.Add(accumulating, 30f, CreateContext("Accumulating"));

            container.Tick(1f);

            Assert.That(container.GetCurrentAmount(active), Is.EqualTo(87.5f));
            Assert.That(container.GetCurrentAmount(accumulating), Is.EqualTo(20f));
        }

        [Test]
        public void Add_WithZeroTriggeredStatusDurationAppliesStatusAndRemovesBuildup()
        {
            BuildupDefinition definition = CreateDefinition(CreateStatus(), triggeredStatusDuration: 0f);
            FakeStatusReceiver receiver = new();
            BuildupContainer container = new(receiver);

            container.Add(definition, 100f, CreateContext("Source"));

            Assert.That(receiver.AppliedStatuses.Count, Is.EqualTo(1));
            Assert.That(container.GetCurrentAmount(definition), Is.Zero);
            Assert.That(container.ActiveBuildups, Is.Empty);
        }

        [Test]
        public void Tick_DoesNotDecayBeforeDelayExpires()
        {
            BuildupDefinition definition = CreateDefinition(decayDelay: 3f, decayPerSecond: 10f);
            BuildupContainer container = new(new FakeStatusReceiver());
            container.Add(definition, 50f, CreateContext("Source"));

            container.Tick(2f);

            Assert.That(container.GetCurrentAmount(definition), Is.EqualTo(50f));
        }

        [Test]
        public void Tick_DecaysAfterDelay()
        {
            BuildupDefinition definition = CreateDefinition(decayDelay: 1f, decayPerSecond: 10f);
            BuildupContainer container = new(new FakeStatusReceiver());
            container.Add(definition, 50f, CreateContext("Source"));

            container.Tick(1f);
            container.Tick(2f);

            Assert.That(container.GetCurrentAmount(definition), Is.EqualTo(30f));
        }

        [Test]
        public void Tick_ClampsDecayAtZero()
        {
            BuildupDefinition definition = CreateDefinition(decayDelay: 0f, decayPerSecond: 10f);
            BuildupContainer container = new(new FakeStatusReceiver());
            container.Add(definition, 5f, CreateContext("Source"));

            container.Tick(1f);

            Assert.That(container.GetCurrentAmount(definition), Is.Zero);
        }

        [Test]
        public void Tick_DecaysDefinitionsIndependently()
        {
            BuildupDefinition first = CreateDefinition(decayDelay: 0f, decayPerSecond: 10f);
            BuildupDefinition second = CreateDefinition(decayDelay: 2f, decayPerSecond: 10f);
            BuildupContainer container = new(new FakeStatusReceiver());
            container.Add(first, 30f, CreateContext("Source"));
            container.Add(second, 30f, CreateContext("Source"));

            container.Tick(1f);

            Assert.That(container.GetCurrentAmount(first), Is.EqualTo(20f));
            Assert.That(container.GetCurrentAmount(second), Is.EqualTo(30f));
        }

        [Test]
        public void Add_WhileTriggeredStatusIsActive_IgnoresBuildup()
        {
            StatusDefinition status = CreateStatus();
            BuildupDefinition definition = CreateDefinition(status);
            FakeStatusReceiver receiver = new();
            BuildupContainer container = new(receiver);
            container.Add(definition, 30f, CreateContext("BeforeStatus"));
            receiver.ActiveStatuses.Add(status);

            container.Add(definition, 30f, CreateContext("Source"));

            Assert.That(container.GetCurrentAmount(definition), Is.Zero);
            receiver.ActiveStatuses.Remove(status);
            container.Add(definition, 30f, CreateContext("Source"));
            Assert.That(container.GetCurrentAmount(definition), Is.EqualTo(30f));
        }

        [Test]
        public void Clear_RemovesAllProgressRegardlessOfState()
        {
            BuildupDefinition first = CreateDefinition(CreateStatus());
            BuildupDefinition second = CreateDefinition();
            BuildupContainer container = new(new FakeStatusReceiver());
            container.Add(first, 100f, CreateContext("Source"));
            container.Add(second, 40f, CreateContext("Source"));

            container.Clear();

            Assert.That(container.GetCurrentAmount(first), Is.Zero);
            Assert.That(container.GetCurrentAmount(second), Is.Zero);
        }

        private BuildupDefinition CreateDefinition(
            StatusDefinition status = null,
            float decayDelay = 3f,
            float decayPerSecond = 10f,
            float triggeredStatusDuration = 8f)
        {
            BuildupDefinition definition = ScriptableObject.CreateInstance<BuildupDefinition>();
            createdAssets.Add(definition);
            SerializedObject serializedDefinition = new(definition);
            serializedDefinition.FindProperty("threshold").floatValue = 100f;
            serializedDefinition.FindProperty("decayDelay").floatValue = decayDelay;
            serializedDefinition.FindProperty("decayPerSecond").floatValue = decayPerSecond;
            serializedDefinition.FindProperty("triggeredStatus").objectReferenceValue = status;
            serializedDefinition.FindProperty("triggeredStatusDuration").floatValue = triggeredStatusDuration;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private StatusDefinition CreateStatus()
        {
            TestBuildupStatusDefinition status = ScriptableObject.CreateInstance<TestBuildupStatusDefinition>();
            createdAssets.Add(status);
            return status;
        }

        private StatusContext CreateContext(string sourceId)
        {
            GameObject source = new(sourceId);
            createdObjects.Add(source);
            return new StatusContext(new StatusSourceId(sourceId), source);
        }

        private sealed class FakeStatusReceiver : IStatusReceiver
        {
            public HashSet<StatusDefinition> ActiveStatuses { get; } = new();
            public List<AppliedStatus> AppliedStatuses { get; } = new();

            public bool ApplyStatus(StatusDefinition status, float duration, StatusContext context)
            {
                AppliedStatuses.Add(new AppliedStatus(status, duration, context));
                ActiveStatuses.Add(status);
                return true;
            }

            public bool HasStatus(StatusDefinition status) => ActiveStatuses.Contains(status);
            public bool TryConsumeStatus(StatusDefinition status) => ActiveStatuses.Remove(status);
        }

        private readonly struct AppliedStatus
        {
            public AppliedStatus(StatusDefinition definition, float duration, StatusContext context)
            {
                Definition = definition;
                Duration = duration;
                Context = context;
            }

            public StatusDefinition Definition { get; }
            public float Duration { get; }
            public StatusContext Context { get; }
        }

        private sealed class TestBuildupStatusDefinition : StatusDefinition
        {
            public override string ToString() => "Test Buildup Status";
        }
    }
}
