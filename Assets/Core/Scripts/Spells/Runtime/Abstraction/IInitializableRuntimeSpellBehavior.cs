using UnityEngine;

namespace RPGame.Core.Spells
{
    public interface IInitializableRuntimeSpellBehavior
    {
        bool Supports(Spell spell);
        void Initialize(GameObject caster);
    }
}
