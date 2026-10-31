using System.Collections.Generic;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(
        fileName = "OverchargeEffect",
        menuName = "RPGame/Progression/Effects/Overcharge")]
    public sealed class OverchargeEffectDefinition : RuntimeBehaviorModifierEffectDefinition
    {
        [SerializeField, Min(0f)] private float damageBonus = 0.25f;

        public override bool Supports(Spell spell)
        {
            return spell is LightningZoneSpell;
        }

        public override void ModifyRuntimeBehaviors(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            Spell spell,
            GameObject casterObject)
        {
            if (!Supports(spell) || behaviors == null || damageBonus <= 0f)
            {
                return;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is DischargeBehavior discharge)
                {
                    discharge.AddDamageMultiplierBonus(damageBonus);
                }
            }
        }

        public override string ToString()
        {
            return $"Overcharge +{Mathf.Max(0f, damageBonus) * 100f:0.#}% Discharge damage";
        }

        private void OnValidate()
        {
            damageBonus = Mathf.Max(0f, damageBonus);
        }
    }
}
