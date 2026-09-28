using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class ManaLeechBehaviorTests
    {
        private readonly List<Object> createdObjects = new();
        private GameObject caster;
        private GameObject target;
        private TestCharacter casterStatistics;
        private TestCharacter targetCharacter;

        [SetUp]
        public void SetUp()
        {
            caster = CreateGameObject("Caster");
            casterStatistics = caster.AddComponent<TestCharacter>();
            casterStatistics.SetHealth(100f, 100f);
            casterStatistics.SetMana(0f, 100f);

            target = CreateGameObject("Target");
            targetCharacter = target.AddComponent<TestCharacter>();
            targetCharacter.SetHealth(100f, 100f);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void Execute_WithSingleDamageResult_RestoresConfiguredPercent()
        {
            ExecuteManaLeech(
                0.25f,
                CreateDamageResult(appliedAmount: 40f));

            Assert.AreEqual(10f, casterStatistics.CurrentMana);
        }

        [Test]
        public void Execute_WithMultipleDamageResults_SumsAppliedDamage()
        {
            ExecuteManaLeech(
                0.5f,
                CreateDamageResult(appliedAmount: 10f),
                CreateDamageResult(appliedAmount: 30f));

            Assert.AreEqual(20f, casterStatistics.CurrentMana);
        }

        [Test]
        public void Execute_WhenAppliedAmountIsZero_DoesNotRestoreMana()
        {
            ExecuteManaLeech(
                0.5f,
                CreateDamageResult(appliedAmount: 0f));

            Assert.AreEqual(0f, casterStatistics.CurrentMana);
        }

        [Test]
        public void Execute_WhenDamageOverkills_UsesAppliedAmountOnly()
        {
            ExecuteManaLeech(
                0.5f,
                CreateDamageResult(requestedAmount: 100f, appliedAmount: 20f, wasFatal: true));

            Assert.AreEqual(10f, casterStatistics.CurrentMana);
        }

        [Test]
        public void Execute_WhenDamageIsNotFatal_RestoresMana()
        {
            ExecuteManaLeech(
                0.5f,
                CreateDamageResult(appliedAmount: 20f, wasFatal: false));

            Assert.AreEqual(10f, casterStatistics.CurrentMana);
        }

        [Test]
        public void Resolve_WhenStatusExploitationDealsDamage_IncludesStatusExploitationDamage()
        {
            StatusAggregator statusReceiver = target.AddComponent<StatusAggregator>();
            InvokeAwake(statusReceiver);
            TestStatusDefinition slow = ScriptableObject.CreateInstance<TestStatusDefinition>();
            createdObjects.Add(slow);
            statusReceiver.ApplyStatus(
                slow,
                5f,
                new StatusContext(new StatusSourceId("Slow"), caster));

            ExecuteProjectileHit(
                new IRuntimeSpellBehavior[]
                {
                    new StatusExploitationBehavior(4f, caster),
                    new ManaLeechBehavior(0.5f, caster)
                },
                baseDamage: 10f);

            Assert.AreEqual(7f, casterStatistics.CurrentMana);
        }

        [Test]
        public void Resolve_WhenFinisherDealsDamage_IncludesFinisherDamage()
        {
            StatusAggregator statusReceiver = target.AddComponent<StatusAggregator>();
            InvokeAwake(statusReceiver);
            MarkStatusDefinition mark = ScriptableObject.CreateInstance<MarkStatusDefinition>();
            createdObjects.Add(mark);
            statusReceiver.ApplyStatus(
                mark,
                5f,
                new StatusContext(new StatusSourceId("Mark"), caster));
            targetCharacter.SetHealth(30f, 100f);

            ExecuteProjectileHit(
                new IRuntimeSpellBehavior[]
                {
                    new FinisherBehavior(mark, 0.25f, caster),
                    new ManaLeechBehavior(0.5f, caster)
                },
                baseDamage: 10f);

            Assert.AreEqual(15f, casterStatistics.CurrentMana);
        }

        [Test]
        public void Supports_ForProjectileSpell_ReturnsTrue()
        {
            ManaLeechBehavior behavior = new(0.5f, caster);
            ProjectileSpell projectileSpell = ScriptableObject.CreateInstance<ProjectileSpell>();
            createdObjects.Add(projectileSpell);

            Assert.IsTrue(behavior.Supports(projectileSpell));
        }

        [Test]
        public void Supports_ForNonProjectileSpell_ReturnsFalse()
        {
            ManaLeechBehavior behavior = new(0.5f, caster);
            TestSpell spell = ScriptableObject.CreateInstance<TestSpell>();
            createdObjects.Add(spell);

            Assert.IsFalse(behavior.Supports(spell));
        }

        [Test]
        public void Execute_WhenRestoredManaExceedsMax_ClampsToMaxMana()
        {
            casterStatistics.SetMana(95f, 100f);

            ExecuteManaLeech(
                1f,
                CreateDamageResult(appliedAmount: 20f));

            Assert.AreEqual(100f, casterStatistics.CurrentMana);
        }

        private void ExecuteManaLeech(
            float manaLeechPercent,
            params DamageResult[] results)
        {
            ManaLeechBehavior behavior = new(manaLeechPercent, caster);
            SpellBehaviorContext context = new(
                target,
                caster,
                new SpellId("wave"),
                null);

            for (int resultIndex = 0; resultIndex < results.Length; resultIndex++)
            {
                context.AddResolveResult(results[resultIndex]);
            }

            behavior.Execute(context);
        }

        private void ExecuteProjectileHit(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            float baseDamage)
        {
            CasterData casterData = new CasterDataBuilder(caster, caster.transform, target.transform)
                .WithSpellId(new SpellId("wave"))
                .WithRuntimeBehaviors(behaviors)
                .WithDamageRanges(new[]
                {
                    new PartialDamageRange(
                        baseDamage,
                        baseDamage,
                        DamageType.Magical,
                        DamageElement.None)
                })
                .Build();

            SpellBehaviorPipelineExecutor.Resolve(
                casterData,
                target,
                new DamageResolveBehavior(targetCharacter, casterData));
        }

        private static DamageResult CreateDamageResult(
            float appliedAmount,
            bool wasFatal = false)
        {
            return CreateDamageResult(appliedAmount, appliedAmount, wasFatal);
        }

        private static DamageResult CreateDamageResult(
            float requestedAmount,
            float appliedAmount,
            bool wasFatal)
        {
            DamageData data = new(new[]
            {
                new PartialDamage(
                    requestedAmount,
                    DamageType.Magical,
                    DamageElement.None)
            });
            return DamageResult.Applied(
                data,
                appliedAmount,
                100f,
                100f - appliedAmount,
                wasFatal);
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeAwake(StatusAggregator statusReceiver)
        {
            MethodInfo awake = typeof(StatusAggregator).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(statusReceiver, null);
        }

        private sealed class TestStatusDefinition : StatusDefinition
        {
            public override string ToString()
            {
                return "Test Status";
            }
        }

        private sealed class TestCharacter : MonoBehaviour, IDamageable, IStatisticsController
        {
            public event Action<float, float> HealthChanged;
            public event Action<float, float> StaminaChanged;
            public event Action<float, float> OnManaChanged;
            public event Action Died;

            public bool CanReceiveDamage => IsAlive;
            public float CurrentHealth { get; private set; }
            public float CurrentStamina => 0f;
            public float CurrentMana { get; private set; }
            public float MaxHealth { get; private set; }
            public float MaxStamina => 0f;
            public float MaxMana { get; private set; }
            public float HealthRegenerationPerSecond => 0f;
            public float StaminaRegenerationPerSecond => 0f;
            public float StaminaRegenerationDelay => 0f;
            public float ManaRegenerationPerSecond => 0f;
            public float ManaRegenerationDelay => 0f;
            public float HealthNormalized => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
            public float StaminaNormalized => 0f;
            public float ManaNormalized => MaxMana > 0f ? CurrentMana / MaxMana : 0f;
            public bool IsAlive => CurrentHealth > 0f;

            public void SetHealth(float currentHealth, float maxHealth)
            {
                CurrentHealth = currentHealth;
                MaxHealth = maxHealth;
            }

            public void SetMana(float currentMana, float maxMana)
            {
                CurrentMana = currentMana;
                MaxMana = maxMana;
            }

            public DamageResult ApplyDamage(DamageData data)
            {
                if (!CanReceiveDamage || !data.HasDamage)
                {
                    return DamageResult.Ignored(data, CurrentHealth);
                }

                float previousHealth = CurrentHealth;
                TakeDamage(data.Amount);
                float appliedAmount = Mathf.Max(0f, previousHealth - CurrentHealth);
                return appliedAmount > 0f
                    ? DamageResult.Applied(
                        data,
                        appliedAmount,
                        previousHealth,
                        CurrentHealth,
                        !IsAlive)
                    : DamageResult.Ignored(data, CurrentHealth);
            }

            public void TakeDamage(float amount)
            {
                if (amount <= 0f || !IsAlive)
                {
                    return;
                }

                float previousHealth = CurrentHealth;
                CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, MaxHealth);
                HealthChanged?.Invoke(CurrentHealth, MaxHealth);
                if (previousHealth > 0f && CurrentHealth <= 0f)
                {
                    Died?.Invoke();
                }
            }

            public void Heal(float amount)
            {
                CurrentHealth = Mathf.Clamp(CurrentHealth + Mathf.Max(0f, amount), 0f, MaxHealth);
                HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            }

            public bool CanSpendStamina(float amount)
            {
                return true;
            }

            public bool TrySpendStamina(float amount)
            {
                return true;
            }

            public void RestoreStamina(float amount)
            {
                StaminaChanged?.Invoke(CurrentStamina, MaxStamina);
            }

            public bool CanSpendMana(float amount)
            {
                return CurrentMana >= amount;
            }

            public bool TrySpendMana(float amount)
            {
                if (!CanSpendMana(amount))
                {
                    return false;
                }

                CurrentMana -= amount;
                OnManaChanged?.Invoke(CurrentMana, MaxMana);
                return true;
            }

            public void RestoreMana(float amount)
            {
                CurrentMana = Mathf.Clamp(
                    CurrentMana + Mathf.Max(0f, amount),
                    0f,
                    MaxMana);
                OnManaChanged?.Invoke(CurrentMana, MaxMana);
            }
        }

        private sealed class TestSpell : Spell
        {
            public override void OnCast(CasterData casterData)
            {
            }
        }
    }
}
