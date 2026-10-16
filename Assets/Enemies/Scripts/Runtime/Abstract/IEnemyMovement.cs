using System;
using UnityEngine;

namespace RPGame.Enemies
{
    public interface IEnemyMovement
    {
        Vector3 Position { get; }
        bool IsLeaping { get; }
        bool IsCharging { get; }
        bool IsMovementBlocked { get; }

        void FaceTowards(Vector3 position);
        void MoveTo(Vector3 position);
        void Stop();
        bool TryResolvePosition(Vector3 desiredPosition, out Vector3 validPosition);
        bool TryLeapTo(Vector3 destination, float speed, float arcHeight);
        bool TryCombatLeapTo(
            Vector3 destination,
            float speed,
            float arcHeight,
            float fallSpeed,
            Action<bool> onCompleted);
        bool TryStartCharge(
            Vector3 destination,
            float speed,
            float maxDistance,
            float knockbackResistance,
            Action<Collider, Vector3> onCollision);
    }
}
