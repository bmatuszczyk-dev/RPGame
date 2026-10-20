using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class HealerBehaviourCreator : EnemyBehaviourCreator
    {
        protected override bool TryCreateBehaviour(
            Config config,
            Detection detection,
            Movement movement,
            Attack attack,
            out IEnemyBehaviour behaviour)
        {
            behaviour = null;
            if (config.BehaviourConfig is not HealerEnemyBehaviourConfig healerConfig)
            {
                Debug.LogError("HealerBehaviourCreator requires HealerEnemyBehaviourConfig.", this);
                return false;
            }

            HealerTargetSensor healerTargetSensor = GetComponentInChildren<HealerTargetSensor>(true);
            if (healerTargetSensor == null
                || !TryGetRequiredComponent(out LineOfSight lineOfSight, "lineOfSight")
                || !TryGetRequiredComponent(out StatisticsController statistics, "statisticsController")
                || !TryGetRequiredComponent(out EnemyTargetable targetable, "enemyTargetable"))
            {
                Debug.LogError("Missing healer dependencies.", this);
                return false;
            }

            healerTargetSensor.SetSearchRadius(healerConfig.AllySearchRange);
            behaviour = new HealerEnemyBehaviour(
                detection,
                movement,
                healerTargetSensor,
                lineOfSight,
                statistics,
                healerConfig,
                targetable);
            return true;
        }
    }
}
