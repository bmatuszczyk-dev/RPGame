using System.Collections.Generic;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(
        fileName = "BlastRadiusEffect",
        menuName = "RPGame/Progression/Effects/Blast Radius")]
    public sealed class BlastRadiusEffectDefinition : RuntimeBehaviorModifierEffectDefinition
    {
        [SerializeField, Min(0f)] private float radiusBonus = 0.5f;

        public override bool Supports(Spell spell)
        {
            return spell is FireZoneSpell;
        }

        public override void ModifyRuntimeBehaviors(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            Spell spell,
            GameObject casterObject)
        {
            if (!Supports(spell) || behaviors == null || radiusBonus <= 0f)
            {
                return;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is DetonationBehavior detonation)
                {
                    detonation.AddRadiusMultiplierBonus(radiusBonus);
                }
            }
        }

        public override string ToString()
        {
            return $"Blast Radius +{Mathf.Max(0f, radiusBonus) * 100f:0.#}% Detonation radius";
        }

        private void OnValidate()
        {
            radiusBonus = Mathf.Max(0f, radiusBonus);
        }
    }
}
