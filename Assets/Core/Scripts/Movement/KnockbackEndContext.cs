using UnityEngine;

namespace RPGame.Core.Movement
{
    public readonly struct KnockbackEndContext
    {
        public KnockbackEndContext(
            KnockbackEndReason reason,
            Collider obstacle = null,
            Vector3 collisionPoint = default)
        {
            Reason = reason;
            Obstacle = obstacle;
            CollisionPoint = collisionPoint;
        }

        public KnockbackEndReason Reason { get; }
        public Collider Obstacle { get; }
        public Vector3 CollisionPoint { get; }
    }
}
