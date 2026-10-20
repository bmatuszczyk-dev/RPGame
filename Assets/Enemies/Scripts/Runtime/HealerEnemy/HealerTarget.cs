using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class HealerTarget
    {
        public HealerTarget(EnemyTargetable targetable, IStatisticsController statistics)
        {
            Targetable = targetable;
            Statistics = statistics;
        }

        public EnemyTargetable Targetable { get; }
        public IStatisticsController Statistics { get; }
        public Vector3 Position => Targetable.TargetPoint != null ? Targetable.TargetPoint.position : Targetable.transform.position;
        public Transform TargetPoint => Targetable.TargetPoint;
        public bool IsValid => Targetable != null
            && Targetable.isActiveAndEnabled
            && Targetable.gameObject.activeInHierarchy
            && Statistics != null
            && Statistics.IsAlive;
    }
}
