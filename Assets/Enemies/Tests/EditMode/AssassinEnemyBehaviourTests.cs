using System;
using NUnit.Framework;
using RPGame.Core.Targeting;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Enemies.Tests
{
    public sealed class AssassinEnemyBehaviourTests
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

        [Test]
        public void Tick_WithoutTarget_UsesSearchMovement()
        {
            FakeMovement movement = new();
            AssassinEnemyBehaviour behaviour = CreateBehaviour(new FakeDetection(), movement, out _, out _);

            behaviour.Tick(0.1f);

            Assert.That(movement.StopCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_DuringLeap_DoesNotIssueAttacksOrMovementCommands()
        {
            FakeMovement movement = new() { IsLeapingValue = true };
            FakeDetection detection = new() { Target = CreateTarget(new Vector3(5f, 0f, 0f)) };
            AssassinEnemyBehaviour behaviour = CreateBehaviour(detection, movement, out FakeAttack melee, out FakeAttack ranged);

            behaviour.Tick(0.1f);

            Assert.That(melee.AttackCount, Is.Zero);
            Assert.That(ranged.AttackCount, Is.Zero);
            Assert.That(movement.CommandCount, Is.Zero);
        }

        [Test]
        public void Tick_WhenMovementBlocked_DoesNotIssueNewActions()
        {
            FakeMovement movement = new() { IsMovementBlockedValue = true };
            FakeDetection detection = new() { Target = CreateTarget(new Vector3(5f, 0f, 0f)) };
            AssassinEnemyBehaviour behaviour = CreateBehaviour(detection, movement, out FakeAttack melee, out FakeAttack ranged);

            behaviour.Tick(0.1f);

            Assert.That(melee.AttackCount, Is.Zero);
            Assert.That(ranged.AttackCount, Is.Zero);
            Assert.That(movement.CommandCount, Is.Zero);
        }

        [Test]
        public void Tick_InMeleeRangeAfterCompletedCombatLeap_PrioritizesMelee()
        {
            FakeMovement movement = new();
            FakeAttack melee = new() { IsInRangeValue = true };
            FakeAttack ranged = new();
            FakeDetection detection = new() { Target = CreateTarget(new Vector3(5f, 0f, 0f)) };
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                detection,
                movement,
                melee,
                ranged);

            behaviour.Tick(0f);
            movement.CompleteCombatLeap(true);
            detection.Target = CreateTarget(Vector3.forward);
            behaviour.Tick(0f);

            Assert.That(melee.AttackCount, Is.EqualTo(1));
            Assert.That(ranged.AttackCount, Is.Zero);
            Assert.That(movement.CombatLeapCount, Is.Zero);
        }

        [Test]
        public void Tick_AfterInterruptedCombatLeap_DoesNotUseMelee()
        {
            FakeMovement movement = new();
            FakeAttack melee = new() { IsInRangeValue = true };
            FakeDetection detection = new() { Target = CreateTarget(new Vector3(5f, 0f, 0f)) };
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                detection,
                movement,
                melee,
                new FakeAttack());

            behaviour.Tick(0f);
            movement.CompleteCombatLeap(false);
            detection.Target = CreateTarget(Vector3.forward);
            behaviour.Tick(0f);

            Assert.That(melee.AttackCount, Is.Zero);
        }

        [Test]
        public void Tick_TooCloseBeforeCombatLeap_MovesOutwardWithoutMelee()
        {
            FakeMovement movement = new();
            FakeAttack melee = new() { IsInRangeValue = true };
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                new FakeDetection() { Target = CreateTarget(Vector3.forward) },
                movement,
                melee,
                new FakeAttack());

            behaviour.Tick(0.1f);

            Assert.That(melee.AttackCount, Is.Zero);
            Assert.That(movement.LastMoveDestination, Is.EqualTo(new Vector3(0f, 0f, -4.5f)));
        }

        [Test]
        public void Tick_AfterCompletedCombatLeap_AllowsOnlyOneMeleeAttack()
        {
            FakeMovement movement = new();
            FakeAttack melee = new() { IsInRangeValue = true };
            FakeDetection detection = new() { Target = CreateTarget(new Vector3(5f, 0f, 0f)) };
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                detection,
                movement,
                melee,
                new FakeAttack());

            behaviour.Tick(0f);
            movement.CompleteCombatLeap(true);
            detection.Target = CreateTarget(Vector3.forward);
            behaviour.Tick(0f);
            behaviour.Tick(0f);

            Assert.That(melee.AttackCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_InCombatLeapRangeWithLineOfSight_PrioritizesCombatLeapOverRangedAttack()
        {
            FakeMovement movement = new();
            FakeAttack ranged = new();
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                new FakeDetection() { Target = CreateTarget(new Vector3(5f, 0f, 0f)) },
                movement,
                new FakeAttack(),
                ranged);

            behaviour.Tick(0.1f);

            Assert.That(movement.CombatLeapCount, Is.EqualTo(1));
            Assert.That(ranged.AttackCount, Is.Zero);
        }

        [Test]
        public void Tick_WhenCombatLeapChanceIsZero_UsesRangedAttack()
        {
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("combatLeapChance").floatValue = 0f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            FakeMovement movement = new();
            FakeAttack ranged = new();
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                new FakeDetection() { Target = CreateTarget(new Vector3(5f, 0f, 0f)) },
                movement,
                new FakeAttack(),
                ranged);

            behaviour.Tick(0f);

            Assert.That(movement.CombatLeapCount, Is.Zero);
            Assert.That(ranged.AttackCount, Is.EqualTo(1));
            Assert.That(movement.StopCount, Is.Zero);
        }

        [Test]
        public void Tick_WithoutLineOfSight_DoesNotStartCombatLeap()
        {
            FakeMovement movement = new();
            FakeAttack ranged = new();
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                new FakeDetection() { Target = CreateTarget(new Vector3(5f, 0f, 0f)) },
                movement,
                new FakeAttack(),
                ranged,
                hasLineOfSight: false);

            behaviour.Tick(0.1f);

            Assert.That(movement.CombatLeapCount, Is.Zero);
            Assert.That(ranged.AttackCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_WhenCombatLeapIsOnCooldown_UsesRangedFallback()
        {
            FakeMovement movement = new();
            FakeAttack ranged = new();
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                new FakeDetection() { Target = CreateTarget(new Vector3(5f, 0f, 0f)) },
                movement,
                new FakeAttack(),
                ranged);

            behaviour.Tick(0f);
            behaviour.Tick(0f);

            Assert.That(movement.CombatLeapCount, Is.EqualTo(1));
            Assert.That(ranged.AttackCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_WhenTargetIsTooClose_MovesToPreferredDistanceFromPlayerAndCanUseRanged()
        {
            FakeMovement movement = new();
            FakeAttack ranged = new();
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                new FakeDetection() { Target = CreateTarget(new Vector3(2f, 0f, 0f)) },
                movement,
                new FakeAttack(),
                ranged);

            behaviour.Tick(0.1f);

            Assert.That(movement.LastMoveDestination, Is.EqualTo(new Vector3(-3.5f, 0f, 0f)));
            Assert.That(ranged.AttackCount, Is.EqualTo(1));
            Assert.That(movement.StopCount, Is.Zero);
        }

        [Test]
        public void Tick_WhenTargetIsTooFar_ApproachesAndCanUseRanged()
        {
            FakeMovement movement = new();
            FakeAttack ranged = new();
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                new FakeDetection() { Target = CreateTarget(new Vector3(10f, 0f, 0f)) },
                movement,
                new FakeAttack(),
                ranged);

            behaviour.Tick(0f);

            Assert.That(movement.LastMoveDestination, Is.EqualTo(new Vector3(10f, 0f, 0f)));
            Assert.That(ranged.AttackCount, Is.EqualTo(1));
            Assert.That(movement.StopCount, Is.Zero);
        }

        [Test]
        public void Tick_WhenStrafePointIsInvalid_TriesTheOtherSide()
        {
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("combatLeapChance").floatValue = 0f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            FakeMovement movement = new() { ResolveFailuresBeforeSuccess = 1 };
            AssassinEnemyBehaviour behaviour = CreateBehaviour(
                new FakeDetection() { Target = CreateTarget(new Vector3(5f, 0f, 0f)) },
                movement,
                new FakeAttack(),
                new FakeAttack());

            behaviour.Tick(0f);

            Assert.That(movement.ResolvePositionCount, Is.EqualTo(2));
            Assert.That(movement.LastMoveDestination, Is.Not.EqualTo(Vector3.zero));
        }

        private AssassinEnemyBehaviour CreateBehaviour(
            FakeDetection detection,
            FakeMovement movement,
            out FakeAttack melee,
            out FakeAttack ranged)
        {
            melee = new FakeAttack();
            ranged = new FakeAttack();
            return CreateBehaviour(detection, movement, melee, ranged);
        }

        private AssassinEnemyBehaviour CreateBehaviour(
            FakeDetection detection,
            FakeMovement movement,
            FakeAttack melee,
            FakeAttack ranged,
            bool hasLineOfSight = true)
        {
            return new AssassinEnemyBehaviour(
                detection,
                movement,
                new FakeLineOfSight(hasLineOfSight),
                melee,
                ranged,
                config,
                new CombatLeap(config, movement));
        }

        private static SelectedTarget CreateTarget(Vector3 position)
        {
            return new SelectedTarget(new FakeTargetable(), position);
        }

        private sealed class FakeDetection : IEnemyDetection
        {
            public SelectedTarget Target { get; set; }
            public bool TryGetTarget(out SelectedTarget target)
            {
                target = Target;
                return target.IsValid;
            }
        }

        private sealed class FakeMovement : IEnemyMovement
        {
            public Vector3 Position { get; set; }
            public bool IsLeapingValue { get; set; }
            public bool IsMovementBlockedValue { get; set; }
            public int CombatLeapCount { get; private set; }
            public int StopCount { get; private set; }
            public int CommandCount { get; private set; }
            public int ResolvePositionCount { get; private set; }
            public int ResolveFailuresBeforeSuccess { get; set; }
            public Vector3 LastMoveDestination { get; private set; }
            private Action<bool> combatLeapCompleted;

            public bool IsLeaping => IsLeapingValue;
            public bool IsCharging => false;
            public bool IsMovementBlocked => IsMovementBlockedValue;

            public void FaceTowards(Vector3 position) => CommandCount++;
            public void MoveTo(Vector3 position)
            {
                CommandCount++;
                LastMoveDestination = position;
            }

            public void Stop()
            {
                CommandCount++;
                StopCount++;
            }

            public bool TryResolvePosition(Vector3 desiredPosition, out Vector3 validPosition)
            {
                ResolvePositionCount++;
                if (ResolveFailuresBeforeSuccess-- > 0)
                {
                    validPosition = default;
                    return false;
                }

                validPosition = desiredPosition;
                return true;
            }

            public bool TryLeapTo(Vector3 destination, float speed, float arcHeight) => false;
            public bool TryCombatLeapTo(
                Vector3 destination,
                float speed,
                float arcHeight,
                float fallSpeed,
                Action<bool> onCompleted)
            {
                CombatLeapCount++;
                combatLeapCompleted = onCompleted;
                return true;
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
                Action<Collider, Vector3> onCollision) => false;
        }

        private sealed class FakeLineOfSight : IEnemyLineOfSight
        {
            private readonly bool hasLineOfSight;

            public FakeLineOfSight(bool hasLineOfSight)
            {
                this.hasLineOfSight = hasLineOfSight;
            }

            public bool HasLineOfSight(Vector3 targetPosition) => hasLineOfSight;
            public bool HasLineOfSightFrom(Vector3 origin, Vector3 targetPosition) => hasLineOfSight;
        }

        private sealed class FakeAttack : IEnemyAttack
        {
            public bool IsInRangeValue { get; set; }
            public bool CanAttack { get; set; } = true;
            public int AttackCount { get; private set; }
            public float Range => 0f;

            public void Tick(float deltaTime) { }
            public bool IsInRange(SelectedTarget target) => IsInRangeValue;
            public bool TryAttack(SelectedTarget target)
            {
                if (!CanAttack)
                {
                    return false;
                }

                AttackCount++;
                return true;
            }
        }

        private sealed class FakeTargetable : ITargetable
        {
            public Transform TargetPoint => null;
        }
    }
}
