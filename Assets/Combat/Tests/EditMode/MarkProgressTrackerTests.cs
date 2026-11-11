using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class MarkProgressTrackerTests
    {
        private GameObject source;
        private MarkStatusDefinition markDefinition;
        private TestStatusReceiver statusReceiver;
        private MarkProgressTracker tracker;
        private StatusContext context;

        [SetUp]
        public void SetUp()
        {
            source = new GameObject("Mark Source");
            markDefinition = ScriptableObject.CreateInstance<MarkStatusDefinition>();
            statusReceiver = new TestStatusReceiver();
            tracker = new MarkProgressTracker(statusReceiver, markDefinition);
            context = new StatusContext(new StatusSourceId("MarkProgress"), source);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(markDefinition);
        }

        [Test]
        public void RecordSuccessfulSpell_WhenThreeUniqueSpellsRecorded_CreatesMark()
        {
            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);
            tracker.RecordSuccessfulSpell(new SpellId("fire_zone"), context);

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void RecordSuccessfulSpell_WhenDuplicateSpellRecorded_DoesNotProgressTwice()
        {
            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            tracker.RecordSuccessfulSpell(new SpellId("fire_zone"), context);

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void RecordSuccessfulSpell_ExposesUniqueSpellProgressAndComboExpiry()
        {
            float time = 10f;
            tracker = new MarkProgressTracker(statusReceiver, markDefinition, () => time);

            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);

            Assert.AreEqual(1, tracker.CurrentUniqueSpellProgress);
            Assert.AreEqual(15f, tracker.ComboWindowExpiresAt, 0.0001f);

            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);

            Assert.AreEqual(2, tracker.CurrentUniqueSpellProgress);
        }

        [Test]
        public void ExpireIfNecessary_WhenComboWindowExpires_ClearsProgressAndNotifies()
        {
            float time = 0f;
            tracker = new MarkProgressTracker(statusReceiver, markDefinition, () => time);
            int changeCount = 0;
            int expirationCount = 0;
            tracker.ProgressChanged += () => changeCount++;
            tracker.ProgressExpired += () => expirationCount++;

            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            time = 5f;

            bool expired = tracker.ExpireIfNecessary(time, source);

            Assert.IsTrue(expired);
            Assert.AreEqual(0, tracker.CurrentUniqueSpellProgress);
            Assert.AreEqual(-1f, tracker.ComboWindowExpiresAt);
            Assert.AreEqual(2, changeCount);
            Assert.AreEqual(1, expirationCount);
        }

        [Test]
        public void RecordSuccessfulSpell_WhenSameSpellIdComesFromDifferentValueInstances_CountsOnce()
        {
            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            tracker.RecordSuccessfulSpell(new SpellId("fire_zone"), context);

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void RecordSuccessfulSpell_WhenMarkIsCreated_ClearsProgress()
        {
            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);
            tracker.RecordSuccessfulSpell(new SpellId("fire_zone"), context);

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
            Assert.IsTrue(statusReceiver.TryConsumeStatus(markDefinition));

            tracker.RecordSuccessfulSpell(new SpellId("earth_zone"), context);
            tracker.RecordSuccessfulSpell(new SpellId("lightning_zone"), context);

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            tracker.RecordSuccessfulSpell(new SpellId("magic_missile"), context);

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void RecordSuccessfulSpell_WhenMarkIsCreated_AppliesFiveSecondLifetime()
        {
            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);
            tracker.RecordSuccessfulSpell(new SpellId("fire_zone"), context);

            StatusInstance mark = statusReceiver.GetFirstStatus(markDefinition);
            Assert.AreEqual(5f, mark.Duration, 0.0001f);

            statusReceiver.Tick(5f);

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void TryConsumeStatus_WhenMarkExists_ConsumesItOnlyOnce()
        {
            statusReceiver.ApplyStatus(markDefinition, MarkProgressTracker.MarkDuration, context);

            bool firstConsume = statusReceiver.TryConsumeStatus(markDefinition);
            bool secondConsume = statusReceiver.TryConsumeStatus(markDefinition);

            Assert.IsTrue(firstConsume);
            Assert.IsFalse(secondConsume);
            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void RecordSuccessfulSpell_WhenMarkIsActive_DoesNotStartNextProgressCycle()
        {
            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);
            tracker.RecordSuccessfulSpell(new SpellId("fire_zone"), context);

            tracker.RecordSuccessfulSpell(new SpellId("earth_zone"), context);
            tracker.RecordSuccessfulSpell(new SpellId("lightning_zone"), context);

            statusReceiver.Tick(5f);
            tracker.RecordSuccessfulSpell(new SpellId("magic_missile"), context);

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        [Test]
        public void RecordSuccessfulSpell_WhenSpellIdIsInvalid_DoesNotProgress()
        {
            tracker.RecordSuccessfulSpell(new SpellId(null), context);
            tracker.RecordSuccessfulSpell(new SpellId(""), context);
            tracker.RecordSuccessfulSpell(new SpellId("   "), context);
            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));

            tracker.RecordSuccessfulSpell(new SpellId("fire_zone"), context);

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
        }

        private sealed class TestStatusReceiver : IStatusReceiver
        {
            private readonly StatusContainer container = new(new StatusTarget(null, null, null));

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

            public StatusInstance GetFirstStatus(StatusDefinition status)
            {
                return container.GetFirstStatus(status);
            }

            public void Tick(float deltaTime)
            {
                container.Tick(deltaTime);
            }
        }
    }
}
