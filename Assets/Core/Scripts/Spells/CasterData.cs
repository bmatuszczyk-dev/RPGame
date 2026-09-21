using System;
using System.Collections.Generic;
using RPGame.Core.Damage;
using RPGame.Core.Statistics;
using RPGame.Core.Statistics.Attributes;
using UnityEngine;

namespace RPGame.Core.Spells
{
    public readonly struct CasterData
    {
        private static readonly IReadOnlyList<PartialDamageRange> EmptyDamageRanges = Array.Empty<PartialDamageRange>();
        private static readonly IReadOnlyList<IRuntimeSpellBehavior> EmptyRuntimeBehaviors = Array.Empty<IRuntimeSpellBehavior>();

        public CasterData(
            GameObject casterObject,
            Transform castOrigin,
            Transform target,
            ICharacterAttributes attributes = null,
            IStatisticsController statistics = null,
            IReadOnlyList<PartialDamageRange> damageRanges = null,
            Vector3? targetPosition = null,
            IReadOnlyList<IRuntimeSpellBehavior> runtimeBehaviors = null,
            SpellPropertyModifiers propertyModifiers = null,
            SpellId spellId = default)
        {
            CasterObject = casterObject;
            CastOrigin = castOrigin;
            Target = target;
            Attributes = attributes;
            Statistics = statistics;
            DamageRanges = CopyDamageRanges(damageRanges);
            TargetPosition = targetPosition;
            RuntimeBehaviors = CopyRuntimeBehaviors(runtimeBehaviors);
            PropertyModifiers = propertyModifiers ?? SpellPropertyModifiers.Empty;
            SpellId = spellId;
        }

        public GameObject CasterObject { get; }
        public Transform CastOrigin { get; }
        public Transform Target { get; }
        public ICharacterAttributes Attributes { get; }
        public IStatisticsController Statistics { get; }
        public IReadOnlyList<PartialDamageRange> DamageRanges { get; }
        public Vector3? TargetPosition { get; }
        public IReadOnlyList<IRuntimeSpellBehavior> RuntimeBehaviors { get; }
        public SpellPropertyModifiers PropertyModifiers { get; }
        public SpellId SpellId { get; }

        private static IReadOnlyList<PartialDamageRange> CopyDamageRanges(IReadOnlyList<PartialDamageRange> damageRanges)
        {
            if (damageRanges == null || damageRanges.Count == 0)
            {
                return EmptyDamageRanges;
            }

            List<PartialDamageRange> copiedDamageRanges = new(damageRanges.Count);
            for (int i = 0; i < damageRanges.Count; i++)
            {
                if (damageRanges[i].MaxDamage > 0f)
                {
                    copiedDamageRanges.Add(damageRanges[i]);
                }
            }

            return copiedDamageRanges.Count > 0 ? copiedDamageRanges : EmptyDamageRanges;
        }

        private static IReadOnlyList<IRuntimeSpellBehavior> CopyRuntimeBehaviors(
            IReadOnlyList<IRuntimeSpellBehavior> runtimeBehaviors)
        {
            if (runtimeBehaviors == null || runtimeBehaviors.Count == 0)
            {
                return EmptyRuntimeBehaviors;
            }

            List<IRuntimeSpellBehavior> copiedRuntimeBehaviors = new(runtimeBehaviors.Count);
            for (int i = 0; i < runtimeBehaviors.Count; i++)
            {
                if (runtimeBehaviors[i] != null)
                {
                    copiedRuntimeBehaviors.Add(runtimeBehaviors[i]);
                }
            }

            return copiedRuntimeBehaviors.Count > 0 ? copiedRuntimeBehaviors : EmptyRuntimeBehaviors;
        }
    }
}
