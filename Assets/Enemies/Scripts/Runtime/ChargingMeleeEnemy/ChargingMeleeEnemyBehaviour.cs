using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class ChargingMeleeEnemyBehaviour : IEnemyBehaviour
    {
        private readonly IEnemyDetection detection;
        private readonly MeleeEnemyBehaviour meleeBehaviour;
        private readonly Charge charge;

        public ChargingMeleeEnemyBehaviour(
            IEnemyDetection detection,
            IEnemyMovement movement,
            IEnemyAttack attack,
            ChargingMeleeEnemyBehaviourConfig config,
            GameObject source)
        {
            this.detection = detection;
            meleeBehaviour = new MeleeEnemyBehaviour(detection, movement, attack);
            charge = new Charge(config, movement, source);
        }

        public void Tick(float deltaTime)
        {
            detection.TryGetTarget(out SelectedTarget target);
            charge.Tick(deltaTime, target);
            if (!charge.HasControl)
            {
                meleeBehaviour.Tick(deltaTime);
            }
        }
    }
}
