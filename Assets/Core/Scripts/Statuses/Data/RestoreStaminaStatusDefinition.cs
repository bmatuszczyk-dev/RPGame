using RPGame.Core.Statistics;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "RestoreStaminaEffect", menuName = "RPGame/Statuses/Restore Stamina")]
    [MovedFrom(true, null, null, "RestoreStaminaEffectDefinition")]
    public sealed class RestoreStaminaStatusDefinition : RestoreResourceStatusDefinition
    {
        protected override string ResourceName => "Stamina";

        public override bool IsFinished(StatusTarget target)
        {
            IStatisticsController statisticsController = target.StatisticsController;
            return statisticsController == null
                || statisticsController.CurrentStamina >= statisticsController.MaxStamina;
        }

        protected override void Restore(IStatisticsController statisticsController, float amount)
        {
            statisticsController.RestoreStamina(amount);
        }
    }
}
