using System.Collections.Generic;
using RPGame.Core.Movement;
using UnityEngine;

namespace RPGame.Core.Spells
{
    public static class KnockbackEndDispatcher
    {
        public static void Dispatch(
            IReadOnlyList<IRuntimeSpellBehavior> runtimeBehaviors,
            GameObject target,
            KnockbackEndContext context)
        {
            if (runtimeBehaviors == null)
            {
                return;
            }

            switch (context.Reason)
            {
                case KnockbackEndReason.Collision:
                    DispatchCollision(runtimeBehaviors, target, context.Obstacle, context.CollisionPoint);
                    break;
                case KnockbackEndReason.Completed:
                    DispatchCompleted(runtimeBehaviors, target);
                    break;
            }
        }

        private static void DispatchCollision(
            IReadOnlyList<IRuntimeSpellBehavior> runtimeBehaviors,
            GameObject target,
            Collider obstacle,
            Vector3 point)
        {
            for (int i = 0; i < runtimeBehaviors.Count; i++)
            {
                if (runtimeBehaviors[i] is IKnockbackCollisionHandler handler)
                {
                    handler.OnKnockbackCollision(target, obstacle, point);
                }
            }
        }

        private static void DispatchCompleted(
            IReadOnlyList<IRuntimeSpellBehavior> runtimeBehaviors,
            GameObject target)
        {
            for (int i = 0; i < runtimeBehaviors.Count; i++)
            {
                if (runtimeBehaviors[i] is IKnockbackCompletedHandler handler)
                {
                    handler.OnKnockbackCompleted(target);
                }
            }
        }
    }
}
