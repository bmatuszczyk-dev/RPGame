namespace RPGame.Enemies
{
    public sealed class MeleeEnemyBehaviour : IEnemyBehaviour
    {
        private readonly IEnemyDetection detection;
        private readonly IEnemyMovement movement;
        private readonly IEnemyAttack attack;
        private readonly EnemySearchTarget searchTarget = new();

        public MeleeEnemyBehaviour(
            IEnemyDetection detection,
            IEnemyMovement movement,
            IEnemyAttack attack)
        {
            this.detection = detection;
            this.movement = movement;
            this.attack = attack;
        }

        public void Tick(float deltaTime)
        {
            attack.Tick(deltaTime);

            if (!detection.TryGetTarget(out SelectedTarget target))
            {
                searchTarget.MoveTowardsPlayerArea(movement);
                return;
            }

            searchTarget.Reset();
            movement.FaceTowards(target.Position);

            if (attack.IsInRange(target))
            {
                movement.Stop();
                attack.TryAttack(target);
                return;
            }

            movement.MoveTo(target.Position);
        }
    }
}
