using System;
using NUnit.Framework;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class LeapMovementTests
    {
        private GameObject gameObject;

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Tick_InterpolatesArcAndCompletesTraversal()
        {
            gameObject = new GameObject("Leap");
            bool completedTraversal = false;
            LeapMovement leap = new(
                gameObject.transform,
                () => new MovementAgentState(false, true),
                (agentState, position, completeTraversal) => completedTraversal = completeTraversal);

            leap.Start(new Vector3(2f, 0f, 0f), 2f, 1f, true);
            leap.Tick(0.5f);

            Assert.IsTrue(leap.IsActive);
            Assert.That(gameObject.transform.position.y, Is.GreaterThan(0f));

            leap.Tick(0.5f);

            Assert.IsTrue(leap.IsActive);

            leap.Tick(0f);

            Assert.IsFalse(leap.IsActive);
            Assert.IsTrue(completedTraversal);
        }

        [Test]
        public void Cancel_DoesNotCompleteTraversal()
        {
            gameObject = new GameObject("Leap");
            bool completedTraversal = true;
            LeapMovement leap = new(
                gameObject.transform,
                () => new MovementAgentState(false, true),
                (agentState, position, completeTraversal) => completedTraversal = completeTraversal);

            leap.Start(Vector3.forward, 1f, 0f, true);
            leap.Cancel();

            Assert.IsFalse(leap.IsActive);
            Assert.IsFalse(completedTraversal);
        }

        [Test]
        public void InterruptCombat_StopsHorizontalMovementAndFallsToLandingPoint()
        {
            gameObject = new GameObject("Leap");
            bool? completedCombatLeap = null;
            LeapMovement leap = new(
                gameObject.transform,
                () => new MovementAgentState(false, true),
                (_, position, _) => gameObject.transform.position = position,
                (Vector3 position, out Vector3 landingPosition) =>
                {
                    landingPosition = new Vector3(position.x, 0f, position.z);
                    return true;
                });

            leap.StartCombat(
                new Vector3(2f, 0f, 0f),
                2f,
                1f,
                2f,
                completed => completedCombatLeap = completed);
            leap.Tick(0.5f);
            leap.InterruptCombat();
            Vector3 interruptedPosition = gameObject.transform.position;

            leap.Tick(0.25f);

            Assert.That(gameObject.transform.position.x, Is.EqualTo(interruptedPosition.x));
            Assert.That(gameObject.transform.position.z, Is.EqualTo(interruptedPosition.z));
            Assert.That(gameObject.transform.position.y, Is.LessThan(interruptedPosition.y));

            leap.Tick(0.5f);

            Assert.IsFalse(leap.IsActive);
            Assert.That(gameObject.transform.position.y, Is.EqualTo(0f));
            Assert.IsFalse(completedCombatLeap);
        }

        [Test]
        public void CompleteCombat_ReportsSuccessfulCompletion()
        {
            gameObject = new GameObject("Leap");
            bool? completedCombatLeap = null;
            LeapMovement leap = new(
                gameObject.transform,
                () => new MovementAgentState(false, true),
                (_, _, _) => { });

            leap.StartCombat(
                new Vector3(2f, 0f, 0f),
                2f,
                1f,
                2f,
                completed => completedCombatLeap = completed);
            leap.Tick(1f);
            leap.Tick(0f);

            Assert.IsFalse(leap.IsActive);
            Assert.IsTrue(completedCombatLeap);
        }

        [Test]
        public void LeapMovement_IsNotMonoBehaviour()
        {
            Assert.IsFalse(typeof(LeapMovement).IsSubclassOf(typeof(MonoBehaviour)));
        }
    }
}
