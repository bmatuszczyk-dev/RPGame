using System.Collections.Generic;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(
        fileName = "OverpressureEffect",
        menuName = "RPGame/Progression/Effects/Overpressure")]
    public sealed class OverpressureEffectDefinition : RuntimeBehaviorModifierEffectDefinition
    {
        [SerializeField, Min(0f)] private float damageBonus = 0.5f;

        public override bool Supports(Spell spell)
        {
            return spell is FireZoneSpell;
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
                if (behaviors[i] is DetonationBehavior detonation)
                {
                    detonation.AddDamageMultiplierBonus(damageBonus);
                }
            }
        }

        public override string ToString()
        {
            return $"Overpressure +{Mathf.Max(0f, damageBonus) * 100f:0.#}% Detonation damage";
        }

        private void OnValidate()
        {
            damageBonus = Mathf.Max(0f, damageBonus);
        }
    }
}
