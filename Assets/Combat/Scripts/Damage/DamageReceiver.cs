using System;
using System.Collections.Generic;
using System.Text;
using RPGame.Core.Damage;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Damage
{
    public sealed class DamageReceiver : MonoBehaviour, IDamageable
    {
        [SerializeField] private StatisticsController statisticsController;
        [SerializeField] private bool loggingEnabled = true;

        public event Action<DamageResult> DamageReceived;

        public bool CanReceiveDamage => ResolveStatisticsController() != null && statisticsController.IsAlive;
        public IStatisticsController Statistics => ResolveStatisticsController();

        private void Awake()
        {
            ResolveStatisticsController();
        }

        public DamageResult ApplyDamage(DamageData data)
        {
            data = ApplyWeakness(data);

            if (!CanReceiveDamage || !data.HasDamage)
            {
                return DamageResult.Ignored(data, GetCurrentHealth());
            }

            float previousHealth = statisticsController.CurrentHealth;
            statisticsController.TakeDamage(data.Amount);
            float currentHealth = statisticsController.CurrentHealth;
            float appliedAmount = Mathf.Max(0f, previousHealth - currentHealth);

            if (appliedAmount <= 0f)
            {
                return DamageResult.Ignored(data, currentHealth);
            }

            DamageResult result = DamageResult.Applied(
                data,
                appliedAmount,
                previousHealth,
                currentHealth,
                !statisticsController.IsAlive);

            LogDamageReceived(result);
            DamageReceived?.Invoke(result);
            return result;
        }

        private void LogDamageReceived(DamageResult result)
        {
            if (loggingEnabled)
            {
                Debug.Log(
                    $"{FormatDamageSource(result.Data)} dealt {result.AppliedAmount:0.#} damage to {name} ({FormatDamageParts(result.Data)}).",
                    this);
            }
        }

        private static string FormatDamageSource(DamageData data)
        {
            return data.Source != null ? data.Source.name : "Unknown source";
        }

        private static string FormatDamageParts(DamageData data)
        {
            if (data.Parts == null || data.Parts.Count == 0)
            {
                return "no damage parts";
            }

            StringBuilder builder = new();
            for (int i = 0; i < data.Parts.Count; i++)
            {
                PartialDamage part = data.Parts[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(part.Amount.ToString("0.#"));
                builder.Append(' ');
                builder.Append(part.DamageType);

                if (part.DamageElement != DamageElement.None)
                {
                    builder.Append(' ');
                    builder.Append(part.DamageElement);
                }
            }

            return builder.ToString();
        }

        private float GetCurrentHealth()
        {
            StatisticsController resolvedStatisticsController = ResolveStatisticsController();
            return resolvedStatisticsController != null ? resolvedStatisticsController.CurrentHealth : 0f;
        }

        private StatisticsController ResolveStatisticsController()
        {
            if (statisticsController == null)
            {
                statisticsController = GetComponentInParent<StatisticsController>();
            }

            return statisticsController;
        }

        private DamageData ApplyWeakness(DamageData data)
        {
            float weaknessValue = GetWeaknessValue();
            if (!data.HasDamage || weaknessValue <= 0f)
            {
                return data;
            }

            float multiplier = 1f + weaknessValue;
            List<PartialDamage> modifiedParts = new(data.Parts.Count);
            for (int i = 0; i < data.Parts.Count; i++)
            {
                PartialDamage part = data.Parts[i];
                modifiedParts.Add(new PartialDamage(
                    part.Amount * multiplier,
                    part.DamageType,
                    part.DamageElement));
            }

            return new DamageData(modifiedParts, data.Source);
        }

        private float GetWeaknessValue()
        {
            IStatusReader statusReader = GetComponentInParent<IStatusReader>();
            if (statusReader?.Statuses == null)
            {
                return 0f;
            }

            float weaknessValue = 0f;
            IReadOnlyList<StatusInstance> statuses = statusReader.Statuses;
            for (int i = 0; i < statuses.Count; i++)
            {
                StatusInstance status = statuses[i];
                if (status != null
                    && !status.IsFinished
                    && status.Definition is WeaknessStatusDefinition)
                {
                    weaknessValue += status.Value;
                }
            }

            return Mathf.Max(0f, weaknessValue);
        }
    }
}
