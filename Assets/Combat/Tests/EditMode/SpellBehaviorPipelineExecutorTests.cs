using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class SpellBehaviorPipelineExecutorTests
    {
        private readonly List<Object> createdObjects = new();
        private MarkStatusDefinition markDefinition;
        private GameObject source;
        private GameObject target;
        private StatusAggregator statusReceiver;

        [SetUp]
        public void SetUp()
        {
            markDefinition = ScriptableObject.CreateInstance<MarkStatusDefinition>();
            createdObjects.Add(markDefinition);
            source = CreateGameObject("Source");
            target = CreateGameObject("Target");
            statusReceiver = target.AddComponent<StatusAggregator>();
            InvokeAwake(statusReceiver);
            MarkProgressReceiver progressReceiver = target.AddComponent<MarkProgressReceiver>();
            Configure(progressReceiver, markDefinition);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void Resolve_WhenSuccessful_UsesExpectedFlow()
        {
            List<string> executed = new();
            IRuntimeSpellBehavior[] behaviors =
            {
                new RecordingBehavior("Effect", SpellBehaviorPhase.Effect, executed),
                new RecordingBehavior("PreResolve", SpellBehaviorPhase.PreResolve, executed),
                new RecordingBehavior("MarkConsumption", SpellBehaviorPhase.MarkConsumption, executed)
            };
            RecordingResolveBehavior resolve = new("Resolve", executed, true);

            ExecuteSpell(new SpellId("wave"), behaviors, resolve);
            ExecuteSpell(new SpellId("orbit"));
            ExecuteSpell(new SpellId("fire_zone"));

            CollectionAssert.AreEqual(
                new[] { "PreResolve", "MarkConsumption", "Resolve", "Effect" },
                executed);
            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void Resolve_WhenResolveFails_DoesNotRunEffectOrMarkProgress()
        {
            List<string> executed = new();
            IRuntimeSpellBehavior[] behaviors =
            {
                new RecordingBehavior("PreResolve", SpellBehaviorPhase.PreResolve, executed),
                new RecordingBehavior("MarkConsumption", SpellBehaviorPhase.MarkConsumption, executed),
                new RecordingBehavior("Effect", SpellBehaviorPhase.Effect, executed)
            };
            RecordingResolveBehavior resolve = new("Resolve", executed, false);

            ExecuteSpell(new SpellId("wave"), behaviors, resolve);
            ExecuteSpell(new SpellId("orbit"));
            ExecuteSpell(new SpellId("fire_zone"));

            CollectionAssert.AreEqual(
                new[] { "PreResolve", "MarkConsumption", "Resolve" },
                executed);
            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            ExecuteSpell(new SpellId("magic_missile"));

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void Resolve_WhenThreeUniqueSpellsSucceed_CreatesMark()
        {
            ExecuteSpell(new SpellId("wave"));
            ExecuteSpell(new SpellId("orbit"));
            ExecuteSpell(new SpellId("fire_zone"));

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void Resolve_WhenSpellIsDuplicated_DoesNotProgressTwice()
        {
            ExecuteSpell(new SpellId("wave"));
            ExecuteSpell(new SpellId("wave"));
            ExecuteSpell(new SpellId("orbit"));

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            ExecuteSpell(new SpellId("fire_zone"));

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void Resolve_WhenMarkExists_ConsumesBeforeStartingNewProgress()
        {
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());
            ConsumeMarkBehavior consumeMark = new(markDefinition);

            ExecuteSpell(new SpellId("magic_missile"), new IRuntimeSpellBehavior[] { consumeMark });

            Assert.IsTrue(consumeMark.Results[0]);
            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            ExecuteSpell(new SpellId("wave"));
            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            ExecuteSpell(new SpellId("orbit"));
            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void Resolve_WhenThirdSpellCreatesMark_DoesNotConsumeNewMark()
        {
            ConsumeMarkBehavior consumeMark = new(markDefinition);

            ExecuteSpell(new SpellId("wave"), new IRuntimeSpellBehavior[] { consumeMark });
            ExecuteSpell(new SpellId("orbit"), new IRuntimeSpellBehavior[] { consumeMark });
            ExecuteSpell(new SpellId("fire_zone"), new IRuntimeSpellBehavior[] { consumeMark });

            CollectionAssert.AreEqual(new[] { false, false, false }, consumeMark.Results);
            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void Resolve_WhenTwoConsumptionBehaviorsRun_ConsumesMarkOnlyOnce()
        {
            statusReceiver.ApplyStatus(markDefinition, 5f, CreateStatusContext());
            ConsumeMarkBehavior firstConsume = new(markDefinition);
            ConsumeMarkBehavior secondConsume = new(markDefinition);

            ExecuteSpell(
                new SpellId("wave"),
                new IRuntimeSpellBehavior[] { firstConsume, secondConsume },
                new RecordingResolveBehavior("Resolve", new List<string>(), false));

            int successCount = 0;
            successCount += firstConsume.Results[0] ? 1 : 0;
            successCount += secondConsume.Results[0] ? 1 : 0;

            Assert.AreEqual(1, successCount);
            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void Resolve_WhenStatusApplicationFails_DoesNotRecordMarkProgress()
        {
            StatusDefinition rejectedStatus = ScriptableObject.CreateInstance<RejectingStatusDefinition>();
            createdObjects.Add(rejectedStatus);

            ExecuteSpell(
                new SpellId("wave"),
                new IRuntimeSpellBehavior[0],
                new StatusResolveBehavior(rejectedStatus, CreateStatusContext()));
            ExecuteSpell(new SpellId("orbit"));
            ExecuteSpell(new SpellId("fire_zone"));

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            ExecuteSpell(new SpellId("magic_missile"));

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void TriggerStatusZone_WhenChildColliderEnters_BehaviorReceivesReceiverGameObject()
        {
            List<GameObject> targets = new();
            TargetCaptureBehavior behavior = new(targets);
            GameObject zoneObject = CreateGameObject("Zone");
            TestTriggerStatusZoneBehaviour zone =
                zoneObject.AddComponent<TestTriggerStatusZoneBehaviour>();
            EnsureZoneComponents(zoneObject);
            CasterData casterData = CreateCasterData(
                new SpellId("fire_zone"),
                new IRuntimeSpellBehavior[] { behavior });
            zone.Initialize(casterData, 1f);
            zone.Activate();

            GameObject child = CreateGameObject("Child Collider");
            child.transform.SetParent(target.transform);
            BoxCollider childCollider = child.AddComponent<BoxCollider>();

            InvokeNonPublic(zone, "OnTriggerEnter", childCollider);

            Assert.AreSame(target, targets[0]);
        }

        [Test]
        public void TriggerStatusZone_WhenReapplying_DoesNotRunTargetBehaviorsOrMarkProgress()
        {
            List<string> executed = new();
            RecordingBehavior behavior = new(
                "MarkConsumption",
                SpellBehaviorPhase.MarkConsumption,
                executed);
            GameObject zoneObject = CreateGameObject("Zone");
            TestTriggerStatusZoneBehaviour zone =
                zoneObject.AddComponent<TestTriggerStatusZoneBehaviour>();
            EnsureZoneComponents(zoneObject);
            zone.Initialize(
                CreateCasterData(new SpellId("fire_zone"), new IRuntimeSpellBehavior[] { behavior }),
                1f);

            zone.ApplyInitialForTest(statusReceiver, target);
            zone.ReapplyForTest(statusReceiver);

            CollectionAssert.AreEqual(new[] { "MarkConsumption" }, executed);
            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
        }

        private void ExecuteSpell(SpellId spellId)
        {
            ExecuteSpell(spellId, new IRuntimeSpellBehavior[0]);
        }

        private void ExecuteSpell(
            SpellId spellId,
            IReadOnlyList<IRuntimeSpellBehavior> behaviors)
        {
            ExecuteSpell(
                spellId,
                behaviors,
                new RecordingResolveBehavior("Resolve", new List<string>(), true));
        }

        private void ExecuteSpell(
            SpellId spellId,
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            ISpellResolveBehavior resolveBehavior)
        {
            SpellBehaviorPipelineExecutor.Resolve(
                CreateCasterData(spellId, behaviors),
                target,
                resolveBehavior);
        }

        private CasterData CreateCasterData(
            SpellId spellId,
            IReadOnlyList<IRuntimeSpellBehavior> behaviors)
        {
            return new CasterDataBuilder(source, source.transform, target.transform)
                .WithSpellId(spellId)
                .WithRuntimeBehaviors(behaviors)
                .Build();
        }

        private StatusContext CreateStatusContext()
        {
            return new StatusContext(new StatusSourceId("Test"), source);
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void Configure(
            MarkProgressReceiver progressReceiver,
            MarkStatusDefinition markDefinition)
        {
            SerializedObject serializedReceiver = new(progressReceiver);
            serializedReceiver.FindProperty("markDefinition").objectReferenceValue = markDefinition;
            serializedReceiver.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureZoneComponents(GameObject zoneObject)
        {
            if (zoneObject.GetComponent<SphereCollider>() == null)
            {
                zoneObject.AddComponent<SphereCollider>();
            }

            if (zoneObject.GetComponent<Rigidbody>() == null)
            {
                zoneObject.AddComponent<Rigidbody>();
            }
        }

        private static void InvokeAwake(StatusAggregator statusReceiver)
        {
            MethodInfo awake = typeof(StatusAggregator).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(statusReceiver, null);
        }

        private static void InvokeNonPublic(
            object targetObject,
            string methodName,
            params object[] parameters)
        {
            MethodInfo method = null;
            System.Type type = targetObject.GetType();
            while (type != null && method == null)
            {
                method = type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            method.Invoke(targetObject, parameters);
        }

        private sealed class RecordingBehavior : ISpellBehavior
        {
            private readonly string name;
            private readonly List<string> executed;

            public RecordingBehavior(
                string name,
                SpellBehaviorPhase phase,
                List<string> executed)
            {
                this.name = name;
                Phase = phase;
                this.executed = executed;
            }

            public SpellBehaviorPhase Phase { get; }

            public void Execute(SpellBehaviorContext context)
            {
                executed.Add(name);
            }
        }

        private sealed class TargetCaptureBehavior : ISpellBehavior
        {
            private readonly List<GameObject> targets;

            public TargetCaptureBehavior(List<GameObject> targets)
            {
                this.targets = targets;
            }

            public SpellBehaviorPhase Phase => SpellBehaviorPhase.PreResolve;

            public void Execute(SpellBehaviorContext context)
            {
                targets.Add(context.Target);
            }
        }

        private sealed class ConsumeMarkBehavior : ISpellBehavior
        {
            private readonly MarkStatusDefinition markDefinition;

            public ConsumeMarkBehavior(MarkStatusDefinition markDefinition)
            {
                this.markDefinition = markDefinition;
            }

            public SpellBehaviorPhase Phase => SpellBehaviorPhase.MarkConsumption;
            public List<bool> Results { get; } = new();

            public void Execute(SpellBehaviorContext context)
            {
                Results.Add(context.StatusReceiver != null
                    && context.StatusReceiver.TryConsumeStatus(markDefinition));
            }
        }

        private sealed class RecordingResolveBehavior : ISpellResolveBehavior
        {
            private readonly string name;
            private readonly List<string> executed;
            private readonly bool success;

            public RecordingResolveBehavior(
                string name,
                List<string> executed,
                bool success)
            {
                this.name = name;
                this.executed = executed;
                this.success = success;
            }

            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;

            public bool Resolve(SpellBehaviorContext context)
            {
                executed.Add(name);
                return success;
            }
        }

        private sealed class StatusResolveBehavior : ISpellResolveBehavior
        {
            private readonly StatusDefinition status;
            private readonly StatusContext statusContext;

            public StatusResolveBehavior(
                StatusDefinition status,
                StatusContext statusContext)
            {
                this.status = status;
                this.statusContext = statusContext;
            }

            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;

            public bool Resolve(SpellBehaviorContext context)
            {
                return context.StatusReceiver != null
                    && context.StatusReceiver.ApplyStatus(status, 5f, statusContext);
            }
        }

        private sealed class RejectingStatusDefinition : StatusDefinition
        {
            public override bool CanApply(StatusTarget target)
            {
                return false;
            }

            public override string ToString()
            {
                return "Rejecting Status";
            }
        }

        private sealed class TestTriggerStatusZoneBehaviour : TriggerStatusZoneBehaviour
        {
            protected override float ReapplyInterval => 0.5f;

            public void ApplyInitialForTest(
                IStatusReceiver target,
                GameObject targetObject)
            {
                ApplyInitialTo(target, targetObject);
            }

            public void ReapplyForTest(IStatusReceiver target)
            {
                ReapplyTo(target);
            }

            protected override void ApplyInitialTo(
                IStatusReceiver target,
                GameObject targetObject)
            {
                SpellBehaviorPipelineExecutor.Resolve(
                    CasterData,
                    targetObject,
                    new RecordingResolveBehavior("Resolve", new List<string>(), true));
            }

            protected override void ReapplyTo(IStatusReceiver target)
            {
            }
        }
    }
}
