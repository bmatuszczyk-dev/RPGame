using System;
using System.Collections.Generic;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;

namespace RPGame.Combat.Spells
{
    public sealed class MarkProgressTracker
    {
        public const int RequiredUniqueSpellCount = 3;
        public const float MarkDuration = 5f;

        private readonly IStatusReceiver statusReceiver;
        private readonly MarkStatusDefinition markDefinition;
        private readonly HashSet<SpellId> successfulSpellIds = new();
        private readonly Func<float> getTime;
        private float windowStartedAt = -1f;

        public MarkProgressTracker(
            IStatusReceiver statusReceiver,
            MarkStatusDefinition markDefinition,
            Func<float> getTime = null)
        {
            this.statusReceiver = statusReceiver;
            this.markDefinition = markDefinition;
            this.getTime = getTime ?? (() => UnityEngine.Time.time);
        }

        public bool RecordSuccessfulSpell(SpellId spellId, StatusContext context)
        {
            if (!spellId.IsValid
                || statusReceiver == null
                || markDefinition == null)
            {
                return false;
            }

            if (statusReceiver.HasStatus(markDefinition))
            {
                ClearProgress();
                return false;
            }

            float currentTime = getTime();
            if (successfulSpellIds.Count > 0
                && currentTime - windowStartedAt > GetComboWindow(context.Source))
            {
                ClearProgress();
            }

            if (successfulSpellIds.Count == 0)
            {
                windowStartedAt = currentTime;
            }

            if (!successfulSpellIds.Add(spellId))
            {
                return false;
            }

            if (successfulSpellIds.Count < RequiredUniqueSpellCount)
            {
                return true;
            }

            ClearProgress();
            statusReceiver.ApplyStatus(markDefinition, GetMarkDuration(context.Source), context);
            return true;
        }

        public bool RecordSuccessfulSpell(Spell spell, StatusContext context)
        {
            return spell != null && RecordSuccessfulSpell(spell.Id, context);
        }

        public void ClearProgress()
        {
            successfulSpellIds.Clear();
            windowStartedAt = -1f;
        }

        private float GetComboWindow(UnityEngine.GameObject source)
        {
            return EffectSourceValueResolver.Resolve(MarkDuration, source, EffectStat.MarkComboWindow);
        }

        private float GetMarkDuration(UnityEngine.GameObject source)
        {
            return EffectSourceValueResolver.Resolve(MarkDuration, source, EffectStat.MarkDuration);
        }

    }
}
