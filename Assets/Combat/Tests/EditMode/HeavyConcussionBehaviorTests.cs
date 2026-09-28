using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Movement;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Tests
{
    public sealed class HeavyConcussionBehaviorTests
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
        public void OnKnockbackCollision_WithConcussion_AddsHeavyDurationToSingleStun()
        {
            GameObject target = CreateObject("Target");
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            StunStatusDefinition stunStatus = CreateStunStatus();
            GameObject source = CreateObject("Source");
            ConcussionOnImpactCollisionBehavior concussion = new(stunStatus, 1f, source);
            HeavyConcussionBehavior heavyConcussion = new(stunStatus, 3f, source);

            concussion.OnKnockbackCollision(target, null, Vector3.zero);
            heavyConcussion.OnKnockbackCollision(target, null, Vector3.zero);

            Assert.AreEqual(1, statusReceiver.CountStatusInstances(stunStatus));
            StatusInstance stun = statusReceiver.GetFirstStatus(stunStatus);
            Assert.AreEqual(4f, stun.Duration);
            Assert.AreEqual(4f, stun.RemainingDuration);
            Assert.AreSame(source, stun.Source);
        }

        [Test]
        public void OnKnockbackCollision_WhenConcussionDurationIsLonger_AddsHeavyDuration()
        {
            GameObject target = CreateObject("Target");
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            StunStatusDefinition stunStatus = CreateStunStatus();
            GameObject source = CreateObject("Source");
            ConcussionOnImpactCollisionBehavior concussion = new(stunStatus, 5f, source);
            HeavyConcussionBehavior heavyConcussion = new(stunStatus, 3f, source);

            concussion.OnKnockbackCollision(target, null, Vector3.zero);
            heavyConcussion.OnKnockbackCollision(target, null, Vector3.zero);

            Assert.AreEqual(1, statusReceiver.CountStatusInstances(stunStatus));
            StatusInstance stun = statusReceiver.GetFirstStatus(stunStatus);
            Assert.AreEqual(8f, stun.Duration);
            Assert.AreEqual(8f, stun.RemainingDuration);
            Assert.AreSame(source, stun.Source);
        }

        [Test]
        public void OnKnockbackCollision_WithStatusReceiver_UsesCasterAsStatusSource()
        {
            GameObject target = CreateObject("Target");
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            StunStatusDefinition stunStatus = CreateStunStatus();
            GameObject source = CreateObject("Source");
            HeavyConcussionBehavior behavior = new(stunStatus, 3f, source);

            behavior.OnKnockbackCollision(target, null, Vector3.zero);

            StatusInstance stun = statusReceiver.GetFirstStatus(stunStatus);
            Assert.AreSame(source, stun.Source);
        }

        [Test]
        public void Supports_ForWaveSpell_ReturnsTrue()
        {
            HeavyConcussionBehavior behavior = new(CreateStunStatus(), 3f);
            WaveSpell spell = CreateAsset<WaveSpell>();

            Assert.IsTrue(behavior.Supports(spell));
        }

        [Test]
        public void Supports_ForOtherSpell_ReturnsFalse()
        {
            HeavyConcussionBehavior behavior = new(CreateStunStatus(), 3f);
            Spell otherSpell = CreateAsset<TestSpell>();

            Assert.IsFalse(behavior.Supports(otherSpell));
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private StunStatusDefinition CreateStunStatus()
        {
            return CreateAsset<StunStatusDefinition>();
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdAssets.Add(asset);
            return asset;
        }

        private sealed class TestStatusReceiver : MonoBehaviour, IStatusReceiver
        {
            private readonly StatusContainer container =
                new(new StatusTarget(null, new TestMovement(), null));

            public bool ApplyStatus(StatusDefinition status, float duration, StatusContext context)
            {
                return container.Add(status, duration, context);
            }

            public bool HasStatus(StatusDefinition status)
            {
                return container.HasStatus(status);
            }

            public bool TryConsumeStatus(StatusDefinition status)
            {
                return container.TryConsumeStatus(status);
            }

            public int CountStatusInstances(StatusDefinition status)
            {
                return container.CountStatusInstances(status);
            }

            public StatusInstance GetFirstStatus(StatusDefinition status)
            {
                return container.GetFirstStatus(status);
            }
        }

        private sealed class TestMovement : IMovement
        {
            public void BlockMovement()
            {
            }

            public void UnblockMovement()
            {
            }

            public int AddMovementSpeedModifier(float multiplier)
            {
                return 0;
            }

            public void RemoveMovementSpeedModifier(int modifierId)
            {
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
