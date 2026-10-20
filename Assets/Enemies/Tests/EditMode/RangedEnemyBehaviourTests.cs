using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Targeting;
using UnityEngine;
using UnityEngine.AI;

namespace RPGame.Enemies.Tests
{
    public sealed class RangedEnemyBehaviourTests
    {
        [Test]
        public void Tick_WhenNoTarget_StopsAndStaysIdle()
        {
            FakeDetection detection = new();
            FakeMovement movement = new();
            FakeAttack straightAttack = new();
            FakeAttack parabolicAttack = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, straightAttack, parabolicAttack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Idle, behaviour.State);
            Assert.AreEqual(1, movement.StopCount);
            Assert.AreEqual(0, movement.MoveToCount);
            Assert.AreEqual(0, straightAttack.TryAttackCount);
            Assert.AreEqual(0, parabolicAttack.TryAttackCount);
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
                RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

                behaviour.Tick(0.1f);

                Assert.AreEqual(RangedBehaviourState.Idle, behaviour.State);
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
                Vector3 targetPosition = new(8f, 0f, 0f);
                FakeDetection detection = CreateDetectionWithTarget(targetPosition);
                FakeMovement movement = new();
                RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

                behaviour.Tick(0.1f);

                Assert.AreEqual(RangedBehaviourState.Approach, behaviour.State);
                Assert.AreEqual(targetPosition, movement.LastDestination);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void Tick_WhenTargetIsDetected_FacesTarget()
        {
            Vector3 targetPosition = new(8f, 0f, 0f);
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

            behaviour.Tick(0.1f);

            Assert.AreEqual(1, movement.FaceTowardsCount);
            Assert.AreEqual(targetPosition, movement.LastFaceTarget);
        }

        [Test]
        public void Tick_WhenTargetIsTooFar_Approaches()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(8f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack straightAttack = new();
            FakeAttack parabolicAttack = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, straightAttack, parabolicAttack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Approach, behaviour.State);
            Assert.AreEqual(1, movement.MoveToCount);
            Assert.AreEqual(0, movement.StopCount);
            Assert.AreEqual(0, straightAttack.TryAttackCount);
            Assert.AreEqual(0, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_WhenTargetIsInRange_Holds()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(4f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack straightAttack = new();
            FakeAttack parabolicAttack = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, straightAttack, parabolicAttack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Hold, behaviour.State);
            Assert.AreEqual(1, movement.StopCount);
            Assert.AreEqual(0, movement.MoveToCount);
            Assert.AreEqual(0, straightAttack.TryAttackCount);
            Assert.AreEqual(1, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_WhenTargetIsInRangeWithoutLineOfSight_Repositions()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(4f, 0f, 0f));
            FakeMovement movement = new();
            FakeLineOfSight lineOfSight = new() { HasLineOfSightResult = false };
            FakeAttack parabolicAttack = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, lineOfSight, null, parabolicAttack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Reposition, behaviour.State);
            Assert.AreEqual(1, movement.StopCount);
            Assert.AreEqual(0, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_WhenRepositionCandidateHasNoLineOfSight_RejectsIt()
        {
            Vector3 targetPosition = Vector3.zero;
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new() { Position = new Vector3(0f, 0f, -4f) };
            FakeLineOfSight lineOfSight = new() { HasLineOfSightResult = false };
            Vector3 blockedCandidate = GetRepositionCandidate(targetPosition, movement.Position, 0);
            Vector3 visibleCandidate = GetRepositionCandidate(targetPosition, movement.Position, 1);
            lineOfSight.AddVisibleOrigin(visibleCandidate);
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, lineOfSight);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Reposition, behaviour.State);
            Assert.AreEqual(1, movement.MoveToCount);
            Assert.AreEqual(visibleCandidate, movement.LastDestination);
            Assert.AreNotEqual(blockedCandidate, movement.LastDestination);
        }

