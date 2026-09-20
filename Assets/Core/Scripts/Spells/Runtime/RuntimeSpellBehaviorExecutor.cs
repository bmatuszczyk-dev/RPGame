using System;
using System.Collections.Generic;

namespace RPGame.Core.Spells
{
    public static class RuntimeSpellBehaviorExecutor
    {
        private static readonly RuntimeSpellBehaviorPhase[] PhaseOrder =
        {
            RuntimeSpellBehaviorPhase.PreResolve,
            RuntimeSpellBehaviorPhase.MarkConsumption,
            RuntimeSpellBehaviorPhase.PrimaryEffect,
            RuntimeSpellBehaviorPhase.PostEffect,
            RuntimeSpellBehaviorPhase.MarkProgress,
            RuntimeSpellBehaviorPhase.Aftermath
        };

        public static void Execute<TBehavior>(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            Action<TBehavior> execute)
            where TBehavior : class, IRuntimeSpellBehavior
        {
            if (behaviors == null || execute == null)
            {
                return;
            }

            for (int phaseIndex = 0; phaseIndex < PhaseOrder.Length; phaseIndex++)
            {
                RuntimeSpellBehaviorPhase phase = PhaseOrder[phaseIndex];
                for (int behaviorIndex = 0; behaviorIndex < behaviors.Count; behaviorIndex++)
                {
                    if (behaviors[behaviorIndex] is TBehavior behavior
                        && behavior.Phase == phase)
                    {
                        execute(behavior);
                    }
                }
            }
        }
    }
}
