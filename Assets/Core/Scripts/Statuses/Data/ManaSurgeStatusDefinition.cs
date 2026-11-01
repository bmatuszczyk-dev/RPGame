using RPGame.Core.Effects;
using UnityEngine;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "ManaSurgeStatus", menuName = "RPGame/Statuses/Mana Surge")]
    public sealed class ManaSurgeStatusDefinition : StatusDefinition, IStatusStatModifier
    {
        [SerializeField, Min(0f)] private float manaRegenerationPerStack = 1f;
        [SerializeField, Min(1)] private int maxStacks = 5;

        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.StackAndRefresh;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.SingleInstance;
        public EffectStat Stat => EffectStat.ManaRegeneration;
        public EffectModifierType ModifierType => EffectModifierType.Flat;
        public float ManaRegenerationPerStack => Mathf.Max(0f, manaRegenerationPerStack);

        public override void OnApply(StatusTarget target, StatusInstance instance)
        {
            instance.SetCurrentStacks(1);
            if (!instance.HasValueOverride)
            {
                instance.SetValue(ManaRegenerationPerStack);
            }
        }

        public override void OnRefresh(StatusTarget target, StatusInstance instance)
        {
            instance.IncrementCurrentStacks(MaxStacks);
        }

        public override void ApplySingleInstanceReapply(
            StatusTarget target,
            StatusInstance existingInstance,
            float incomingDuration,
            StatusContext incomingContext)
        {
            existingInstance.StackAndRefresh(target, incomingDuration);
        }

        public float GetValue(StatusInstance instance)
        {
            return instance != null ? instance.CurrentStacks * instance.Value : 0f;
        }

        public override string ToString()
        {
            return $"Mana Surge +{ManaRegenerationPerStack:0.##} Mana/s x{MaxStacks}";
        }

        private int MaxStacks => Mathf.Max(1, maxStacks);

        private void OnValidate()
        {
            manaRegenerationPerStack = Mathf.Max(0f, manaRegenerationPerStack);
            maxStacks = Mathf.Max(1, maxStacks);
        }
    }
}
