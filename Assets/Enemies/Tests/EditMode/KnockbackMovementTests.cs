using NUnit.Framework;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class KnockbackMovementTests
    {
        private GameObject gameObject;

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Tick_AppliesEasedDisplacementAndFinishes()
        {
            gameObject = new GameObject("Knockback");
            bool finished = false;
            KnockbackMovement knockback = new(
                gameObject.transform,
                () => { },
                () => finished = true,
                CreateCapsule,
                collider => false);

            knockback.Start(Vector3.right, 2f, 1f, null);
            knockback.Tick(0.5f);

            Assert.IsTrue(knockback.IsActive);
            Assert.That(gameObject.transform.position.x, Is.GreaterThan(1f));

            knockback.Tick(0.5f);

            Assert.IsTrue(knockback.IsActive);

            knockback.Tick(0f);

            Assert.IsFalse(knockback.IsActive);
            Assert.IsTrue(finished);
            Assert.That(gameObject.transform.position.x, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void KnockbackMovement_IsNotMonoBehaviour()
        {
            Assert.IsFalse(typeof(KnockbackMovement).IsSubclassOf(typeof(MonoBehaviour)));
        }

        [Test]
        public void Tick_WhenBlockedByObstacle_InvokesCollisionCallback()
        {
            gameObject = new GameObject("Knockback");
            GameObject obstacle = new GameObject("Obstacle");
            BoxCollider obstacleCollider = obstacle.AddComponent<BoxCollider>();
            obstacle.transform.position = new Vector3(1.5f, 0.5f, 0f);
            Physics.SyncTransforms();
            int collisionCount = 0;
            KnockbackMovement knockback = new(
                gameObject.transform,
                () => { },
                () => { },
                CreateCapsule,
                collider => false);

            knockback.Start(Vector3.right, 2f, 1f, (collider, point) =>
            {
                if (collider == obstacleCollider)
                {
                    collisionCount++;
                }
            });
            knockback.Tick(1f);

            Assert.IsFalse(knockback.IsActive);
            Assert.AreEqual(1, collisionCount);
            Object.DestroyImmediate(obstacle);
        }

        private MovementCapsule CreateCapsule()
        {
            Vector3 center = gameObject.transform.position + Vector3.up * 0.5f;
            return new MovementCapsule(center + Vector3.down * 0.25f, center + Vector3.up * 0.25f, 0.25f);
        }
    }
}
