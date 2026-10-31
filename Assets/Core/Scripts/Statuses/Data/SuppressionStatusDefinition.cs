using UnityEngine;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "SuppressionStatus", menuName = "RPGame/Statuses/Suppression")]
    public sealed class SuppressionStatusDefinition : StatusDefinition
    {
        [SerializeField, Range(0f, 1f)] private float damageReductionPercent = 0.25f;

        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Refresh;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.SingleInstance;
        public float DamageReductionPercent => Mathf.Clamp01(damageReductionPercent);

        public override void OnApply(StatusTarget target, StatusInstance instance)
        {
            instance.SetValue(DamageReductionPercent);
        }

        public override void OnRefresh(StatusTarget target, StatusInstance instance)
        {
            instance.SetValue(DamageReductionPercent);
        }

        public override string ToString()
        {
            return $"Suppression -{DamageReductionPercent * 100f:0.#}% damage";
        }

        private void OnValidate()
        {
            damageReductionPercent = Mathf.Clamp01(damageReductionPercent);
        }
    }
}
