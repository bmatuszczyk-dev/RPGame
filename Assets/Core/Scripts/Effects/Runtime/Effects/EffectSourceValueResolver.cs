using UnityEngine;
using RPGame.Core.Statuses;

namespace RPGame.Core.Effects
{
    public static class EffectSourceValueResolver
    {
        public static float Resolve(float baseValue, GameObject source, EffectStat stat)
        {
            EffectAggregator aggregator = source != null
                ? source.GetComponentInParent<EffectAggregator>()
                : null;
            if (aggregator == null)
            {
                return Mathf.Max(0f, baseValue);
            }

            float flat = aggregator.GetEffectValue(stat, EffectModifierType.Flat);
            float percent = aggregator.GetEffectValue(stat, EffectModifierType.Percent);
            return Mathf.Max(0f, (baseValue + flat) * Mathf.Max(0f, 1f + percent));
        }

        public static float GetMultiplier(GameObject source, EffectStat stat)
        {
            return GetPercentMultiplier(source, stat);
        }

        public static float GetFlatValue(GameObject source, EffectStat stat)
        {
            EffectAggregator aggregator = GetEffectAggregator(source);
            return aggregator != null
                ? aggregator.GetEffectValue(stat, EffectModifierType.Flat)
                : 0f;
        }

        public static float GetPercentMultiplier(GameObject source, EffectStat stat)
        {
            EffectAggregator aggregator = GetEffectAggregator(source);
            float percent = aggregator != null
                ? aggregator.GetEffectValue(stat, EffectModifierType.Percent)
                : 0f;
            return Mathf.Max(0f, 1f + percent);
        }

        public static float ResolveStatusDuration(
            StatusDefinition status,
            float duration,
            GameObject source)
        {
            EffectAggregator aggregator = GetEffectAggregator(source);
            return aggregator != null
                ? aggregator.ResolveStatusDuration(status, duration)
                : Mathf.Max(0f, duration);
        }

        private static EffectAggregator GetEffectAggregator(GameObject source)
        {
            return source != null
                ? source.GetComponentInParent<EffectAggregator>()
                : null;
        }
    }
}
