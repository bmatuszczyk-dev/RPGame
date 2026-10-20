using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class AssassinEnemyBehaviour : IEnemyBehaviour
    {
        private readonly IEnemyDetection detection;
        private readonly IEnemyMovement movement;
        private readonly IEnemyLineOfSight lineOfSight;
        private readonly IEnemyAttack slashAttack;
        private readonly IEnemyAttack rangedAttack;
        private readonly AssassinEnemyBehaviourConfig config;
        private readonly CombatLeap combatLeap;
        private readonly EnemySearchTarget searchTarget = new();
        private bool hasPendingSlash;
        private bool wasCombatLeapFinished;
        private readonly int strafeDirection = Random.value < 0.5f ? -1 : 1;

        internal AssassinEnemyBehaviour(
            IEnemyDetection detection,
            IEnemyMovement movement,
            IEnemyLineOfSight lineOfSight,
            IEnemyAttack slashAttack,
            IEnemyAttack rangedAttack,
            AssassinEnemyBehaviourConfig config,
            CombatLeap combatLeap)
        {
            this.detection = detection;
            this.movement = movement;
            this.lineOfSight = lineOfSight;
            this.slashAttack = slashAttack;
            this.rangedAttack = rangedAttack;
            this.config = config;
            this.combatLeap = combatLeap;
        }

        public void Tick(float deltaTime)
        {
            slashAttack.Tick(deltaTime);
            rangedAttack.Tick(deltaTime);
            combatLeap.Tick(deltaTime);
            if (combatLeap.LeapFinished && !wasCombatLeapFinished)
            {
                hasPendingSlash = true;
            }

            wasCombatLeapFinished = combatLeap.LeapFinished;

            if (slashAttack is AssassinSlashAttack { IsActive: true }
                || movement.IsLeaping
                || movement.IsMovementBlocked)
            {
                return;
            }

            if (!detection.TryGetTarget(out SelectedTarget target))
            {
                searchTarget.MoveTowardsPlayerArea(movement);
                return;
            }

            if (!target.IsValid)
            {
                searchTarget.Reset();
                movement.Stop();
                return;
            }

            searchTarget.Reset();
            movement.FaceTowards(target.Position);

            if (hasPendingSlash)
            {
                movement.Stop();
                if (slashAttack.TryAttack(target))
                {
                    hasPendingSlash = false;
                }

                return;
            }

            float distance = Vector3.Distance(movement.Position, target.Position);
            if (distance > config.CombatLeapMaxDistance)
            {
                movement.MoveTo(target.Position);
                rangedAttack.TryAttack(target);
                return;
            }

            if (distance < config.CombatLeapMinDistance)
            {
                MoveToPreferredDistance(target.Position);
                rangedAttack.TryAttack(target);
                return;
            }

            if (combatLeap.CanStart(distance)
                && lineOfSight.HasLineOfSight(target.Position)
                && combatLeap.TryStart(target.Position, deltaTime))
            {
                hasPendingSlash = false;
                wasCombatLeapFinished = false;
                return;
            }

            Strafe(target.Position, distance);
            rangedAttack.TryAttack(target);
        }

        private void MoveToPreferredDistance(Vector3 targetPosition)
        {
            Vector3 direction = movement.Position - targetPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                movement.Stop();
                return;
            }

            Vector3 desiredPosition = targetPosition + direction.normalized * config.PreferredDistanceFromPlayer;
            if (TryMoveTo(desiredPosition))
            {
                return;
            }

            movement.Stop();
        }

        private void Strafe(Vector3 targetPosition, float distance)
        {
            Vector3 radialDirection = movement.Position - targetPosition;
            radialDirection.y = 0f;
            if (radialDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                MoveToPreferredDistance(targetPosition);
                return;
            }

            radialDirection.Normalize();
            Vector3 tangentDirection = Vector3.Cross(Vector3.up, radialDirection) * strafeDirection;
            float radialCorrection = config.PreferredDistanceFromPlayer - distance;
            Vector3 desiredPosition = movement.Position
                + tangentDirection * config.StrafeLookAheadDistance
                + radialDirection * radialCorrection;
            if (TryMoveTo(desiredPosition))
            {
                return;
            }

            desiredPosition = movement.Position
                - tangentDirection * config.StrafeLookAheadDistance
                + radialDirection * radialCorrection;
            if (TryMoveTo(desiredPosition))
            {
                return;
            }

            movement.Stop();
        }

        private bool TryMoveTo(Vector3 desiredPosition)
        {
            if (!movement.TryResolvePosition(desiredPosition, out Vector3 validPosition))
            {
                return false;
            }

            movement.MoveTo(validPosition);
            return true;
        }
    }
}
