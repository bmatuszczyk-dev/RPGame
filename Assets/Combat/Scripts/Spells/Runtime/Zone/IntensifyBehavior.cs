using System;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class IntensifyBehavior :
        IInitializableRuntimeSpellBehavior,
        IStatusApplicationModifier
    {
        public bool Supports(Spell spell)
        {
            return spell is FireZoneSpell;
        }

        public void Initialize(UnityEngine.GameObject caster)
        {
        }

        public StatusContext ModifyStatusApplication(
            StatusDefinition status,
            StatusContext context)
        {
            return status is BurningStatusDefinition
                ? context.WithReapplyPolicyOverride(ReapplyPolicy.StackAndRefresh)
                : context;
        }
    }
}
