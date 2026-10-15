using System.Collections.Generic;
using RPGame.Core.Buildup;
using RPGame.Core.Damage;
using UnityEngine;

namespace RPGame.Enemies
{
    public interface IEnemyProjectile
    {
        void Initialize(
            Vector3 targetPosition,
            IDamageable targetDamageable,
            IReadOnlyList<PartialDamage> damageParts,
            GameObject source,
            BuildupData buildup = null);
    }
}
