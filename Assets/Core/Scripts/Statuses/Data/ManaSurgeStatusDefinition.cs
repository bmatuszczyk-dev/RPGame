using RPGame.Core.Effects;
using UnityEngine;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "ManaSurgeStatus", menuName = "RPGame/Statuses/Mana Surge")]
    public sealed class ManaSurgeStatusDefinition :
        StatusDefinition,
        IStatusStatModifier,
        IStackableStatus
    {
        [SerializeField, Min(0f)] private float manaRegenerationPerStack = 1f;
        [SerializeField, Min(1)] private int maxStacks = 5;

        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.StackAndRefresh;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.SingleInstance;
        public EffectStat Stat => EffectStat.ManaRegeneration;
        public EffectModifierType ModifierType => EffectModifierType.Flat;
        public float ManaRegenerationPerStack => Mathf.Max(0f, manaRegenerationPerStack);
        public int MaxStacks => Mathf.Max(1, maxStacks);

        public override void OnApply(StatusTarget target, StatusInstance instance)
        {
            if (!instance.HasValueOverride)
            {
                instance.SetValue(ManaRegenerationPerStack);
            }
        }

        public float GetValue(StatusInstance instance)
        {
            return instance != null ? instance.CurrentStacks * instance.Value : 0f;
        }

        public override string ToString()
        {
            return $"Mana Surge +{ManaRegenerationPerStack:0.##} Mana/s x{MaxStacks}";
        }

        private void OnValidate()
        {
            manaRegenerationPerStack = Mathf.Max(0f, manaRegenerationPerStack);
            maxStacks = Mathf.Max(1, maxStacks);
        }
    }
}
