using System;
using UnityEngine;

namespace RPGame.Enemies
{
    internal enum LeapMovementState
    {
        None,
        Traversal,
        Combat,
        CombatFalling
    }

    internal delegate bool TryResolveCombatLanding(Vector3 position, out Vector3 landingPosition);

    internal sealed class LeapMovement
    {
        private readonly Transform transform;
        private readonly Func<MovementAgentState> beginMovement;
        private readonly Action<MovementAgentState, Vector3, bool> finishMovement;
        private readonly TryResolveCombatLanding tryResolveCombatLanding;

        private bool isActive;
        private bool traversesOffMeshLink;
        private Vector3 startPosition;
        private Vector3 landingPoint;
        private float duration;
        private float elapsed;
        private float arcHeight;
        private float fallSpeed;
        private Vector3 fallLandingPoint;
        private bool hasFallLandingPoint;
        private MovementAgentState agentState;
        private Action<bool> combatLeapCompleted;

        internal LeapMovementState State { get; private set; }

        public bool IsActive => isActive;
        public bool TraversesOffMeshLink => traversesOffMeshLink;
        public bool IsCombat => State == LeapMovementState.Combat || State == LeapMovementState.CombatFalling;

        public LeapMovement(
            Transform transform,
            Func<MovementAgentState> beginMovement,
            Action<MovementAgentState, Vector3, bool> finishMovement,
            TryResolveCombatLanding tryResolveCombatLanding = null)
        {
            this.transform = transform;
            this.beginMovement = beginMovement;
            this.finishMovement = finishMovement;
            this.tryResolveCombatLanding = tryResolveCombatLanding;
        }

        public void Start(Vector3 destination, float speed, float arcHeight, bool traversesOffMeshLink)
        {
            startPosition = transform.position;
            landingPoint = destination;
            duration = Vector3.Distance(startPosition, landingPoint) / speed;
            elapsed = 0f;
            this.arcHeight = arcHeight;
            this.traversesOffMeshLink = traversesOffMeshLink;
            agentState = beginMovement();
            isActive = true;
            State = LeapMovementState.Traversal;
        }

        public void StartCombat(
            Vector3 destination,
            float speed,
            float arcHeight,
            float fallSpeed,
            Action<bool> onCompleted)
        {
            Start(destination, speed, arcHeight, false);
            State = LeapMovementState.Combat;
            this.fallSpeed = fallSpeed;
            combatLeapCompleted = onCompleted;
        }

        public void Tick(float deltaTime)
        {
            if (!isActive)
            {
                return;
            }

            if (State == LeapMovementState.CombatFalling)
            {
                TickCombatFalling(deltaTime);
                return;
            }

            if (elapsed >= duration)
            {
                Finish(landingPoint, true);
                return;
            }

            elapsed += deltaTime;
            float progress = duration > Mathf.Epsilon ? Mathf.Clamp01(elapsed / duration) : 1f;
            transform.position = Vector3.Lerp(startPosition, landingPoint, progress)
                + Vector3.up * Mathf.Sin(Mathf.PI * progress) * arcHeight;
        }

        public void Cancel()
        {
            if (!isActive)
            {
                return;
            }

            Finish(startPosition, false);
        }

        public void InterruptCombat()
        {
            if (State != LeapMovementState.Combat)
            {
                return;
            }

            State = LeapMovementState.CombatFalling;
            hasFallLandingPoint = tryResolveCombatLanding != null
                && tryResolveCombatLanding(transform.position, out fallLandingPoint);
        }

        private void TickCombatFalling(float deltaTime)
        {
            transform.position += Vector3.down * (fallSpeed * deltaTime);
            if (!hasFallLandingPoint && tryResolveCombatLanding != null)
            {
                hasFallLandingPoint = tryResolveCombatLanding(transform.position, out fallLandingPoint);
            }

            if (hasFallLandingPoint && transform.position.y <= fallLandingPoint.y)
            {
                Finish(fallLandingPoint, false);
            }
        }

        private void Finish(Vector3 position, bool completeTraversal)
        {
            bool completedCombatLeap = State == LeapMovementState.Combat && completeTraversal;
            bool shouldCompleteTraversal = completeTraversal && traversesOffMeshLink;
            Action<bool> onCombatLeapCompleted = combatLeapCompleted;
            isActive = false;
            traversesOffMeshLink = false;
            State = LeapMovementState.None;
            fallSpeed = 0f;
            hasFallLandingPoint = false;
            combatLeapCompleted = null;
            finishMovement(agentState, position, shouldCompleteTraversal);
            onCombatLeapCompleted?.Invoke(completedCombatLeap);
        }
    }
}
