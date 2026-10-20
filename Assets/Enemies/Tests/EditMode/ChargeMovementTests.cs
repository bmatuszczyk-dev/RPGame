using System;
using NUnit.Framework;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class ChargeMovementTests
    {
        private GameObject gameObject;

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Tick_ReachesMaxDistanceAndFinishes()
        {
            gameObject = new GameObject("Charge");
            bool finished = false;
            ChargeMovement charge = new(
                gameObject.transform,
                () => new MovementAgentState(false, true),
                (agentState, position, completeTraversal) => finished = true,
                CreateCapsule,
                collider => false,
                () => { });

            charge.Start(Vector3.right * 10f, 2f, 1f, null);
            charge.Tick(0.5f);

            Assert.IsTrue(charge.IsActive);

            charge.Tick(0f);

            Assert.IsFalse(charge.IsActive);
            Assert.IsTrue(finished);
            Assert.That(gameObject.transform.position.x, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Tick_WhenCollisionStopsStep_InvokesHandlerAndFinishes()
        {
            gameObject = new GameObject("Charge");
            GameObject colliderObject = new GameObject("Collision");
            Collider collider = colliderObject.AddComponent<BoxCollider>();
            int collisionCount = 0;
            ChargeMovement charge = new(
                gameObject.transform,
                () => new MovementAgentState(false, true),
                (agentState, position, completeTraversal) => { },
                CreateCapsule,
                hitCollider => false,
                () => { });

            charge.Start(Vector3.right, 10f, 1f, (hitCollider, point) => collisionCount++);
            colliderObject.transform.position = new Vector3(0.9f, 0.5f, 0f);
            Physics.SyncTransforms();
            charge.Tick(0.1f);

            Assert.IsFalse(charge.IsActive);
            Assert.AreEqual(1, collisionCount);
            UnityEngine.Object.DestroyImmediate(colliderObject);
        }

        [Test]
        public void ChargeMovement_IsNotMonoBehaviour()
        {
            Assert.IsFalse(typeof(ChargeMovement).IsSubclassOf(typeof(MonoBehaviour)));
        }

        private MovementCapsule CreateCapsule()
        {
            Vector3 center = gameObject.transform.position + Vector3.up * 0.5f;
            return new MovementCapsule(center + Vector3.down * 0.25f, center + Vector3.up * 0.25f, 0.25f);
        }
    }
}
