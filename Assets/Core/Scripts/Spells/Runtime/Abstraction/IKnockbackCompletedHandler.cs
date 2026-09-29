using UnityEngine;

namespace RPGame.Core.Spells
{
    public interface IKnockbackCompletedHandler : IRuntimeSpellBehavior
    {
        void OnKnockbackCompleted(GameObject target);
    }
}
