using System.Collections.Generic;
using UnityEngine;

namespace RPGame.Core.Spells
{
    public static class KnockbackCollisionDispatcher
    {
        public static void Dispatch(
            IReadOnlyList<IRuntimeSpellBehavior> runtimeBehaviors,
            GameObject target,
            Collider obstacle,
            Vector3 point)
        {
            if (runtimeBehaviors == null)
            {
                return;
            }

            RuntimeSpellBehaviorExecutor.Execute<IKnockbackCollisionHandler>(
                runtimeBehaviors,
                handler => handler.OnKnockbackCollision(target, obstacle, point));
        }
    }
}
