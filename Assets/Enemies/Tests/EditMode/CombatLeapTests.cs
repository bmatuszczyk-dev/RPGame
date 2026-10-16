using NUnit.Framework;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class CombatLeapTests
    {
        private AssassinEnemyBehaviourConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<AssassinEnemyBehaviourConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [TestCase(2f, false)]
        [TestCase(3f, true)]
        [TestCase(9f, true)]
        [TestCase(10f, false)]
        public void CanStart_RequiresTargetWithinConfiguredDistance(float distance, bool expected)
        {
            CombatLeap leap = new(config, new FakeMovement());

            Assert.That(leap.CanStart(distance), Is.EqualTo(expected));
        }

        [Test]
        public void TryStart_UsesTargetPositionAtStartAndStartsCooldownAfterSuccess()
        {
            FakeMovement movement = new() { Position = Vector3.zero };
            CombatLeap leap = new(config, movement);
            Vector3 targetPosition = new(5f, 0f, 2f);

            bool started = leap.TryStart(targetPosition, 0f);

            Assert.IsTrue(started);
            Assert.That(movement.CombatLeapDestination, Is.EqualTo(targetPosition));
            Assert.IsFalse(leap.CanStart(5f));
            Assert.IsFalse(leap.LeapFinished);
        }

        [Test]
        public void CompleteCombatLeap_SetsLeapFinished()
        {
            FakeMovement movement = new() { Position = Vector3.zero };
            CombatLeap leap = new(config, movement);

            leap.TryStart(new Vector3(5f, 0f, 0f), 0f);
            movement.CompleteCombatLeap(true);

            Assert.IsTrue(leap.LeapFinished);
        }

        [Test]
        public void InterruptedCombatLeap_DoesNotSetLeapFinished()
        {
            FakeMovement movement = new() { Position = Vector3.zero };
            CombatLeap leap = new(config, movement);

            leap.TryStart(new Vector3(5f, 0f, 0f), 0f);
            movement.CompleteCombatLeap(false);

            Assert.IsFalse(leap.LeapFinished);
        }

        [Test]
        public void TryStart_WhenMovementRejectsLeap_DoesNotStartCooldown()
        {
            FakeMovement movement = new() { Position = Vector3.zero, CanStartCombatLeap = false };
            CombatLeap leap = new(config, movement);

            bool started = leap.TryStart(new Vector3(5f, 0f, 0f), 0f);

            Assert.IsFalse(started);
            Assert.IsTrue(leap.CanStart(5f));
        }

        [Test]
        public void Tick_AfterCooldownExpires_AllowsAnotherLeap()
        {
            FakeMovement movement = new() { Position = Vector3.zero };
            CombatLeap leap = new(config, movement);
            leap.TryStart(new Vector3(5f, 0f, 0f), 0f);

            leap.Tick(config.CombatLeapCooldown);

            Assert.IsTrue(leap.CanStart(5f));
        }

        private sealed class FakeMovement : IEnemyMovement
        {
            public Vector3 Position { get; set; }
            public bool IsLeaping => false;
            public bool IsCharging => false;
            public bool IsMovementBlocked => false;
            public bool CanStartCombatLeap { get; set; } = true;
            public Vector3 CombatLeapDestination { get; private set; }
            private System.Action<bool> combatLeapCompleted;

            public void FaceTowards(Vector3 position) { }
            public void MoveTo(Vector3 position) { }
            public void Stop() { }
            public bool TryResolvePosition(Vector3 desiredPosition, out Vector3 validPosition)
            {
                validPosition = desiredPosition;
                return true;
            }

            public bool TryLeapTo(Vector3 destination, float speed, float arcHeight) => false;
            public bool TryCombatLeapTo(
                Vector3 destination,
                float speed,
                float arcHeight,
                float fallSpeed,
                System.Action<bool> onCompleted)
            {
                CombatLeapDestination = destination;
                combatLeapCompleted = onCompleted;
                return CanStartCombatLeap;
            }

            public void CompleteCombatLeap(bool completed)
            {
                combatLeapCompleted?.Invoke(completed);
            }

            public bool TryStartCharge(
                Vector3 destination,
                float speed,
                float maxDistance,
                float knockbackResistance,
                System.Action<Collider, Vector3> onCollision) => false;
        }
    }
}
