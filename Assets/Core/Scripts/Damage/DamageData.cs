using System;
using System.Collections.Generic;
using RPGame.Core.Effects;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Core.Damage
{
    public enum DamageApplicationType
    {
        Direct,
        Periodic
    }

    public enum DamageOrigin
    {
        Other,
        Spell
    }

    public readonly struct DamageData
    {
        private static readonly IReadOnlyList<PartialDamage> EmptyParts = Array.Empty<PartialDamage>();

        public DamageData(
            IReadOnlyList<PartialDamage> parts,
            GameObject source = null,
            DamageApplicationType applicationType = DamageApplicationType.Direct,
            DamageOrigin origin = DamageOrigin.Other)
            : this(parts, source, applicationType, origin, true)
        {
        }

        private DamageData(
            IReadOnlyList<PartialDamage> parts,
            GameObject source,
            DamageApplicationType applicationType,
            DamageOrigin origin,
            bool applySourceModifiers)
        {
            Source = source;
            ApplicationType = applicationType;
            Origin = origin;
            IReadOnlyList<PartialDamage> copiedParts = CopyDamageParts(parts);
            Parts = applySourceModifiers
                ? ApplySourceDamageModifiers(copiedParts, source, applicationType, origin)
                : copiedParts;
            Amount = CalculateAmount(Parts);
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public DamageApplicationType ApplicationType { get; }
        public DamageOrigin Origin { get; }
        public IReadOnlyList<PartialDamage> Parts { get; }

        public bool HasDamage => Amount > 0f;

        public DamageData WithParts(IReadOnlyList<PartialDamage> parts)
        {
            return new DamageData(parts, Source, ApplicationType, Origin, false);
        }

        private static IReadOnlyList<PartialDamage> CopyDamageParts(IReadOnlyList<PartialDamage> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                return EmptyParts;
            }

            List<PartialDamage> copiedParts = new(parts.Count);
            for (int i = 0; i < parts.Count; i++)
            {
                PartialDamage part = parts[i];
                if (part.HasDamage)
                {
                    copiedParts.Add(part);
                }
            }

            return copiedParts.Count > 0 ? copiedParts : EmptyParts;
        }

        private static float CalculateAmount(IReadOnlyList<PartialDamage> parts)
        {
            float amount = 0f;
            for (int i = 0; i < parts.Count; i++)
            {
                amount += parts[i].Amount;
            }

            return Mathf.Max(0f, amount);
        }

        private static IReadOnlyList<PartialDamage> ApplySourceDamageModifiers(
            IReadOnlyList<PartialDamage> parts,
            GameObject source,
            DamageApplicationType applicationType,
            DamageOrigin origin)
        {
            IStatusReader statusReader = source != null
                ? source.GetComponentInParent<IStatusReader>()
                : null;
            if (parts.Count == 0)
            {
                return parts;
            }

            float flatSpellDamage = applicationType == DamageApplicationType.Direct
                && origin == DamageOrigin.Spell
                ? EffectSourceValueResolver.GetFlatValue(source, EffectStat.SpellDamage)
                : 0f;
            float multiplier = 1f - GetSuppressionValue(statusReader);
            if (origin == DamageOrigin.Spell)
            {
                multiplier *= EffectSourceValueResolver.GetPercentMultiplier(
                    source,
                    EffectStat.SpellDamage);
                if (applicationType == DamageApplicationType.Periodic)
                {
                    multiplier *= EffectSourceValueResolver.GetPercentMultiplier(
                        source,
                        EffectStat.PeriodicSpellDamage);
                }
            }

            if (Mathf.Approximately(flatSpellDamage, 0f)
                && Mathf.Approximately(multiplier, 1f))
            {
                return parts;
            }

            List<PartialDamage> modifiedParts = new(parts.Count);
            float baseAmount = CalculateAmount(parts);
            for (int i = 0; i < parts.Count; i++)
            {
                PartialDamage part = parts[i];
                float flatPart = baseAmount > 0f
                    ? flatSpellDamage * part.Amount / baseAmount
                    : 0f;
                modifiedParts.Add(new PartialDamage(
                    (part.Amount + flatPart) * multiplier,
                    part.DamageType,
                    part.DamageElement));
            }

            return modifiedParts;
        }

        private static float GetSuppressionValue(IStatusReader statusReader)
        {
            if (statusReader?.Statuses == null)
            {
                return 0f;
            }

            float suppressionValue = 0f;
            IReadOnlyList<StatusInstance> statuses = statusReader.Statuses;
            for (int i = 0; i < statuses.Count; i++)
            {
                StatusInstance status = statuses[i];
                if (status != null
                    && !status.IsFinished
                    && status.Definition is SuppressionStatusDefinition)
                {
                    suppressionValue += status.Value;
                }
            }

            return Mathf.Clamp01(suppressionValue);
        }
    }
}
