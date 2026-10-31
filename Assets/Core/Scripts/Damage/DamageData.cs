using System;
using System.Collections.Generic;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Core.Damage
{
    public readonly struct DamageData
    {
        private static readonly IReadOnlyList<PartialDamage> EmptyParts = Array.Empty<PartialDamage>();

        public DamageData(IReadOnlyList<PartialDamage> parts, GameObject source = null)
        {
            Source = source;
            Parts = ApplySourceDamageModifiers(CopyDamageParts(parts), source);
            Amount = CalculateAmount(Parts);
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public IReadOnlyList<PartialDamage> Parts { get; }

        public bool HasDamage => Amount > 0f;

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
            GameObject source)
        {
            IStatusReader statusReader = source != null
                ? source.GetComponentInParent<IStatusReader>()
                : null;
            float suppressionValue = GetSuppressionValue(statusReader);
            if (parts.Count == 0 || suppressionValue <= 0f)
            {
                return parts;
            }

            float multiplier = 1f - suppressionValue;
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
