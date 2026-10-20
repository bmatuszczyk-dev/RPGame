using System;
using UnityEngine;

namespace RPGame.Enemies
{
    internal sealed class ChargeMovement
    {
        private readonly Transform transform;
        private readonly Func<MovementAgentState> beginMovement;
        private readonly Action<MovementAgentState, Vector3, bool> finishMovement;
        private readonly Func<MovementCapsule> capsuleProvider;
        private readonly Func<Collider, bool> shouldIgnoreCollider;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        private bool isActive;
        private Vector3 direction;
        private float speed;
        private float maxDistance;
        private float distance;
        private Action<Collider, Vector3> collisionHandler;
        private MovementAgentState agentState;
        private readonly Action chargeFinished;

        public bool IsActive => isActive;

        public ChargeMovement(
            Transform transform,
            Func<MovementAgentState> beginMovement,
            Action<MovementAgentState, Vector3, bool> finishMovement,
            Func<MovementCapsule> capsuleProvider,
            Func<Collider, bool> shouldIgnoreCollider,
            Action chargeFinished)
        {
            this.transform = transform;
            this.beginMovement = beginMovement;
            this.finishMovement = finishMovement;
            this.capsuleProvider = capsuleProvider;
            this.shouldIgnoreCollider = shouldIgnoreCollider;
            this.chargeFinished = chargeFinished;
        }

        public void Start(
            Vector3 destination,
            float speed,
            float maxDistance,
            Action<Collider, Vector3> onCollision)
        {
            Vector3 chargeDirection = destination - transform.position;
            chargeDirection.y = 0f;
            direction = chargeDirection.normalized;
            this.speed = speed;
            this.maxDistance = maxDistance;
            distance = 0f;
            collisionHandler = onCollision;
            agentState = beginMovement();
            isActive = true;
        }

        public void Tick(float deltaTime)
        {
            if (!isActive)
            {
                return;
            }

            if (distance >= maxDistance)
            {
                Finish();
                return;
            }

            float stepDistance = Mathf.Min(speed * deltaTime, maxDistance - distance);
            if (stepDistance <= Mathf.Epsilon)
            {
                return;
            }

            if (!TryApplyStep(direction * stepDistance))
            {
                Finish();
                return;
            }

            distance += stepDistance;
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
            speed = 0f;
            maxDistance = 0f;
            distance = 0f;
            finishMovement(agentState, transform.position, false);
            chargeFinished();
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
