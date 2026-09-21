using System;
using System.Collections.Generic;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    public static class SpellBehaviorPipelineExecutor
    {
        public static bool Resolve(
            CasterData casterData,
            GameObject target,
            ISpellResolveBehavior resolveBehavior)
        {
            if (target == null || resolveBehavior == null)
            {
                return false;
            }

            IStatusReceiver statusReceiver = target.GetComponentInParent<IStatusReceiver>();
            SpellBehaviorContext context = new(
                target,
                casterData.CasterObject,
                casterData.SpellId,
                statusReceiver);

            ExecuteTargetPhase(casterData, SpellBehaviorPhase.PreResolve, context);
            ExecuteMarkConsumers(casterData, context);

            if (resolveBehavior.Phase != SpellBehaviorPhase.Resolve
                || !resolveBehavior.Resolve(context))
            {
                return false;
            }

            ExecuteTargetPhase(casterData, SpellBehaviorPhase.PostResolve, context);
            ExecuteTargetPhase(casterData, SpellBehaviorPhase.Effect, context);
            RecordMarkProgress(context);
            return true;
        }

        private static void ExecuteTargetPhase(
            CasterData casterData,
            SpellBehaviorPhase phase,
            SpellBehaviorContext context)
        {
            ExecutePhase(
                casterData.RuntimeBehaviors,
                phase,
                behavior => behavior.Execute(context));
        }

        private static void ExecuteMarkConsumers(
            CasterData casterData,
            SpellBehaviorContext context)
        {
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = casterData.RuntimeBehaviors;
            if (behaviors == null)
            {
                return;
            }

            for (int behaviorIndex = 0; behaviorIndex < behaviors.Count; behaviorIndex++)
            {
                if (behaviors[behaviorIndex] is IMarkConsumer markConsumer)
                {
                    markConsumer.TryConsumeMark(context);
                }
            }
        }

        private static void ExecutePhase(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            SpellBehaviorPhase phase,
            Action<ISpellBehavior> execute)
        {
            if (behaviors == null || execute == null)
            {
                return;
            }

            for (int behaviorIndex = 0; behaviorIndex < behaviors.Count; behaviorIndex++)
            {
                IRuntimeSpellBehavior runtimeBehavior = behaviors[behaviorIndex];
                if (runtimeBehavior is ISpellBehavior behavior
                    && behavior.Phase == phase)
                {
                    execute(behavior);
                }
            }
        }

        private static void RecordMarkProgress(SpellBehaviorContext context)
        {
            MarkProgressReceiver progressReceiver =
                context.Target.GetComponentInParent<MarkProgressReceiver>();
            if (progressReceiver == null)
            {
                return;
            }

            progressReceiver.RecordSuccessfulSpell(
                context.SpellId,
                new StatusContext(new StatusSourceId(context.SpellId.ToString()), context.Source));
        }
    }
}
