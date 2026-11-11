using RPGame.Core.Statistics;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "RestoreHealthEffect", menuName = "RPGame/Statuses/Restore Health")]
    [MovedFrom(true, null, null, "RestoreHealthEffectDefinition")]
    public sealed class RestoreHealthStatusDefinition : RestoreResourceStatusDefinition
    {
        protected override string ResourceName => "Health";

        public override bool IsFinished(StatusTarget target)
        {
            IStatisticsController statisticsController = target.StatisticsController;
            return statisticsController == null
                || statisticsController.CurrentHealth >= statisticsController.MaxHealth;
        }

        protected override void Restore(IStatisticsController statisticsController, float amount)
        {
            statisticsController.Heal(amount);
        }
    }
}
