using System;
using NUnit.Framework;
using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class ContinuousHealTests
    {
        private GameObject targetObject;

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void TryHeal_SpendsManaAndHealsByRates()
        {
            FakeStatistics healer = new(currentMana: 10f);
            FakeStatistics targetStatistics = new(currentHealth: 40f, maxHealth: 100f);
            ContinuousHeal heal = new(10f, 5f);

            bool didHeal = heal.TryHeal(healer, CreateTarget(targetStatistics), 0.5f);

            Assert.IsTrue(didHeal);
            Assert.That(healer.CurrentMana, Is.EqualTo(7.5f));
            Assert.That(targetStatistics.CurrentHealth, Is.EqualTo(45f));
        }

        [Test]
        public void TryHeal_WithInsufficientMana_DoesNotPartiallyHeal()
        {
            FakeStatistics healer = new(currentMana: 2f);
            FakeStatistics targetStatistics = new(currentHealth: 40f, maxHealth: 100f);
            ContinuousHeal heal = new(10f, 5f);

            bool didHeal = heal.TryHeal(healer, CreateTarget(targetStatistics), 0.5f);

            Assert.IsFalse(didHeal);
            Assert.That(healer.CurrentMana, Is.EqualTo(2f));
            Assert.That(targetStatistics.CurrentHealth, Is.EqualTo(40f));
        }

        private HealerTarget CreateTarget(FakeStatistics statistics)
        {
            targetObject = new GameObject("Target");
            return new HealerTarget(targetObject.AddComponent<EnemyTargetable>(), statistics);
        }

        internal sealed class FakeStatistics : IStatisticsController
        {
            public FakeStatistics(float currentHealth = 100f, float maxHealth = 100f, float currentMana = 100f)
            {
                CurrentHealth = currentHealth;
                MaxHealth = maxHealth;
                CurrentMana = currentMana;
            }

#pragma warning disable CS0067
            public event Action<float, float> HealthChanged;
            public event Action<float, float> StaminaChanged;
            public event Action<float, float> OnManaChanged;
            public event Action Died;
#pragma warning restore CS0067
            public float CurrentHealth { get; private set; }
            public float CurrentStamina => 0f;
            public float CurrentMana { get; private set; }
            public float MaxHealth { get; }
            public float MaxStamina => 0f;
            public float MaxMana => 100f;
            public float HealthRegenerationPerSecond => 0f;
            public float StaminaRegenerationPerSecond => 0f;
            public float StaminaRegenerationDelay => 0f;
            public float ManaRegenerationPerSecond => 0f;
            public float ManaRegenerationDelay => 0f;
            public float HealthNormalized => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
            public float StaminaNormalized => 0f;
            public float ManaNormalized => CurrentMana / MaxMana;
            public bool IsAlive => CurrentHealth > 0f;

            public void TakeDamage(float amount) => CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            public void Heal(float amount) => CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            public bool CanSpendStamina(float amount) => false;
            public bool TrySpendStamina(float amount) => false;
            public void RestoreStamina(float amount) { }
            public bool CanSpendMana(float amount) => amount <= 0f || CurrentMana >= amount;
            public bool TrySpendMana(float amount)
            {
                if (!CanSpendMana(amount))
                {
                    return false;
                }

                CurrentMana -= amount;
                return true;
            }

            public void RestoreMana(float amount) => CurrentMana += amount;
        }
    }
}
