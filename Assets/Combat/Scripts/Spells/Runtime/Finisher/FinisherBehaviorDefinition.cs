using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [System.Serializable]
    public sealed class FinisherBehaviorDefinition : RuntimeSpellBehaviorDefinition
    {
        [SerializeField] private MarkStatusDefinition markDefinition;
        [SerializeField, Range(0f, 1f)] private float healthThreshold = 0.2f;

        public override bool TryCreate(
            Spell spell,
            GameObject casterObject,
            out IRuntimeSpellBehavior behavior)
        {
            behavior = null;
            if (spell is not IProjectileCapability || markDefinition == null)
            {
                return false;
            }

            behavior = new FinisherBehavior(
                markDefinition,
                healthThreshold,
                casterObject);
            return true;
        }
    }
}
