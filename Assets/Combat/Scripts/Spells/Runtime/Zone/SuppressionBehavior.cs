using System;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class SuppressionBehavior :
        ISpellBehavior,
        IInitializableRuntimeSpellBehavior,
        IMarkConsumer
    {
        [SerializeField] private MarkStatusDefinition markDefinition;
        [SerializeField] private SuppressionStatusDefinition suppressionStatus;
        [SerializeField, Min(0f)] private float suppressionDuration = 3f;

        public SuppressionBehavior()
        {
        }

        public SuppressionBehavior(
            MarkStatusDefinition markDefinition,
            SuppressionStatusDefinition suppressionStatus,
            float suppressionDuration)
        {
            this.markDefinition = markDefinition;
            this.suppressionStatus = suppressionStatus;
            this.suppressionDuration = Mathf.Max(0f, suppressionDuration);
        }

        public SpellBehaviorPhase Phase => SpellBehaviorPhase.PostResolve;
        public float SuppressionDuration => suppressionDuration;

        public bool Supports(Spell spell)
        {
            return spell is EarthZoneSpell
                && markDefinition != null
                && suppressionStatus != null;
        }

        public void Initialize(GameObject caster)
        {
            suppressionDuration = Mathf.Max(0f, suppressionDuration);
        }

        public bool TryConsumeMark(SpellBehaviorContext context)
        {
            if (context.HasExecutionFlag(SpellExecutionFlag.MarkConsumed))
            {
                return true;
            }

            if (markDefinition == null
                || context.StatusReceiver == null
                || !context.StatusReceiver.TryConsumeStatus(markDefinition))
            {
                return false;
            }

            context.SetExecutionFlag(SpellExecutionFlag.MarkConsumed);
            return true;
        }

        public void Execute(SpellBehaviorContext context)
        {
            if (!context.HasExecutionFlag(SpellExecutionFlag.MarkConsumed)
                || suppressionStatus == null
                || suppressionDuration <= 0f
                || context.StatusReceiver == null)
            {
                return;
            }

            context.StatusReceiver.ApplyStatus(
                suppressionStatus,
                suppressionDuration,
                new StatusContext(
                    new StatusSourceId(nameof(SuppressionBehavior)),
                    context.Source));
        }
    }
}
