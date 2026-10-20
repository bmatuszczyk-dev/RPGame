using RPGame.Core.Buildup;
using UnityEngine;
using UnityEngine.UI;

namespace RPGame.UI.Buildup
{
    public sealed class BuildupBarUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image fill;

        public void Initialize(BuildupDefinition definition, BuildupState state)
        {
            if (icon != null)
            {
                icon.sprite = definition != null ? definition.Icon : null;
            }

            SetState(definition, state);
        }

        public void SetState(BuildupDefinition definition, BuildupState state)
        {
            if (fill != null && definition != null)
            {
                fill.color = state == BuildupState.EffectActive
                    ? definition.EffectActiveColor
                    : definition.FillColor;
            }
        }

        public void SetProgress(float current, float threshold)
        {
            if (fill != null)
            {
                fill.fillAmount = threshold > 0f
                    ? Mathf.Clamp01(current / threshold)
                    : 0f;
            }
        }
    }
}
