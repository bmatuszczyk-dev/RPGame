using System.Collections.Generic;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(
        fileName = "EmpoweredSurgeEffect",
        menuName = "RPGame/Progression/Effects/Empowered Surge")]
    public sealed class EmpoweredSurgeEffectDefinition : RuntimeBehaviorModifierEffectDefinition
    {
        [SerializeField, Min(0f)] private float manaRegenerationBonus = 0.5f;

        public override bool Supports(Spell spell)
        {
            return spell is LightningZoneSpell;
        }

        public override void ModifyRuntimeBehaviors(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            Spell spell,
            GameObject casterObject)
        {
            if (!Supports(spell) || behaviors == null || manaRegenerationBonus <= 0f)
            {
                return;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is ManaSurgeBehavior manaSurge)
                {
                    manaSurge.AddManaRegenerationMultiplierBonus(manaRegenerationBonus);
                }
            }
        }

        public override string ToString()
        {
            return $"Empowered Surge +{Mathf.Max(0f, manaRegenerationBonus) * 100f:0.#}% Mana Surge regeneration";
        }

        private void OnValidate()
        {
            manaRegenerationBonus = Mathf.Max(0f, manaRegenerationBonus);
        }
    }
}
