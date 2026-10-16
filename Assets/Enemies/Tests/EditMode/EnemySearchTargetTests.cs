using NUnit.Framework;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class EnemySearchTargetTests
    {
        private GameObject playerObject;

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void MoveTowardsPlayerArea_KeepsDestinationUntilReached()
        {
            CreatePlayer(new Vector3(10f, 0f, 0f));
            FakeMovement movement = new();
            EnemySearchTarget searchTarget = new();

            searchTarget.MoveTowardsPlayerArea(movement);
            Vector3 firstDestination = movement.LastDestination;
            searchTarget.MoveTowardsPlayerArea(movement);

            Assert.AreEqual(firstDestination, movement.LastDestination);
            Assert.AreEqual(1, movement.TryResolvePositionCount);
            Assert.AreEqual(2, movement.MoveToCount);
        }

        [Test]
        public void MoveTowardsPlayerArea_AfterArrival_UsesCurrentPlayerPosition()
        {
            CreatePlayer(Vector3.zero);
            FakeMovement movement = new();
            EnemySearchTarget searchTarget = new();

            searchTarget.MoveTowardsPlayerArea(movement);
            Vector3 firstDestination = movement.LastDestination;
            movement.Position = firstDestination;
            playerObject.transform.position = new Vector3(10f, 0f, 0f);

            searchTarget.MoveTowardsPlayerArea(movement);

            Assert.AreEqual(2, movement.TryResolvePositionCount);
            Assert.LessOrEqual(Vector3.Distance(movement.LastDestination, playerObject.transform.position), 3f);
            Assert.AreNotEqual(firstDestination, movement.LastDestination);
        }

        [Test]
        public void MoveTowardsPlayerArea_WithoutPlayer_Stops()
        {
            FakeMovement movement = new();

            new EnemySearchTarget().MoveTowardsPlayerArea(movement);

            Assert.AreEqual(1, movement.StopCount);
            Assert.AreEqual(0, movement.MoveToCount);
        }

        [Test]
        public void MoveTowardsPlayerArea_WhenDestinationCannotResolve_StopsWithoutRetryingEveryTick()
        {
            CreatePlayer(Vector3.zero);
            FakeMovement movement = new();
            movement.CanResolvePosition = false;
            EnemySearchTarget searchTarget = new();

            searchTarget.MoveTowardsPlayerArea(movement);
            searchTarget.MoveTowardsPlayerArea(movement);

            Assert.AreEqual(2, movement.StopCount);
            Assert.AreEqual(1, movement.TryResolvePositionCount);
            Assert.AreEqual(0, movement.MoveToCount);
        }

        private void CreatePlayer(Vector3 position)
        {
            playerObject = new GameObject("PlayerTarget");
            playerObject.transform.position = position;
            playerObject.AddComponent<PlayerTargetable>();
        }

        private sealed class FakeMovement : IEnemyMovement
        {
            public bool CanResolvePosition { get; set; } = true;
            public int MoveToCount { get; private set; }
            public int StopCount { get; private set; }
            public int TryResolvePositionCount { get; private set; }
            public Vector3 Position { get; set; }
            public bool IsLeaping => false;
            public bool IsCharging => false;
            public bool IsMovementBlocked => false;
            public Vector3 LastDestination { get; private set; }

            public void FaceTowards(Vector3 position)
            {
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

            public bool TryCombatLeapTo(Vector3 destination, float speed, float arcHeight, float fallSpeed, System.Action<bool> onCompleted) => false;

            public bool TryStartCharge(
                Vector3 destination,
                float speed,
                float maxDistance,
                float knockbackResistance,
                System.Action<Collider, Vector3> onCollision)
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
                validPosition = desiredPosition;
                return CanResolvePosition;
            }
        }
    }
}