        [Test]
        public void Tick_WhenRepositionCandidateCannotResolvePosition_RejectsIt()
        {
            Vector3 targetPosition = Vector3.zero;
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new() { Position = new Vector3(0f, 0f, -4f) };
            FakeLineOfSight lineOfSight = new() { HasLineOfSightResult = false };
            Vector3 unresolvedCandidate = GetRepositionCandidate(targetPosition, movement.Position, 0);
            Vector3 validCandidate = GetRepositionCandidate(targetPosition, movement.Position, 1);
            movement.RejectDesiredPosition(unresolvedCandidate);
            lineOfSight.AddVisibleOrigin(unresolvedCandidate);
            lineOfSight.AddVisibleOrigin(validCandidate);
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, lineOfSight);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Reposition, behaviour.State);
            Assert.AreEqual(1, movement.MoveToCount);
            Assert.AreEqual(validCandidate, movement.LastDestination);
        }

        [Test]
        public void Tick_WhenRepositionFindsCandidate_MovesToCandidate()
        {
            Vector3 targetPosition = Vector3.zero;
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new() { Position = new Vector3(0f, 0f, -4f) };
            FakeLineOfSight lineOfSight = new() { HasLineOfSightResult = false };
            Vector3 candidate = GetRepositionCandidate(targetPosition, movement.Position, 0);
            lineOfSight.AddVisibleOrigin(candidate);
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, lineOfSight);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Reposition, behaviour.State);
            Assert.AreEqual(1, movement.MoveToCount);
            Assert.AreEqual(candidate, movement.LastDestination);
        }

        [Test]
        public void Tick_WhenRepositioning_DoesNotUseParabolicAttack()
        {
            Vector3 targetPosition = Vector3.zero;
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new() { Position = new Vector3(0f, 0f, -4f) };
            FakeLineOfSight lineOfSight = new() { HasLineOfSightResult = false };
            FakeAttack parabolicAttack = new();
            lineOfSight.AddVisibleOrigin(GetRepositionCandidate(targetPosition, movement.Position, 0));
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, lineOfSight, null, parabolicAttack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Reposition, behaviour.State);
            Assert.AreEqual(0, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_WhenRepositioningRegainsLineOfSight_EntersHold()
        {
            Vector3 targetPosition = Vector3.zero;
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new() { Position = new Vector3(0f, 0f, -4f) };
            FakeLineOfSight lineOfSight = new() { HasLineOfSightResult = false };
            FakeAttack parabolicAttack = new();
            lineOfSight.AddVisibleOrigin(GetRepositionCandidate(targetPosition, movement.Position, 0));
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, lineOfSight, null, parabolicAttack);
            behaviour.Tick(0.1f);

            lineOfSight.HasLineOfSightResult = true;
            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Hold, behaviour.State);
            Assert.AreEqual(1, movement.StopCount);
            Assert.AreEqual(1, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_WhenRepositioningBecomesTooFar_Approaches()
        {
            Vector3 targetPosition = Vector3.zero;
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new() { Position = new Vector3(0f, 0f, -4f) };
            FakeLineOfSight lineOfSight = new() { HasLineOfSightResult = false };
            lineOfSight.AddVisibleOrigin(GetRepositionCandidate(targetPosition, movement.Position, 0));
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, lineOfSight);
            behaviour.Tick(0.1f);

            detection.SetTarget(new SelectedTarget(detection.Target.Targetable, new Vector3(8f, 0f, 0f)));
            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Approach, behaviour.State);
            Assert.AreEqual(new Vector3(8f, 0f, 0f), movement.LastDestination);
        }

        [Test]
        public void Tick_WhenRepositioningBecomesTooClose_Retreats()
        {
            Vector3 targetPosition = Vector3.zero;
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new() { Position = new Vector3(0f, 0f, -4f) };
            FakeLineOfSight lineOfSight = new() { HasLineOfSightResult = false };
            FakeAttack straightAttack = new();
            lineOfSight.AddVisibleOrigin(GetRepositionCandidate(targetPosition, movement.Position, 0));
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, lineOfSight, straightAttack);
            behaviour.Tick(0.1f);

            movement.Position = new Vector3(1f, 0f, 0f);
            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Retreat, behaviour.State);
            Assert.AreEqual(1, straightAttack.TryAttackCount);
        }

        [Test]
        public void Tick_WhenNoRepositionCandidateIsValid_StopsAndThrottlesSearch()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(4f, 0f, 0f));
            FakeMovement movement = new();
            FakeLineOfSight lineOfSight = new() { HasLineOfSightResult = false };
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, lineOfSight);

            behaviour.Tick(0.1f);
            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Reposition, behaviour.State);
            Assert.AreEqual(1, movement.StopCount);
            Assert.AreEqual(8, movement.TryResolvePositionCount);
            Assert.AreEqual(0, movement.MoveToCount);
        }

        [Test]
        public void Tick_WhenTargetIsTooClose_Retreats()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(1f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack straightAttack = new();
            FakeAttack parabolicAttack = new();
            movement.Position = Vector3.zero;
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, straightAttack, parabolicAttack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Retreat, behaviour.State);
            Assert.AreEqual(1, movement.MoveToCount);
            Assert.AreEqual(0, movement.StopCount);
            Assert.AreEqual(1, straightAttack.TryAttackCount);
            Assert.AreEqual(0, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_RetreatMovementAndStraightAttackHappenInSameTick()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(1f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack straightAttack = new();
            FakeAttack parabolicAttack = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, straightAttack, parabolicAttack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Retreat, behaviour.State);
            Assert.AreEqual(1, movement.MoveToCount);
            Assert.AreEqual(1, straightAttack.TryAttackCount);
            Assert.AreEqual(0, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_TicksBothAttacksRegardlessOfPositioningState()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(8f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack straightAttack = new();
            FakeAttack parabolicAttack = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, straightAttack, parabolicAttack);

            behaviour.Tick(0.25f);

            Assert.AreEqual(RangedBehaviourState.Approach, behaviour.State);
            Assert.AreEqual(1, straightAttack.TickCount);
            Assert.AreEqual(1, parabolicAttack.TickCount);
            Assert.AreEqual(0.25f, straightAttack.LastDeltaTime, 0.0001f);
            Assert.AreEqual(0.25f, parabolicAttack.LastDeltaTime, 0.0001f);
        }

        [Test]
        public void Tick_StraightCooldownCanAdvanceDuringHold()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(4f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack straightAttack = new();
            FakeAttack parabolicAttack = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, straightAttack, parabolicAttack);

            behaviour.Tick(0.4f);

            Assert.AreEqual(RangedBehaviourState.Hold, behaviour.State);
            Assert.AreEqual(1, straightAttack.TickCount);
            Assert.AreEqual(0.4f, straightAttack.LastDeltaTime, 0.0001f);
            Assert.AreEqual(0, straightAttack.TryAttackCount);
            Assert.AreEqual(1, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_ParabolicCooldownCanAdvanceDuringRetreat()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(1f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack straightAttack = new();
            FakeAttack parabolicAttack = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, straightAttack, parabolicAttack);

            behaviour.Tick(0.4f);

            Assert.AreEqual(RangedBehaviourState.Retreat, behaviour.State);
            Assert.AreEqual(1, parabolicAttack.TickCount);
            Assert.AreEqual(0.4f, parabolicAttack.LastDeltaTime, 0.0001f);
            Assert.AreEqual(1, straightAttack.TryAttackCount);
            Assert.AreEqual(0, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_FailedAttackDoesNotChangePositioningState()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(4f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack parabolicAttack = new();
            parabolicAttack.TryAttackResult = false;
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, new FakeAttack(), parabolicAttack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Hold, behaviour.State);
            Assert.AreEqual(1, parabolicAttack.TryAttackCount);
            Assert.AreEqual(1, movement.StopCount);
        }

        [Test]
        public void Tick_WhenTargetIsLost_EntersIdleStopsAndDoesNotTryAttack()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(4f, 0f, 0f));
            FakeMovement movement = new();
            FakeAttack straightAttack = new();
            FakeAttack parabolicAttack = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, straightAttack, parabolicAttack);

            behaviour.Tick(0.1f);
            detection.ClearTarget();
            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Idle, behaviour.State);
            Assert.AreEqual(2, movement.StopCount);
            Assert.AreEqual(0, straightAttack.TryAttackCount);
            Assert.AreEqual(1, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_WhenDetectionReturnsInvalidTarget_EntersIdleStopsAndDoesNotTryAttack()
        {
            FakeDetection detection = new();
            detection.SetTarget(default);
            FakeMovement movement = new();
            FakeAttack straightAttack = new();
            FakeAttack parabolicAttack = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement, straightAttack, parabolicAttack);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Idle, behaviour.State);
            Assert.AreEqual(1, movement.StopCount);
            Assert.AreEqual(0, straightAttack.TryAttackCount);
            Assert.AreEqual(0, parabolicAttack.TryAttackCount);
        }

        [Test]
        public void Tick_WhenApproaching_DoesNotOscillateAtMaxRangeBoundary()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(6.5f, 0f, 0f));
            FakeMovement movement = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

            behaviour.Tick(0.1f);
            detection.SetTarget(new SelectedTarget(detection.Target.Targetable, new Vector3(4.75f, 0f, 0f)));
            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Approach, behaviour.State);
            Assert.AreEqual(2, movement.MoveToCount);
            Assert.AreEqual(new Vector3(4.75f, 0f, 0f), movement.LastDestination);
        }

        [Test]
        public void Tick_WhenRetreating_DoesNotOscillateAtMinRangeBoundary()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(1f, 0f, 0f));
            FakeMovement movement = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

            behaviour.Tick(0.1f);
            detection.SetTarget(new SelectedTarget(detection.Target.Targetable, new Vector3(2.25f, 0f, 0f)));
            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Retreat, behaviour.State);
            Assert.AreEqual(2, movement.MoveToCount);
        }

        [Test]
        public void Tick_WhenCrossingHysteresisThreshold_EntersHold()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(6.5f, 0f, 0f));
            FakeMovement movement = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

            behaviour.Tick(0.1f);
            detection.SetTarget(new SelectedTarget(detection.Target.Targetable, new Vector3(4f, 0f, 0f)));
            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Hold, behaviour.State);
            Assert.AreEqual(1, movement.StopCount);
        }

        [Test]
        public void Tick_WhenRetreatCrossesHysteresisThreshold_EntersHold()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(1f, 0f, 0f));
            FakeMovement movement = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

            behaviour.Tick(0.1f);
            detection.SetTarget(new SelectedTarget(detection.Target.Targetable, new Vector3(3f, 0f, 0f)));
            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Hold, behaviour.State);
            Assert.AreEqual(1, movement.StopCount);
        }

        [Test]
        public void Tick_WhenApproaching_UsesTargetPositionAsDestination()
        {
            Vector3 targetPosition = new(7f, 0f, 2f);
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new();
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

            behaviour.Tick(0.1f);

            Assert.AreEqual(targetPosition, movement.LastDestination);
        }

        [Test]
        public void Tick_WhenRetreating_ChoosesDirectionAwayFromTarget()
        {
            Vector3 targetPosition = new(1f, 0f, 0f);
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new();
            movement.Position = Vector3.zero;
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

            behaviour.Tick(0.1f);

            Assert.Less(movement.LastDesiredPosition.x, movement.Position.x);
            Assert.AreEqual(Vector3.left, (movement.LastDesiredPosition - targetPosition).normalized);
        }

        [Test]
        public void Tick_WhenRetreating_CalculatesRetreatPointRelativeToTarget()
        {
            Vector3 targetPosition = new(1f, 0f, 0f);
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new();
            movement.Position = Vector3.zero;
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

            behaviour.Tick(0.1f);

            Assert.AreEqual(new Vector3(-1.5f, 0f, 0f), movement.LastDesiredPosition);
        }

        [Test]
        public void Tick_WhenRetreating_DesiredPointUsesMinRangePlusHysteresisDistance()
        {
            Vector3 targetPosition = new(1f, 0f, 0f);
            FakeDetection detection = CreateDetectionWithTarget(targetPosition);
            FakeMovement movement = new();
            movement.Position = Vector3.zero;
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

            behaviour.Tick(0.1f);

            Assert.AreEqual(2.5f, Vector3.Distance(targetPosition, movement.LastDesiredPosition), 0.0001f);
        }

        [Test]
        public void Tick_WhenRetreatPointCannotBeResolved_Stops()
        {
            FakeDetection detection = CreateDetectionWithTarget(new Vector3(1f, 0f, 0f));
            FakeMovement movement = new();
            movement.CanResolvePosition = false;
            RangedEnemyBehaviour behaviour = CreateBehaviour(detection, movement);

            behaviour.Tick(0.1f);

            Assert.AreEqual(RangedBehaviourState.Retreat, behaviour.State);
            Assert.AreEqual(1, movement.StopCount);
            Assert.AreEqual(0, movement.MoveToCount);
        }

        [Test]
        public void RangedEnemyBehaviour_DoesNotDependOnMonoBehaviourNavMeshAgentOrPhysics()
        {
            Assert.IsFalse(typeof(RangedEnemyBehaviour).IsSubclassOf(typeof(MonoBehaviour)));

            Type[] forbiddenTypes =
            {
                typeof(MonoBehaviour),
                typeof(NavMeshAgent),
                typeof(Physics)
            };

            bool hasForbiddenField = typeof(RangedEnemyBehaviour)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => forbiddenTypes.Any(type => type.IsAssignableFrom(field.FieldType)));

            string source = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Enemies",
                "Scripts",
                "Runtime",
                "RangedEnemy",
                "RangedEnemyBehaviour.cs"));

            Assert.IsFalse(hasForbiddenField);
            Assert.IsFalse(source.Contains("NavMesh", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("Physics", StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("CanStartAttack", StringComparison.Ordinal));
        }

        [Test]
        public void MovementAndDetection_DoNotUseLineOfSight()
        {
            string movementSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Enemies",
                "Scripts",
                "Movement.cs"));

            string detectionSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Enemies",
                "Scripts",
                "Detection.cs"));

            Assert.IsFalse(movementSource.Contains("LineOfSight", StringComparison.Ordinal));
            Assert.IsFalse(detectionSource.Contains("LineOfSight", StringComparison.Ordinal));
        }

        [Test]
        public void RangedEnemyBehaviour_DoesNotReferencePlayer()
        {
            bool referencesPlayer = typeof(RangedEnemyBehaviour).Assembly
                .GetReferencedAssemblies()
                .Any(assemblyName => assemblyName.Name == "RPGame.Player");

            Assert.IsFalse(referencesPlayer);
        }

        [Test]
        public void RangedBehaviourState_IsLocalToRangedBehaviour()
        {
            Assert.AreEqual("RPGame.Enemies.RangedBehaviourState", typeof(RangedBehaviourState).FullName);

            bool hasGlobalEnemyBehaviourState = typeof(RangedEnemyBehaviour).Assembly
                .GetTypes()
                .Any(type => type.Name == "EnemyBehaviourState");

            Assert.IsFalse(hasGlobalEnemyBehaviourState);
            Assert.IsFalse(typeof(RangedBehaviourState).IsPublic);
        }

        [Test]
        public void Config_WhenRangesAreInvalid_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TestRangedEnemyBehaviourConfig.Create(-1f, 5f, 0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => TestRangedEnemyBehaviourConfig.Create(5f, 5f, 0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => TestRangedEnemyBehaviourConfig.Create(2f, 5f, -0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => TestRangedEnemyBehaviourConfig.Create(3f, 8f, 2.51f));
        }

        [Test]
        public void Config_AllowsHysteresisUpToHalfOfValidRange()
        {
            Assert.DoesNotThrow(() => TestRangedEnemyBehaviourConfig.Create(3f, 8f, 2.5f));
        }

        [Test]
        public void Config_IsScriptableObject()
        {
            Assert.IsTrue(typeof(ScriptableObject).IsAssignableFrom(typeof(RangedEnemyBehaviourConfig)));
            Assert.IsTrue(typeof(EnemyBehaviourConfigBase).IsAssignableFrom(typeof(RangedEnemyBehaviourConfig)));
            Assert.IsTrue(typeof(IRangedEnemyBehaviourConfig).IsAssignableFrom(typeof(RangedEnemyBehaviourConfig)));
        }

        private RangedEnemyBehaviour CreateBehaviour(FakeDetection detection, FakeMovement movement)
        {
            return CreateBehaviour(detection, movement, new FakeLineOfSight(), null, null);
        }

        private RangedEnemyBehaviour CreateBehaviour(
            FakeDetection detection,
            FakeMovement movement,
            IEnemyAttack straightAttack,
            IEnemyAttack parabolicAttack)
        {
            return CreateBehaviour(detection, movement, new FakeLineOfSight(), straightAttack, parabolicAttack);
        }

        private RangedEnemyBehaviour CreateBehaviour(
            FakeDetection detection,
            FakeMovement movement,
            FakeLineOfSight lineOfSight,
            IEnemyAttack straightAttack = null,
            IEnemyAttack parabolicAttack = null)
        {
            return new RangedEnemyBehaviour(
                detection,
                movement,
                CreateConfig(2f, 5f, 0.5f),
                lineOfSight,
                straightAttack,
                parabolicAttack);
        }

        private static TestRangedEnemyBehaviourConfig CreateConfig(float minRange, float maxRange, float rangeHysteresis)
        {
            return TestRangedEnemyBehaviourConfig.Create(
                minRange,
                maxRange,
                rangeHysteresis);
        }

        private static FakeDetection CreateDetectionWithTarget(Vector3 position)
        {
            FakeDetection detection = new();
            detection.SetTarget(new SelectedTarget(new FakeTargetable(), position));
            return detection;
        }

        private static Vector3 GetRepositionCandidate(Vector3 targetPosition, Vector3 enemyPosition, int candidateIndex)
        {
            Vector3 baseDirection = enemyPosition - targetPosition;
            baseDirection.y = 0f;
            if (baseDirection.sqrMagnitude <= 0.0001f)
            {
                baseDirection = Vector3.forward;
            }

            baseDirection.Normalize();
            int step = (candidateIndex + 1) / 2;
            int side = candidateIndex % 2 == 0 ? 1 : -1;
            float angle = 360f / 8f * step * side;
            return targetPosition + Quaternion.AngleAxis(angle, Vector3.up) * baseDirection * 3.5f;
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
            private readonly HashSet<Vector3> unresolvedDesiredPositions = new();

            public bool CanResolvePosition { get; set; } = true;
            public int MoveToCount { get; private set; }
            public int StopCount { get; private set; }
            public int FaceTowardsCount { get; private set; }
            public int TryResolvePositionCount { get; private set; }
            public Vector3 Position { get; set; }
            public bool IsLeaping => false;
            public bool IsCharging => false;
            public bool IsMovementBlocked => false;
            public Vector3 LastDestination { get; private set; }
            public Vector3 LastDesiredPosition { get; private set; }
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
                TryResolvePositionCount++;
                LastDesiredPosition = desiredPosition;
                validPosition = desiredPosition;
                return CanResolvePosition && !unresolvedDesiredPositions.Contains(desiredPosition);
            }

            public void RejectDesiredPosition(Vector3 desiredPosition)
            {
                unresolvedDesiredPositions.Add(desiredPosition);
            }
        }

        private sealed class FakeAttack : IEnemyAttack
        {
            public float Range => 0f;
            public bool TryAttackResult { get; set; } = true;
            public int TickCount { get; private set; }
            public int TryAttackCount { get; private set; }
            public float LastDeltaTime { get; private set; }
            public SelectedTarget LastTarget { get; private set; }

            public void Tick(float deltaTime)
            {
                TickCount++;
                LastDeltaTime = deltaTime;
            }

            public bool IsInRange(SelectedTarget target)
            {
                return true;
            }

            public bool TryAttack(SelectedTarget target)
            {
                TryAttackCount++;
                LastTarget = target;
                return TryAttackResult;
            }
        }

        private sealed class FakeLineOfSight : IEnemyLineOfSight
        {
            private readonly HashSet<Vector3> visibleOrigins = new();

            public bool HasLineOfSightResult { get; set; } = true;
            public int HasLineOfSightCount { get; private set; }

            public bool HasLineOfSight(Vector3 targetPosition)
            {
                HasLineOfSightCount++;
                return HasLineOfSightResult;
            }

            public bool HasLineOfSightFrom(Vector3 origin, Vector3 targetPosition)
            {
                return visibleOrigins.Contains(origin);
            }

            public void AddVisibleOrigin(Vector3 origin)
            {
                visibleOrigins.Add(origin);
            }
        }

        private sealed class FakeTargetable : ITargetable
        {
            public Transform TargetPoint => null;
        }

        private sealed class TestRangedEnemyBehaviourConfig : IRangedEnemyBehaviourConfig
        {
            private float minRange;
            private float maxRange;
            private float rangeHysteresis;

            public float MinRange => minRange;
            public float MaxRange => maxRange;
            public float RangeHysteresis => rangeHysteresis;
            public float RepositionSearchInterval => 0.5f;
            public float AttackDelay => 0f;

            public static TestRangedEnemyBehaviourConfig Create(
                float minRange,
                float maxRange,
                float rangeHysteresis)
            {
                RangedEnemyBehaviourConfig.ValidateRanges(minRange, maxRange, rangeHysteresis);

                return new TestRangedEnemyBehaviourConfig
                {
                    minRange = minRange,
                    maxRange = maxRange,
                    rangeHysteresis = rangeHysteresis
                };
            }
        }
    }
}
