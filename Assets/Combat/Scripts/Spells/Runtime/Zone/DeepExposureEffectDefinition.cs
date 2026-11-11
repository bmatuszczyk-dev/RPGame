using System.Collections.Generic;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(
        fileName = "DeepExposureEffect",
        menuName = "RPGame/Progression/Effects/Deep Exposure")]
    public sealed class DeepExposureEffectDefinition : RuntimeBehaviorModifierEffectDefinition
    {
        [SerializeField, Min(0f)] private float weaknessDurationBonus = 2f;

        public override bool Supports(Spell spell)
        {
            return spell is EarthZoneSpell;
        }

        public override void ModifyRuntimeBehaviors(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            Spell spell,
            GameObject casterObject)
        {
            if (!Supports(spell) || behaviors == null || weaknessDurationBonus <= 0f)
            {
                return;
            }

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is ExposureBehavior exposure)
                {
                    exposure.AddWeaknessDuration(weaknessDurationBonus);
                }
            }
        }

        public override string ToString()
        {
            return $"Deep Exposure +{Mathf.Max(0f, weaknessDurationBonus):0.##}s";
        }

        private void OnValidate()
        {
            weaknessDurationBonus = Mathf.Max(0f, weaknessDurationBonus);
        }
    }
}
