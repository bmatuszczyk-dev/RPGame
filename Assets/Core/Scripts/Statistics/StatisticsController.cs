using System;
using RPGame.Core.Effects;
using RPGame.Core.Pooling;
using RPGame.Core.Statistics.Attributes;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Core.Statistics
{
    public sealed class StatisticsController :
        MonoBehaviour,
        IStatisticsController,
        IBeforeDeathNotifier,
        IPooledEnemyResettable
    {
        private const float AttributeVitalBonus = 5f;

        [SerializeField] private StatisticsConfig config;
        [SerializeField] private CharacterAttributes attributes;
        [SerializeField] private bool initializeOnAwake = true;

        [Header("Regeneration")]
        [SerializeField] private bool regenerateHealth;
        [SerializeField] private bool regenerateStamina = true;
        [SerializeField] private bool regenerateMana = true;

        public event Action<float, float> HealthChanged;
        public event Action<float, float> StaminaChanged;
        public event Action<float, float> OnManaChanged;
        public event Action BeforeDied;
        public event Action Died;

        public float CurrentHealth { get; private set; }
        public float CurrentStamina { get; private set; }
        public float CurrentMana { get; private set; }
        public float MaxHealth => GetBaseMaxHealth() + GetAttributeVitalBonus(CharacterAttributeType.Vitality);
        public float MaxStamina => GetBaseMaxStamina() + GetAttributeVitalBonus(CharacterAttributeType.Endurance);
        public float MaxMana => GetFinalMaxMana();
        public float HealthRegenerationPerSecond => config != null ? config.HealthRegenerationPerSecond : 0f;
        public float StaminaRegenerationPerSecond => config != null ? config.StaminaRegenerationPerSecond : 0f;
        public float StaminaRegenerationDelay => config != null ? config.StaminaRegenerationDelay : 0f;
        public float ManaRegenerationPerSecond => GetFinalManaRegeneration();
        public float ManaRegenerationDelay => config != null ? config.ManaRegenerationDelay : 0f;
        public float HealthNormalized => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        public float StaminaNormalized => MaxStamina > 0f ? CurrentStamina / MaxStamina : 0f;
        public float ManaNormalized => MaxMana > 0f ? CurrentMana / MaxMana : 0f;
        public bool IsAlive => CurrentHealth > 0f;

        private float staminaRegenerationDelayTimer;
        private float manaRegenerationDelayTimer;
        private float lastKnownMaxHealth;
        private float lastKnownMaxStamina;
        private float lastKnownMaxMana;
        private EffectAggregator effectAggregator;
        private StatusAggregator statusAggregator;

        private void Awake()
        {
            ResolveAttributes();
            ResolveModifierSources();
            SubscribeModifierSources();

            if (initializeOnAwake)
            {
                ResetToConfig();
            }
        }

        private void OnEnable()
        {
            ResolveAttributes();
            SubscribeAttributes();
            ResolveModifierSources();
            SubscribeModifierSources();
        }

        private void OnDisable()
        {
            UnsubscribeAttributes();
            UnsubscribeModifierSources();
        }

        private void OnDestroy()
        {
            UnsubscribeAttributes();
            UnsubscribeModifierSources();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Regenerate(deltaTime);
        }

        public void ResetToConfig()
        {
            staminaRegenerationDelayTimer = 0f;
            manaRegenerationDelayTimer = 0f;
            SetHealth(MaxHealth);
            SetStamina(MaxStamina);
            SetMana(MaxMana);
        }

        public void ResetForSpawn()
        {
            ResetToConfig();
        }

        public void ResetForDespawn()
        {
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return;
            }

            SetHealth(CurrentHealth - amount);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return;
            }

            SetHealth(CurrentHealth + amount);
        }

        public bool CanSpendStamina(float amount)
        {
            return amount <= 0f || CurrentStamina >= amount;
        }

        public bool TrySpendStamina(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (!CanSpendStamina(amount))
            {
                return false;
            }

            SetStamina(CurrentStamina - amount);
            staminaRegenerationDelayTimer = StaminaRegenerationDelay;
            return true;
        }

        public void RestoreStamina(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            SetStamina(CurrentStamina + amount);
        }

        public bool CanSpendMana(float amount)
        {
            return amount <= 0f || CurrentMana >= amount;
        }

        public bool TrySpendMana(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (!CanSpendMana(amount))
            {
                return false;
            }

            SetMana(CurrentMana - amount);
            manaRegenerationDelayTimer = ManaRegenerationDelay;
            return true;
        }

        public void RestoreMana(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            SetMana(CurrentMana + amount);
        }

        private void Regenerate(float deltaTime)
        {
            if (config == null || !IsAlive)
            {
                return;
            }

            if (regenerateHealth)
            {
                Heal(config.HealthRegenerationPerSecond * deltaTime);
            }

            if (staminaRegenerationDelayTimer > 0f)
            {
                staminaRegenerationDelayTimer -= deltaTime;
            }
            else if (regenerateStamina)
            {
                RestoreStamina(config.StaminaRegenerationPerSecond * deltaTime);
            }

            if (regenerateMana)
            {
                if (manaRegenerationDelayTimer > 0f)
                {
                    manaRegenerationDelayTimer -= deltaTime;
                }
                else
                {
                    RestoreMana(ManaRegenerationPerSecond * deltaTime);
                }
            }
        }

        private void SetHealth(float value)
        {
            float previousHealth = CurrentHealth;
            float maxHealth = MaxHealth;
            float previousMaxHealth = lastKnownMaxHealth;
            CurrentHealth = Mathf.Clamp(value, 0f, maxHealth);

            if (!Mathf.Approximately(previousHealth, CurrentHealth)
                || !Mathf.Approximately(previousMaxHealth, maxHealth))
            {
                HealthChanged?.Invoke(CurrentHealth, maxHealth);
            }

            lastKnownMaxHealth = maxHealth;

            if (previousHealth > 0f && CurrentHealth <= 0f)
            {
                BeforeDied?.Invoke();
                Died?.Invoke();
            }
        }

        private void SetStamina(float value)
        {
            float previousStamina = CurrentStamina;
            float maxStamina = MaxStamina;
            float previousMaxStamina = lastKnownMaxStamina;
            CurrentStamina = Mathf.Clamp(value, 0f, maxStamina);

            if (!Mathf.Approximately(previousStamina, CurrentStamina)
                || !Mathf.Approximately(previousMaxStamina, maxStamina))
            {
                StaminaChanged?.Invoke(CurrentStamina, maxStamina);
            }

            lastKnownMaxStamina = maxStamina;
        }

        private void SetMana(float value)
        {
            float previousMana = CurrentMana;
            float maxMana = MaxMana;
            float previousMaxMana = lastKnownMaxMana;
            CurrentMana = Mathf.Clamp(value, 0f, maxMana);

            if (!Mathf.Approximately(previousMana, CurrentMana)
                || !Mathf.Approximately(previousMaxMana, maxMana))
            {
                OnManaChanged?.Invoke(CurrentMana, maxMana);
            }

            lastKnownMaxMana = maxMana;
        }

        private void ResolveAttributes()
        {
            if (attributes == null)
            {
                TryGetComponent(out attributes);
            }
        }

        private void SubscribeAttributes()
        {
            if (attributes != null)
            {
                attributes.ValuesChanged -= OnAttributesChanged;
                attributes.ValuesChanged += OnAttributesChanged;
            }
        }

        private void UnsubscribeAttributes()
        {
            if (attributes != null)
            {
                attributes.ValuesChanged -= OnAttributesChanged;
            }
        }

        private void OnAttributesChanged()
        {
            SetHealth(CurrentHealth);
            SetStamina(CurrentStamina);
            SetMana(CurrentMana);
        }

        private void OnEffectsChanged()
        {
            SetMana(CurrentMana);
        }

        private float GetBaseMaxHealth()
        {
            return config != null ? config.MaxHealth : 0f;
        }

        private float GetBaseMaxStamina()
        {
            return config != null ? config.MaxStamina : 0f;
        }

        private float GetBaseMaxMana()
        {
            return config != null ? config.MaxMana : 0f;
        }

        private float GetFinalMaxMana()
        {
            float baseMana = GetBaseMaxMana()
                + GetAttributeVitalBonus(CharacterAttributeType.Intelligence);
            return EffectSourceValueResolver.Resolve(baseMana, gameObject, EffectStat.MaxMana);
        }

        private float GetAttributeVitalBonus(CharacterAttributeType attributeType)
        {
            ResolveAttributes();
            return attributes != null ? attributes.GetValue(attributeType) * AttributeVitalBonus : 0f;
        }

        private float GetFinalManaRegeneration()
        {
            float baseRegeneration = config != null ? config.ManaRegenerationPerSecond : 0f;
            float flatBonus = GetManaRegenerationEffectValue(EffectModifierType.Flat);
            float percentBonus = GetManaRegenerationEffectValue(EffectModifierType.Percent);
            return Mathf.Max(0f, (baseRegeneration + flatBonus) * (1f + percentBonus));
        }

        private float GetManaRegenerationEffectValue(EffectModifierType modifierType)
        {
            ResolveModifierSources();
            float permanentValue = effectAggregator != null
                ? effectAggregator.GetEffectValue(EffectStat.ManaRegeneration, modifierType)
                : 0f;
            float statusValue = statusAggregator != null
                ? statusAggregator.GetEffectValue(EffectStat.ManaRegeneration, modifierType)
                : 0f;
            return permanentValue + statusValue;
        }

        private void ResolveModifierSources()
        {
            if (effectAggregator == null)
            {
                TryGetComponent(out effectAggregator);
            }

            if (statusAggregator == null)
            {
                TryGetComponent(out statusAggregator);
            }
        }

        private void SubscribeModifierSources()
        {
            if (effectAggregator == null)
            {
                return;
            }

            effectAggregator.EffectsChanged -= OnEffectsChanged;
            effectAggregator.EffectsChanged += OnEffectsChanged;
        }

        private void UnsubscribeModifierSources()
        {
            if (effectAggregator != null)
            {
                effectAggregator.EffectsChanged -= OnEffectsChanged;
            }
        }
    }
}
