using RPGame.Core.Spells;

namespace RPGame.Combat.Spells
{
    internal sealed class MarkActivationState : IExecutionState
    {
        private bool payoffConsumed;

        public bool TryConsumePayoff()
        {
            if (payoffConsumed)
            {
                return false;
            }

            payoffConsumed = true;
            return true;
        }
    }
}
