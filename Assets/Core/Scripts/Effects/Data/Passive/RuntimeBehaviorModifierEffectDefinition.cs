using System.Collections.Generic;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Core.Effects
{
    public abstract class RuntimeBehaviorModifierEffectDefinition : PassiveEffectDefinition
    {
        public abstract bool Supports(Spell spell);

        public abstract void ModifyRuntimeBehaviors(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            Spell spell,
            GameObject casterObject);
    }
}
