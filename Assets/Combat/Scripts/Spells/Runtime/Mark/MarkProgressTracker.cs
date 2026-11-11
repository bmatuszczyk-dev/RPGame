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

        public int CurrentUniqueSpellProgress => successfulSpellIds.Count;
        public float ComboWindowExpiresAt { get; private set; } = -1f;
        public event Action ProgressChanged;
        public event Action ProgressExpired;

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
            ExpireIfNecessary(currentTime, context.Source);

            if (successfulSpellIds.Count == 0)
            {
                ComboWindowExpiresAt = currentTime + GetComboWindow(context.Source);
            }

            if (!successfulSpellIds.Add(spellId))
            {
                return false;
            }

            if (successfulSpellIds.Count < RequiredUniqueSpellCount)
            {
                ProgressChanged?.Invoke();
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
            bool hadProgress = successfulSpellIds.Count > 0;
            successfulSpellIds.Clear();
            ComboWindowExpiresAt = -1f;
            if (hadProgress)
            {
                ProgressChanged?.Invoke();
            }
        }

        public bool ExpireIfNecessary(float currentTime, UnityEngine.GameObject source)
        {
            if (successfulSpellIds.Count == 0
                || currentTime < ComboWindowExpiresAt)
            {
                return false;
            }

            ClearProgress();
            ProgressExpired?.Invoke();
            return true;
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
