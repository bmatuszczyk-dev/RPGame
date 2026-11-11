using RPGame.Core.Spells;

namespace RPGame.Core.Effects
{
    internal readonly struct RuntimeSpellBehaviorOrderEntry
    {
        public RuntimeSpellBehaviorOrderEntry(
            IRuntimeSpellBehavior behavior,
            int executionOrder)
        {
            Behavior = behavior;
            ExecutionOrder = executionOrder;
        }

        public IRuntimeSpellBehavior Behavior { get; }
        public int ExecutionOrder { get; }
    }
}
