using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Movement;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Tests
{
    public sealed class MomentumBehaviorTests
    {
        private readonly List<GameObject> createdObjects = new();
        private readonly List<ScriptableObject> createdAssets = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            for (int i = createdAssets.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdAssets[i]);
            }

            createdObjects.Clear();
            createdAssets.Clear();
        }

        [Test]
        public void OnKnockbackCompleted_WithStatusReceiver_AppliesConfiguredSlow()
        {
            GameObject targetRoot = CreateObject("Target Root");
            GameObject target = CreateObject("Target");
            target.transform.SetParent(targetRoot.transform);
            TestStatusReceiver statusReceiver = targetRoot.AddComponent<TestStatusReceiver>();
            SlowStatusDefinition slowStatus = CreateSlowStatus();
            GameObject source = CreateObject("Source");
            MomentumBehavior behavior = new(slowStatus, 2.5f, source);

            behavior.OnKnockbackCompleted(target);

            Assert.AreEqual(1, statusReceiver.CallCount);
            Assert.AreSame(slowStatus, statusReceiver.LastStatus);
            Assert.AreEqual(2.5f, statusReceiver.LastDuration);
            Assert.AreSame(source, statusReceiver.LastContext.Source);
            Assert.AreEqual(nameof(MomentumBehavior), statusReceiver.LastContext.SourceId.ToString());
        }

        [Test]
        public void Dispatch_WhenKnockbackCompleted_ExecutesMomentum()
        {
            GameObject target = CreateObject("Target");
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            MomentumBehavior behavior = new(CreateSlowStatus(), 2.5f);
            List<IRuntimeSpellBehavior> behaviors = new() { behavior };

            KnockbackEndDispatcher.Dispatch(
                behaviors,
                target,
                new KnockbackEndContext(KnockbackEndReason.Completed));

            Assert.AreEqual(1, statusReceiver.CallCount);
        }

        [Test]
        public void Dispatch_WhenKnockbackCollides_DoesNotExecuteMomentum()
        {
            GameObject target = CreateObject("Target");
            Collider obstacle = CreateObject("Obstacle").AddComponent<BoxCollider>();
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            MomentumBehavior behavior = new(CreateSlowStatus(), 2.5f);
            List<IRuntimeSpellBehavior> behaviors = new() { behavior };

            KnockbackEndDispatcher.Dispatch(
                behaviors,
                target,
                new KnockbackEndContext(KnockbackEndReason.Collision, obstacle, Vector3.zero));

            Assert.AreEqual(0, statusReceiver.CallCount);
        }

        [Test]
        public void Dispatch_WhenKnockbackIsInterrupted_DoesNotExecuteMomentum()
        {
            GameObject target = CreateObject("Target");
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            MomentumBehavior behavior = new(CreateSlowStatus(), 2.5f);
            List<IRuntimeSpellBehavior> behaviors = new() { behavior };

            Assert.DoesNotThrow(() => KnockbackEndDispatcher.Dispatch(
                behaviors,
                target,
                new KnockbackEndContext(KnockbackEndReason.Interrupted)));

            Assert.AreEqual(0, statusReceiver.CallCount);
        }

        [Test]
        public void Dispatch_WhenKnockbackCollides_ExecutesOnlyCollisionHandlers()
        {
            GameObject target = CreateObject("Target");
            Collider obstacle = CreateObject("Obstacle").AddComponent<BoxCollider>();
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            TestCollisionHandler collisionHandler = new();
            MomentumBehavior completedHandler = new(CreateSlowStatus(), 2.5f);
            List<IRuntimeSpellBehavior> behaviors = new() { collisionHandler, completedHandler };

            KnockbackEndDispatcher.Dispatch(
                behaviors,
                target,
                new KnockbackEndContext(KnockbackEndReason.Collision, obstacle, Vector3.one));

            Assert.AreEqual(1, collisionHandler.CallCount);
            Assert.AreSame(obstacle, collisionHandler.LastObstacle);
            Assert.AreEqual(Vector3.one, collisionHandler.LastPoint);
            Assert.AreEqual(0, statusReceiver.CallCount);
        }

        [Test]
        public void Dispatch_WhenKnockbackCompletes_ExecutesOnlyCompletedHandlers()
        {
            GameObject target = CreateObject("Target");
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            TestCollisionHandler collisionHandler = new();
            MomentumBehavior completedHandler = new(CreateSlowStatus(), 2.5f);
            List<IRuntimeSpellBehavior> behaviors = new() { collisionHandler, completedHandler };

            KnockbackEndDispatcher.Dispatch(
                behaviors,
                target,
                new KnockbackEndContext(KnockbackEndReason.Completed));

            Assert.AreEqual(0, collisionHandler.CallCount);
            Assert.AreEqual(1, statusReceiver.CallCount);
        }

        [Test]
        public void Dispatch_WhenKnockbackIsInterrupted_ExecutesNoHandlers()
        {
            GameObject target = CreateObject("Target");
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            TestCollisionHandler collisionHandler = new();
            MomentumBehavior completedHandler = new(CreateSlowStatus(), 2.5f);
            List<IRuntimeSpellBehavior> behaviors = new() { collisionHandler, completedHandler };

            KnockbackEndDispatcher.Dispatch(
                behaviors,
                target,
                new KnockbackEndContext(KnockbackEndReason.Interrupted));

            Assert.AreEqual(0, collisionHandler.CallCount);
            Assert.AreEqual(0, statusReceiver.CallCount);
        }

        [Test]
        public void OnKnockbackCompleted_WithoutStatusReceiver_DoesNothing()
        {
            GameObject target = CreateObject("Target");
            MomentumBehavior behavior = new(CreateSlowStatus(), 2.5f);

            Assert.DoesNotThrow(() => behavior.OnKnockbackCompleted(target));
        }

        [Test]
        public void OnKnockbackCompleted_WithoutSlowStatus_DoesNothing()
        {
            GameObject target = CreateObject("Target");
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            MomentumBehavior behavior = new(null, 2.5f);

            behavior.OnKnockbackCompleted(target);

            Assert.AreEqual(0, statusReceiver.CallCount);
        }

        [Test]
        public void Supports_ForWaveSpell_ReturnsTrue()
        {
            MomentumBehavior behavior = new(CreateSlowStatus(), 2.5f);
            WaveSpell spell = CreateAsset<WaveSpell>();

            Assert.IsTrue(behavior.Supports(spell));
        }

        [Test]
        public void Supports_ForOtherSpell_ReturnsFalse()
        {
            MomentumBehavior behavior = new(CreateSlowStatus(), 2.5f);
            Spell otherSpell = CreateAsset<TestSpell>();

            Assert.IsFalse(behavior.Supports(otherSpell));
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private SlowStatusDefinition CreateSlowStatus()
        {
            return CreateAsset<SlowStatusDefinition>();
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdAssets.Add(asset);
            return asset;
        }

        private sealed class TestStatusReceiver : MonoBehaviour, IStatusReceiver
        {
            public int CallCount { get; private set; }
            public StatusDefinition LastStatus { get; private set; }
            public float LastDuration { get; private set; }
            public StatusContext LastContext { get; private set; }

            public bool ApplyStatus(StatusDefinition status, float duration, StatusContext context)
            {
                CallCount++;
                LastStatus = status;
                LastDuration = duration;
                LastContext = context;
                return true;
            }

            public bool HasStatus(StatusDefinition status)
            {
                return LastStatus == status;
            }

            public bool TryConsumeStatus(StatusDefinition status)
            {
                return LastStatus == status;
            }
        }

        private sealed class TestCollisionHandler : IKnockbackCollisionHandler
        {
            public int CallCount { get; private set; }
            public Collider LastObstacle { get; private set; }
            public Vector3 LastPoint { get; private set; }

            public void OnKnockbackCollision(GameObject target, Collider obstacle, Vector3 point)
            {
                CallCount++;
                LastObstacle = obstacle;
                LastPoint = point;
            }
        }

        private sealed class TestSpell : Spell
        {
            public override void OnCast(CasterData casterData)
            {
            }
        }
    }
}
