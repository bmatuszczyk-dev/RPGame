namespace RPGame.Enemies
{
    public sealed class MeleeBehaviourCreator : EnemyBehaviourCreator
    {
        protected override bool TryCreateBehaviour(
            Config config,
            Detection detection,
            Movement movement,
            Attack attack,
            out IEnemyBehaviour behaviour)
        {
            behaviour = null;
            if (config.BehaviourConfig is not MeleeEnemyBehaviourConfig)
            {
                UnityEngine.Debug.LogError("MeleeBehaviourCreator requires MeleeEnemyBehaviourConfig.", this);
                return false;
            }

            if (!TryGetAttack(attack, AttackType.Melee, out IEnemyAttack meleeAttack))
            {
                return false;
            }

            behaviour = new MeleeEnemyBehaviour(detection, movement, meleeAttack);
            return true;
        }
    }
}
