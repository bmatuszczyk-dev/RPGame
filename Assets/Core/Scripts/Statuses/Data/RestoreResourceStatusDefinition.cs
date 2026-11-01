using RPGame.Core.Statistics;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Core.Statuses
{
    [MovedFrom(true, null, null, "RestoreResourceEffectDefinition")]
    public abstract class RestoreResourceStatusDefinition : StatusDefinition, IAmountStatus
    {
        [SerializeField] private float amount = 25f;

        public float Amount => Mathf.Max(0f, amount);
        public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Accumulate;

        public override bool CanApply(StatusTarget target)
        {
            return target.StatisticsController != null;
        }

        public void Tick(StatusTarget target, float deltaTime, float amount)
        {
            IStatisticsController statisticsController = target.StatisticsController;
            if (statisticsController == null || amount <= 0f)
            {
                return;
            }

            Restore(statisticsController, amount);
        }

        public override string ToString()
        {
            return $"Restore {Amount:0.##} {ResourceName}";
        }

        protected abstract string ResourceName { get; }
        protected abstract void Restore(IStatisticsController statisticsController, float amount);
    }
}
