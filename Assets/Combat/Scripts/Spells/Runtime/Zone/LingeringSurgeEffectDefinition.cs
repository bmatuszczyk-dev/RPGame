using System.Collections.Generic;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(
        fileName = "LingeringSurgeEffect",
        menuName = "RPGame/Progression/Effects/Lingering Surge")]
    public sealed class LingeringSurgeEffectDefinition : RuntimeBehaviorModifierEffectDefinition
    {
        [SerializeField, Min(0f)] private float durationBonus = 0.5f;

        public override bool Supports(Spell spell)
        {
            return spell is LightningZoneSpell;
        }

        public override void ModifyRuntimeBehaviors(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            Spell spell,
            GameObject casterObject)
        {
            if (!Supports(spell) || behaviors == null || durationBonus <= 0f)
            {
                return;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is ManaSurgeBehavior manaSurge)
                {
                    manaSurge.AddDurationMultiplierBonus(durationBonus);
                }
            }
        }

        public override string ToString()
        {
            return $"Lingering Surge +{Mathf.Max(0f, durationBonus) * 100f:0.#}% Mana Surge duration";
        }

        private void OnValidate()
        {
            durationBonus = Mathf.Max(0f, durationBonus);
        }
    }
}
