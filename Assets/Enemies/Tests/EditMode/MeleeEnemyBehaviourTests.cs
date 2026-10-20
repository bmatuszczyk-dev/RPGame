using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class MeleeEnemyBehaviourTests
    {
        [Test]
        public void Tick_WhenNoTarget_StopsAndStaysIdle()
        {
            FakeDetection detection = new();
            FakeMovement movement = new();
            FakeAttack attack = new();
            MeleeEnemyBehaviour behaviour = new(detection, movement, attack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(1, movement.StopCount);
            Assert.AreEqual(0, movement.MoveToCount);
            Assert.AreEqual(0, attack.TryAttackCount);
        }

        [Test]
        public void Tick_WhenNoTarget_MovesTowardPlayerSearchArea()
        {
            GameObject playerObject = new("PlayerTarget");
            playerObject.transform.position = new Vector3(10f, 0f, 0f);
            playerObject.AddComponent<PlayerTargetable>();

            try
            {
                FakeDetection detection = new();
                FakeMovement movement = new();
                MeleeEnemyBehaviour behaviour = new(detection, movement, new FakeAttack());

                behaviour.Tick(0.1f);

                Assert.AreEqual(1, movement.MoveToCount);
                Assert.LessOrEqual(Vector3.Distance(movement.LastDestination, playerObject.transform.position), 3f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Tick_WhenTargetIsDetected_UsesCombatMovementInsteadOfSearchMovement()
        {
            GameObject playerObject = new("PlayerTarget");
            playerObject.transform.position = new Vector3(10f, 0f, 0f);
            playerObject.AddComponent<PlayerTargetable>();

            try
            {
                FakeDetection detection = CreateDetectionWithTarget(new Vector3(3f, 0f, 0f));
                FakeMovement movement = new();
                FakeAttack attack = new();
                attack.IsTargetInRange = false;
                MeleeEnemyBehaviour behaviour = new(detection, movement, attack);

                behaviour.Tick(0.1f);

                Assert.AreEqual(new Vector3(3f, 0f, 0f), movement.LastDestination);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Tick_WhenTargetIsDetected_FacesTarget()
        {
            Vector3 targetPosition = new(3f, 0f, 0f);
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new();
            MeleeEnemyBehaviour behaviour = new(detection, movement, new FakeAttack());

            behaviour.Tick(0.1f);

            Assert.AreEqual(1, movement.FaceTowardsCount);
            Assert.AreEqual(targetPosition, movement.LastFaceTarget);
        }

        [Test]
        public void Tick_WhenTargetIsOutsideAttackRange_MovesToTarget()
        {
            FakeTargetable targetable = new();
            FakeDetection detection = new();
            detection.SetTarget(new SelectedTarget(targetable, new Vector3(3f, 0f, 0f)));
            FakeMovement movement = new();
            FakeAttack attack = new();
            attack.IsTargetInRange = false;
            MeleeEnemyBehaviour behaviour = new(detection, movement, attack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(1, movement.MoveToCount);
            Assert.AreEqual(new Vector3(3f, 0f, 0f), movement.LastDestination);
            Assert.AreEqual(0, movement.StopCount);
            Assert.AreEqual(0, attack.TryAttackCount);
        }

        [Test]
        public void Tick_WhenTargetIsInAttackRange_Stops()
        {
            FakeDetection detection = CreateDetectionWithTarget(Vector3.one);
            FakeMovement movement = new();
            FakeAttack attack = new();
            attack.IsTargetInRange = true;
            MeleeEnemyBehaviour behaviour = new(detection, movement, attack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(1, movement.StopCount);
            Assert.AreEqual(0, movement.MoveToCount);
        }

        [Test]
        public void Tick_WhenTargetIsInAttackRange_TriesAttack()
        {
            FakeDetection detection = CreateDetectionWithTarget(Vector3.one);
            FakeMovement movement = new();
            FakeAttack attack = new();
            attack.IsTargetInRange = true;
            MeleeEnemyBehaviour behaviour = new(detection, movement, attack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(1, attack.TryAttackCount);
            Assert.AreSame(detection.Target.Targetable, attack.LastAttackTarget.Targetable);
        }

        [Test]
        public void Tick_WhenTargetLeavesAttackRange_ChasesAgain()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(1f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack attack = new();
            attack.IsTargetInRange = true;
            MeleeEnemyBehaviour behaviour = new(detection, movement, attack);

            behaviour.Tick(0.1f);
            attack.IsTargetInRange = false;
            detection.SetTarget(new SelectedTarget(detection.Target.Targetable, new Vector3(2f, 0f, 0f)));
            behaviour.Tick(0.1f);

            Assert.AreEqual(1, movement.MoveToCount);
            Assert.AreEqual(new Vector3(2f, 0f, 0f), movement.LastDestination);
        }

        [Test]
        public void Tick_WhenTargetIsLost_ReturnsToIdle()
        {
            FakeDetection detection = CreateDetectionWithTarget(Vector3.one);
            FakeMovement movement = new();
            FakeAttack attack = new();
            attack.IsTargetInRange = false;
            MeleeEnemyBehaviour behaviour = new(detection, movement, attack);

            behaviour.Tick(0.1f);
            detection.ClearTarget();
            behaviour.Tick(0.1f);

            Assert.AreEqual(1, movement.MoveToCount);
            Assert.AreEqual(1, movement.StopCount);
        }

        [Test]
        public void MeleeEnemyBehaviour_DoesNotDependOnMonoBehaviour()
        {
            Assert.IsFalse(typeof(MeleeEnemyBehaviour).IsSubclassOf(typeof(MonoBehaviour)));

            bool hasMonoBehaviourField = typeof(MeleeEnemyBehaviour)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => typeof(MonoBehaviour).IsAssignableFrom(field.FieldType));

            Assert.IsFalse(hasMonoBehaviourField);
        }

        [Test]
        public void Controller_DoesNotContainMeleeDecisionLogic()
        {
            string controllerPath = Path.Combine(Application.dataPath, "Enemies", "Scripts", "Controller.cs");
            string source = File.ReadAllText(controllerPath);

            Assert.IsFalse(source.Contains("HasTarget", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("CurrentTarget", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("IsInRange", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("MoveTo(", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("TryAttack(", StringComparison.Ordinal));
        }

        [Test]
        public void MeleeBehaviour_MatchesExistingMeleeControllerFlow()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(4f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack attack = new();
            MeleeEnemyBehaviour behaviour = new(detection, movement, attack);

            attack.IsTargetInRange = false;
            behaviour.Tick(0.1f);

            attack.IsTargetInRange = true;
            detection.SetTarget(new SelectedTarget(detection.Target.Targetable, new Vector3(1f, 0f, 0f)));
            behaviour.Tick(0.1f);

            detection.ClearTarget();
            behaviour.Tick(0.1f);

            Assert.AreEqual(1, movement.MoveToCount);
            Assert.AreEqual(2, movement.StopCount);
            Assert.AreEqual(1, attack.TryAttackCount);
        }

        private static FakeDetection CreateDetectionWithTarget(Vector3 position)
        {
            FakeDetection detection = new();
            detection.SetTarget(new SelectedTarget(new FakeTargetable(), position));
            return detection;
        }

        private sealed class FakeDetection : IEnemyDetection
        {
            private bool hasTarget;

            public SelectedTarget Target { get; private set; }

            public bool TryGetTarget(out SelectedTarget target)
            {
                target = Target;
                return hasTarget;
            }

            public void SetTarget(SelectedTarget target)
            {
                Target = target;
                hasTarget = true;
            }

            public void ClearTarget()
            {
                Target = default;
                hasTarget = false;
            }
        }

        private sealed class FakeMovement : IEnemyMovement
        {
            public int MoveToCount { get; private set; }
            public int StopCount { get; private set; }
            public int FaceTowardsCount { get; private set; }
            public Vector3 LastDestination { get; private set; }
            public Vector3 Position { get; set; }
            public bool IsLeaping => false;
            public bool IsCharging => false;
            public bool IsMovementBlocked => false;
            public Vector3 LastFaceTarget { get; private set; }

            public void FaceTowards(Vector3 position)
            {
                FaceTowardsCount++;
                LastFaceTarget = position;
            }

            public void MoveTo(Vector3 position)
            {
                MoveToCount++;
                LastDestination = position;
            }

            public bool TryLeapTo(Vector3 destination, float speed, float arcHeight)
            {
                return false;
            }

            public bool TryCombatLeapTo(Vector3 destination, float speed, float arcHeight, float fallSpeed, Action<bool> onCompleted) => false;

            public bool TryStartCharge(
                Vector3 destination,
                float speed,
                float maxDistance,
                float knockbackResistance,
                Action<Collider, Vector3> onCollision)
            {
                return false;
            }

            public void Stop()
            {
                StopCount++;
            }

            public bool TryResolvePosition(Vector3 desiredPosition, out Vector3 validPosition)
            {
                validPosition = desiredPosition;
                return true;
            }
        }

        private sealed class FakeAttack : IEnemyAttack
        {
            public bool IsTargetInRange { get; set; }
            public int TryAttackCount { get; private set; }
            public int TickCount { get; private set; }
            public SelectedTarget LastAttackTarget { get; private set; }
            public float Range => 1f;

            public void Tick(float deltaTime)
            {
                TickCount++;
            }

            public bool IsInRange(SelectedTarget target)
            {
                return IsTargetInRange;
            }

            public bool TryAttack(SelectedTarget target)
            {
                TryAttackCount++;
                LastAttackTarget = target;
                return true;
            }
        }

        private sealed class FakeTargetable : ITargetable
        {
            public Transform TargetPoint => null;
        }
    }
}
