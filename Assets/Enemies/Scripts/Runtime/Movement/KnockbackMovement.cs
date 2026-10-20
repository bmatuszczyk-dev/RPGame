using System;
using UnityEngine;

namespace RPGame.Enemies
{
    internal sealed class KnockbackMovement
    {
        private readonly Transform transform;
        private readonly Action beginMovement;
        private readonly Action finishMovement;
        private readonly Func<MovementCapsule> capsuleProvider;
        private readonly Func<Collider, bool> shouldIgnoreCollider;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        private bool isActive;
        private Vector3 direction;
        private Vector3 startPosition;
        private float distance;
        private float duration;
        private float elapsed;
        private Action<Collider, Vector3> collisionHandler;

        public bool IsActive => isActive;

        public KnockbackMovement(
            Transform transform,
            Action beginMovement,
            Action finishMovement,
            Func<MovementCapsule> capsuleProvider,
            Func<Collider, bool> shouldIgnoreCollider)
        {
            this.transform = transform;
            this.beginMovement = beginMovement;
            this.finishMovement = finishMovement;
            this.capsuleProvider = capsuleProvider;
            this.shouldIgnoreCollider = shouldIgnoreCollider;
        }

        public void Start(
            Vector3 direction,
            float distance,
            float duration,
            Action<Collider, Vector3> onCollision)
        {
            if (isActive)
            {
                Finish();
            }

            direction.y = 0f;
            this.direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.zero;
            this.distance = distance;
            this.duration = duration;
            elapsed = 0f;
            collisionHandler = onCollision;
            startPosition = transform.position;
            beginMovement();
            isActive = true;
        }

        public void Tick(float deltaTime)
        {
            if (!isActive)
            {
                return;
            }

            if (elapsed >= duration)
            {
                Finish();
                return;
            }

            elapsed += deltaTime;
            float progress = duration > Mathf.Epsilon ? Mathf.Clamp01(elapsed / duration) : 1f;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            Vector3 desiredPosition = startPosition + direction * (distance * easedProgress);
            if (!TryApplyStep(desiredPosition - transform.position))
            {
                Finish();
            }
        }

        public void Cancel()
        {
            if (isActive)
            {
                Finish();
            }
        }

        private void Finish()
        {
            isActive = false;
            collisionHandler = null;
            direction = default;
            distance = 0f;
            duration = 0f;
            elapsed = 0f;
            finishMovement();
        }

        private bool TryApplyStep(Vector3 displacement)
        {
            float displacementDistance = displacement.magnitude;
            if (displacementDistance <= Mathf.Epsilon)
            {
                return true;
            }

            MovementCapsule capsule = capsuleProvider();
            int hitCount = Physics.CapsuleCastNonAlloc(
                capsule.Bottom,
                capsule.Top,
                capsule.Radius,
                displacement / displacementDistance,
                hitBuffer,
                displacementDistance,
                ~0,
                QueryTriggerInteraction.Ignore);
            float closestDistance = float.MaxValue;
            RaycastHit closestHit = default;
            bool foundHit = false;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitBuffer[i];
                if (hit.collider == null || shouldIgnoreCollider(hit.collider) || hit.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hit.distance;
                closestHit = hit;
                foundHit = true;
            }

            if (!foundHit)
            {
                transform.position += displacement;
                return true;
            }

            transform.position += displacement.normalized * Mathf.Max(0f, closestDistance - 0.01f);
            collisionHandler?.Invoke(closestHit.collider, closestHit.point);
            return false;
        }
    }
}
