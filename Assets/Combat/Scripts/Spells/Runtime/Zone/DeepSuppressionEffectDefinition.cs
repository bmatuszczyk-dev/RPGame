using System.Collections.Generic;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(
        fileName = "DeepSuppressionEffect",
        menuName = "RPGame/Progression/Effects/Deep Suppression")]
    public sealed class DeepSuppressionEffectDefinition : RuntimeBehaviorModifierEffectDefinition
    {
        [SerializeField, Min(0f)] private float suppressionDurationBonus = 2f;

        public override bool Supports(Spell spell)
        {
            return spell is EarthZoneSpell;
        }

        public override void ModifyRuntimeBehaviors(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            Spell spell,
            GameObject casterObject)
        {
            if (!Supports(spell) || behaviors == null || suppressionDurationBonus <= 0f)
            {
                return;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is SuppressionBehavior suppression)
                {
                    suppression.AddSuppressionDuration(suppressionDurationBonus);
                }
            }
        }

        public override string ToString()
        {
            return $"Deep Suppression +{Mathf.Max(0f, suppressionDurationBonus):0.##}s";
        }

        private void OnValidate()
        {
            suppressionDurationBonus = Mathf.Max(0f, suppressionDurationBonus);
        }
    }
}
