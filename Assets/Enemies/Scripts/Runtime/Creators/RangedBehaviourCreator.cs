using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class RangedBehaviourCreator : EnemyBehaviourCreator
    {
        protected override bool TryCreateBehaviour(
            Config config,
            Detection detection,
            Movement movement,
            Attack attack,
            out IEnemyBehaviour behaviour)
        {
            behaviour = null;
            if (config.BehaviourConfig is not RangedEnemyBehaviourConfig rangedConfig)
            {
                Debug.LogError("RangedBehaviourCreator requires RangedEnemyBehaviourConfig.", this);
                return false;
            }

            if (!TryGetAttack(attack, AttackType.StraightProjectile, out IEnemyAttack straightAttack)
                || !TryGetAttack(attack, AttackType.ParabolicProjectile, out IEnemyAttack parabolicAttack)
                || !TryGetRequiredComponent(out LineOfSight lineOfSight, "lineOfSight"))
            {
                return false;
            }

            behaviour = new RangedEnemyBehaviour(
                detection,
                movement,
                rangedConfig,
                lineOfSight,
                straightAttack,
                parabolicAttack);
            return true;
        }
    }
}
