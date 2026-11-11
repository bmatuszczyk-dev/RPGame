using System.Collections.Generic;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Core.Effects
{
    [CreateAssetMenu(
        fileName = "StatusDurationModifierEffect",
        menuName = "RPGame/Progression/Effects/Status Duration Modifier")]
    public sealed class StatusDurationModifierEffectDefinition : PassiveEffectDefinition
    {
        [SerializeField] private List<StatusDefinition> statuses = new();
        [SerializeField] private EffectModifierType modifierType = EffectModifierType.Percent;
        [SerializeField] private float value = 0.15f;

        public EffectModifierType ModifierType => modifierType;
        public float Value => value;

        public bool Supports(StatusDefinition status)
        {
            return status != null && statuses.Contains(status);
        }

        public override string ToString()
        {
            float displayValue = modifierType == EffectModifierType.Percent
                ? value * 100f
                : value;
            string suffix = modifierType == EffectModifierType.Percent ? "%" : string.Empty;
            return $"Status duration {displayValue:+0.##;-0.##;0}{suffix}";
        }
    }
}
