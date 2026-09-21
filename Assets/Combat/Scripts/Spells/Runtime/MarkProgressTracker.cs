using System.Collections.Generic;
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

        public MarkProgressTracker(IStatusReceiver statusReceiver, MarkStatusDefinition markDefinition)
        {
            this.statusReceiver = statusReceiver;
            this.markDefinition = markDefinition;
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
                successfulSpellIds.Clear();
                return false;
            }

            if (!successfulSpellIds.Add(spellId))
            {
                return false;
            }

            if (successfulSpellIds.Count < RequiredUniqueSpellCount)
            {
                return true;
            }

            successfulSpellIds.Clear();
            statusReceiver.ApplyStatus(markDefinition, MarkDuration, context);
            return true;
        }

        public bool RecordSuccessfulSpell(Spell spell, StatusContext context)
        {
            return spell != null && RecordSuccessfulSpell(spell.Id, context);
        }
    }
}
