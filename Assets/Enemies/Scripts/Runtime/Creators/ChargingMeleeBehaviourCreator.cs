using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class ChargingMeleeBehaviourCreator : EnemyBehaviourCreator
    {
        protected override bool TryCreateBehaviour(
            Config config,
            Detection detection,
            Movement movement,
            Attack attack,
            out IEnemyBehaviour behaviour)
        {
            behaviour = null;
            if (config.BehaviourConfig is not ChargingMeleeEnemyBehaviourConfig chargingConfig)
            {
                Debug.LogError("ChargingMeleeBehaviourCreator requires ChargingMeleeEnemyBehaviourConfig.", this);
                return false;
            }

            if (!TryGetAttack(attack, AttackType.Melee, out IEnemyAttack meleeAttack))
            {
                return false;
            }

            behaviour = new ChargingMeleeEnemyBehaviour(detection, movement, meleeAttack, chargingConfig, gameObject);
            return true;
        }
    }
}
