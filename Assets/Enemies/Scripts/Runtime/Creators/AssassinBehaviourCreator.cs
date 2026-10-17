using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class AssassinBehaviourCreator : EnemyBehaviourCreator
    {
        protected override bool TryCreateBehaviour(
            Config config,
            Detection detection,
            Movement movement,
            Attack attack,
            out IEnemyBehaviour behaviour)
        {
            behaviour = null;
            if (config.BehaviourConfig is not AssassinEnemyBehaviourConfig assassinConfig)
            {
                Debug.LogError("AssassinBehaviourCreator requires AssassinEnemyBehaviourConfig.", this);
                return false;
            }

            if (!TryGetAttack(attack, AttackType.AssassinSlash, out IEnemyAttack slashAttack)
                || !TryGetAttack(attack, AttackType.StraightProjectile, out IEnemyAttack rangedAttack)
                || !TryGetRequiredComponent(out LineOfSight lineOfSight, "lineOfSight"))
            {
                return false;
            }

            behaviour = new AssassinEnemyBehaviour(
                detection,
                movement,
                lineOfSight,
                slashAttack,
                rangedAttack,
                assassinConfig,
                new CombatLeap(assassinConfig, movement));
            return true;
        }
    }
}
