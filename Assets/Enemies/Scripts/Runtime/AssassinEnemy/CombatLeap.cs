using UnityEngine;

namespace RPGame.Enemies
{
    internal sealed class CombatLeap
    {
        private readonly AssassinEnemyBehaviourConfig config;
        private readonly IEnemyMovement movement;
        private float remainingCooldown;
        private float triggerCheckTimer;

        public bool LeapFinished { get; private set; }

        public CombatLeap(AssassinEnemyBehaviourConfig config, IEnemyMovement movement)
        {
            this.config = config;
            this.movement = movement;
        }

        public void Tick(float deltaTime)
        {
            remainingCooldown = Mathf.Max(0f, remainingCooldown - deltaTime);
        }

        public bool CanStart(float distance)
        {
            return remainingCooldown <= 0f
                && distance >= config.CombatLeapMinDistance
                && distance <= config.CombatLeapMaxDistance;
        }

        public bool TryStart(Vector3 targetPosition, float deltaTime)
        {
            float distance = Vector3.Distance(movement.Position, targetPosition);
            if (!CanStart(distance))
            {
                return false;
            }

            triggerCheckTimer -= deltaTime;
            if (triggerCheckTimer > 0f)
            {
                return false;
            }

            triggerCheckTimer = config.CombatLeapTriggerCheckInterval;
            if (config.CombatLeapChance <= 0f
                || (config.CombatLeapChance < 1f
                    && UnityEngine.Random.value > config.CombatLeapChance))
            {
                return false;
            }

            LeapFinished = false;
            if (!movement.TryCombatLeapTo(
                    targetPosition,
                    config.CombatLeapSpeed,
                    config.CombatLeapArcHeight,
                    config.CombatLeapFallSpeed,
                    OnCombatLeapCompleted))
            {
                return false;
            }

            remainingCooldown = config.CombatLeapCooldown;
            return true;
        }

        private void OnCombatLeapCompleted(bool completed)
        {
            LeapFinished = completed;
        }
    }
}
