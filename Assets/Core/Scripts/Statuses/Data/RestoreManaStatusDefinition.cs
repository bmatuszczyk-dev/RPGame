using RPGame.Core.Statistics;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RPGame.Core.Statuses
{
    [CreateAssetMenu(fileName = "RestoreManaEffect", menuName = "RPGame/Statuses/Restore Mana")]
    [MovedFrom(true, null, null, "RestoreManaEffectDefinition")]
    public sealed class RestoreManaStatusDefinition : RestoreResourceStatusDefinition
    {
        protected override string ResourceName => "Mana";

        public override bool IsFinished(StatusTarget target)
        {
            IStatisticsController statisticsController = target.StatisticsController;
            return statisticsController == null
                || statisticsController.CurrentMana >= statisticsController.MaxMana;
        }

        protected override void Restore(IStatisticsController statisticsController, float amount)
        {
            statisticsController.RestoreMana(amount);
        }
    }
}
