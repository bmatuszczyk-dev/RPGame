using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Tests
{
    public sealed class ConcussionOnImpactCollisionBehaviorTests
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
        public void OnKnockbackCollision_WithStatusReceiver_AppliesConfiguredStun()
        {
            GameObject targetRoot = CreateObject("Target Root");
            GameObject target = CreateObject("Target");
            target.transform.SetParent(targetRoot.transform);
            TestStatusReceiver statusReceiver = targetRoot.AddComponent<TestStatusReceiver>();
            StunStatusDefinition stunStatus = CreateStunStatus();
            GameObject source = CreateObject("Source");
            ConcussionOnImpactCollisionBehavior behavior = new(stunStatus, 2.5f, source);

            behavior.OnKnockbackCollision(target, null, Vector3.zero);

            Assert.AreEqual(1, statusReceiver.CallCount);
            Assert.AreSame(stunStatus, statusReceiver.LastStatus);
            Assert.AreEqual(2.5f, statusReceiver.LastDuration);
            Assert.AreSame(source, statusReceiver.LastContext.Source);
            Assert.AreEqual(
                nameof(ConcussionOnImpactCollisionBehavior),
                statusReceiver.LastContext.SourceId.ToString());
        }

        [Test]
        public void OnKnockbackCollision_WithoutStatusReceiver_DoesNothing()
        {
            GameObject target = CreateObject("Target");
            ConcussionOnImpactCollisionBehavior behavior = new(CreateStunStatus(), 2.5f);

            Assert.DoesNotThrow(() => behavior.OnKnockbackCollision(target, null, Vector3.zero));
        }

        [Test]
        public void OnKnockbackCollision_WithoutStunStatus_DoesNothing()
        {
            GameObject target = CreateObject("Target");
            TestStatusReceiver statusReceiver = target.AddComponent<TestStatusReceiver>();
            ConcussionOnImpactCollisionBehavior behavior = new(null, 2.5f);

            behavior.OnKnockbackCollision(target, null, Vector3.zero);

            Assert.AreEqual(0, statusReceiver.CallCount);
        }

        [Test]
        public void Supports_ForWaveSpell_ReturnsTrue()
        {
            ConcussionOnImpactCollisionBehavior behavior = new(CreateStunStatus(), 2.5f);
            WaveSpell spell = CreateAsset<WaveSpell>();

            Assert.IsTrue(behavior.Supports(spell));
        }

        [Test]
        public void Supports_ForOtherSpell_ReturnsFalse()
        {
            ConcussionOnImpactCollisionBehavior behavior = new(CreateStunStatus(), 2.5f);
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

        private sealed class TestSpell : Spell
        {
            public override void OnCast(CasterData casterData)
            {
            }
        }
    }
}
