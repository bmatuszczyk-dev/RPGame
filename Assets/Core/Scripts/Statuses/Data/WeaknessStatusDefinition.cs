using UnityEngine;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "WeaknessStatus", menuName = "RPGame/Statuses/Weakness")]
    public sealed class WeaknessStatusDefinition : StatusDefinition, IStackableStatus
    {
        [SerializeField] private float stackValue = 0.1f;
        [SerializeField] private int maxStack = 3;

        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.StackAndRefresh;
        public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.SingleInstance;
        public int MaxStacks => Mathf.Max(1, maxStack);

        public override void OnApply(StatusTarget target, StatusInstance instance)
        {
            UpdateValue(instance);
        }

        public override void OnRefresh(
            StatusTarget target,
            StatusInstance instance,
            StatusContext context)
        {
            UpdateValue(instance);
        }

        public override string ToString()
        {
            return $"Weakness +{StackValue * 100f:0.#}% x{MaxStacks}";
        }

        private float StackValue => Mathf.Max(0f, stackValue);
        private void UpdateValue(StatusInstance instance)
        {
            instance.SetValue(instance.CurrentStacks * StackValue);
        }

        private void OnValidate()
        {
            stackValue = Mathf.Max(0f, stackValue);
            maxStack = Mathf.Max(1, maxStack);
        }
    }
}
