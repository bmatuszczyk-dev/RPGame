using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Core.Buildup
{
    [CreateAssetMenu(fileName = "Buildup", menuName = "RPGame/Core/Buildup Definition")]
    public sealed class BuildupDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color fillColor = Color.white;
        [SerializeField] private Color effectActiveColor = Color.white;
        [SerializeField, Min(0.0001f)] private float threshold = 100f;
        [SerializeField, Min(0f)] private float decayDelay = 3f;
        [SerializeField, Min(0f)] private float decayPerSecond = 10f;
        [SerializeField] private StatusDefinition triggeredStatus;
        [SerializeField, Min(0f)] private float triggeredStatusDuration = 8f;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Color FillColor => fillColor;
        public Color EffectActiveColor => effectActiveColor;
        public float Threshold => threshold;
        public float DecayDelay => decayDelay;
        public float DecayPerSecond => decayPerSecond;
        public StatusDefinition TriggeredStatus => triggeredStatus;
        public float TriggeredStatusDuration => triggeredStatusDuration;

        private void OnValidate()
        {
            threshold = Mathf.Max(0.0001f, threshold);
            decayDelay = Mathf.Max(0f, decayDelay);
            decayPerSecond = Mathf.Max(0f, decayPerSecond);
            triggeredStatusDuration = Mathf.Max(0f, triggeredStatusDuration);
        }
    }
}
