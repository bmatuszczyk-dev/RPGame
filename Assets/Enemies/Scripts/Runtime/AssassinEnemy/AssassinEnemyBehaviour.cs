using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class AssassinEnemyBehaviour : IEnemyBehaviour
    {
        private readonly IEnemyDetection detection;
        private readonly IEnemyMovement movement;
        private readonly IEnemyLineOfSight lineOfSight;
        private readonly IEnemyAttack meleeAttack;
        private readonly IEnemyAttack rangedAttack;
        private readonly AssassinEnemyBehaviourConfig config;
        private readonly CombatLeap combatLeap;
        private readonly EnemySearchTarget searchTarget = new();
        private bool canPerformMeleeAttack;
        private bool wasCombatLeapFinished;

        internal AssassinEnemyBehaviour(
            IEnemyDetection detection,
            IEnemyMovement movement,
            IEnemyLineOfSight lineOfSight,
            IEnemyAttack meleeAttack,
            IEnemyAttack rangedAttack,
            AssassinEnemyBehaviourConfig config,
            CombatLeap combatLeap)
        {
            this.detection = detection;
            this.movement = movement;
            this.lineOfSight = lineOfSight;
            this.meleeAttack = meleeAttack;
            this.rangedAttack = rangedAttack;
            this.config = config;
            this.combatLeap = combatLeap;
        }

        public void Tick(float deltaTime)
        {
            meleeAttack.Tick(deltaTime);
            rangedAttack.Tick(deltaTime);
            combatLeap.Tick(deltaTime);
            if (combatLeap.LeapFinished && !wasCombatLeapFinished)
            {
                canPerformMeleeAttack = true;
            }

            wasCombatLeapFinished = combatLeap.LeapFinished;

            if (movement.IsLeaping || movement.IsMovementBlocked)
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

            if (canPerformMeleeAttack && meleeAttack.IsInRange(target))
            {
                movement.Stop();
                if (TryAttack(meleeAttack, target))
                {
                    canPerformMeleeAttack = false;
                }

                return;
            }

            float distance = Vector3.Distance(movement.Position, target.Position);
            if (combatLeap.CanStart(distance)
                && lineOfSight.HasLineOfSight(target.Position)
                && combatLeap.TryStart(target.Position, deltaTime))
            {
                canPerformMeleeAttack = false;
                wasCombatLeapFinished = false;
                return;
            }

            if (distance >= config.RangedMinDistance)
            {
                movement.Stop();
                TryAttack(rangedAttack, target);
                return;
            }

            MaintainRangedDistance(target.Position);
        }

        private bool TryAttack(IEnemyAttack attack, SelectedTarget target)
        {
            return attack.TryAttack(target);
        }

        private void MaintainRangedDistance(Vector3 targetPosition)
        {
            Vector3 direction = movement.Position - targetPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                movement.Stop();
                return;
            }

            Vector3 desiredPosition = targetPosition + direction.normalized * config.RangedMinDistance;
            if (movement.TryResolvePosition(desiredPosition, out Vector3 validPosition))
            {
                movement.MoveTo(validPosition);
                return;
            }

            movement.Stop();
        }
    }
}
