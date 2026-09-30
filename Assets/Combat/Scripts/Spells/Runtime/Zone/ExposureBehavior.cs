using System;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [Serializable]
    public sealed class ExposureBehavior : ISpellBehavior, IInitializableRuntimeSpellBehavior
    {
        [SerializeField] private WeaknessStatusDefinition weaknessStatus;
        [SerializeField, Min(0f)] private float weaknessDuration = 3f;

        public ExposureBehavior()
        {
        }

        public ExposureBehavior(WeaknessStatusDefinition weaknessStatus, float weaknessDuration)
        {
            this.weaknessStatus = weaknessStatus;
            this.weaknessDuration = Mathf.Max(0f, weaknessDuration);
        }

        public SpellBehaviorPhase Phase => SpellBehaviorPhase.PostResolve;
        public float WeaknessDuration => weaknessDuration;

        public void AddWeaknessDuration(float durationBonus)
        {
            weaknessDuration = Mathf.Max(0f, weaknessDuration + Mathf.Max(0f, durationBonus));
        }

        public bool Supports(Spell spell)
        {
            return spell is EarthZoneSpell;
        }

        public void Initialize(GameObject caster)
        {
            weaknessDuration = Mathf.Max(0f, weaknessDuration);
        }

        public void Execute(SpellBehaviorContext context)
        {
            if (weaknessStatus == null
                || weaknessDuration <= 0f
                || context.StatusReceiver == null)
            {
                return;
            }

            context.StatusReceiver.ApplyStatus(
                weaknessStatus,
                weaknessDuration,
                new StatusContext(
                    new StatusSourceId(nameof(ExposureBehavior)),
                    context.Source));
        }
    }
}
