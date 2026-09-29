using UnityEngine;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "WeaknessStatus", menuName = "RPGame/Statuses/Weakness")]
    public sealed class WeaknessStatusDefinition : StatusDefinition
    {
        [SerializeField] private float stackValue = 0.1f;
        [SerializeField] private int maxStack = 3;

        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Refresh;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.SingleInstance;

        public override void OnApply(StatusTarget target, StatusInstance instance)
        {
            instance.SetValue(Mathf.Min(MaxValue, StackValue));
        }

        public override void OnRefresh(StatusTarget target, StatusInstance instance)
        {
            instance.SetValue(Mathf.Min(MaxValue, instance.Value + StackValue));
        }

        public override string ToString()
        {
            return $"Weakness +{StackValue * 100f:0.#}% x{MaxStack}";
        }

        private float StackValue => Mathf.Max(0f, stackValue);
        private int MaxStack => Mathf.Max(0, maxStack);
        private float MaxValue => StackValue * MaxStack;

        private void OnValidate()
        {
            stackValue = Mathf.Max(0f, stackValue);
            maxStack = Mathf.Max(0, maxStack);
        }
    }
}
