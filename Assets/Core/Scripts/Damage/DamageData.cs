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

    public readonly struct DamageData
    {
        private static readonly IReadOnlyList<PartialDamage> EmptyParts = Array.Empty<PartialDamage>();

        public DamageData(
            IReadOnlyList<PartialDamage> parts,
            GameObject source = null,
            DamageApplicationType applicationType = DamageApplicationType.Direct)
            : this(parts, source, applicationType, true)
        {
        }

        private DamageData(
            IReadOnlyList<PartialDamage> parts,
            GameObject source,
            DamageApplicationType applicationType,
            bool applySourceModifiers)
        {
            Source = source;
            ApplicationType = applicationType;
            IReadOnlyList<PartialDamage> copiedParts = CopyDamageParts(parts);
            Parts = applySourceModifiers
                ? ApplySourceDamageModifiers(copiedParts, source, applicationType)
                : copiedParts;
            Amount = CalculateAmount(Parts);
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public DamageApplicationType ApplicationType { get; }
        public IReadOnlyList<PartialDamage> Parts { get; }

        public bool HasDamage => Amount > 0f;

        public DamageData WithParts(IReadOnlyList<PartialDamage> parts)
        {
            return new DamageData(parts, Source, ApplicationType, false);
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
            DamageApplicationType applicationType)
        {
            IStatusReader statusReader = source != null
                ? source.GetComponentInParent<IStatusReader>()
                : null;
            if (parts.Count == 0)
            {
                return parts;
            }

            float multiplier = 1f - GetSuppressionValue(statusReader);
            multiplier *= EffectSourceValueResolver.GetMultiplier(source, EffectStat.SpellDamage);
            if (applicationType == DamageApplicationType.Periodic)
            {
                multiplier *= EffectSourceValueResolver.GetMultiplier(
                    source,
                    EffectStat.PeriodicSpellDamage);
            }

            if (Mathf.Approximately(multiplier, 1f))
            {
                return parts;
            }

            List<PartialDamage> modifiedParts = new(parts.Count);
            for (int i = 0; i < parts.Count; i++)
            {
                PartialDamage part = parts[i];
                modifiedParts.Add(new PartialDamage(
                    part.Amount * multiplier,
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
