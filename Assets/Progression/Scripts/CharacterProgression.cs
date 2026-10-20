using System;
using System.Collections.Generic;
using RPGame.Core.Effects;
using RPGame.Core.Progression;
using RPGame.Core.Statistics.Attributes;
using UnityEngine;

namespace RPGame.Progression
{
    public sealed class CharacterProgression : MonoBehaviour, IExperienceReceiver, IExperienceProvider
    {
        [SerializeField] private List<JobDefinition> startingJobs = new();
        [SerializeField] private int availableXP;
        [SerializeField] private EffectAggregator effectAggregator;
        [SerializeField] private CharacterAttributes attributes;

        private readonly JobContainer jobContainer = new();
        private readonly PerkProgression perks = new();
        private JobProgression jobs;

        public JobContainer JobContainer => jobContainer;
        public JobProgression Jobs => GetJobs();
        public AttributeProgression Attributes => GetAttributes();
        public int AvailableExperience => availableXP;

        public event Action AvailableExperienceChanged;
        public event Action<int> ExperienceGained;

        private void Awake()
        {
            for (int i = 0; i < startingJobs.Count; i++)
            {
                if (startingJobs[i] != null)
                {
                    Jobs.UnlockJob(startingJobs[i]);
                }
            }
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0 || amount > int.MaxValue - availableXP)
            {
                return;
            }

            availableXP += amount;
            AvailableExperienceChanged?.Invoke();
            ExperienceGained?.Invoke(amount);
        }

        public int GetAvailableXP()
        {
            return AvailableExperience;
        }

        public PerkUnlockState GetPerkUnlockState(JobInstance job, PerkDefinition perk)
        {
            return perks.GetUnlockState(job, perk);
        }

        public bool TryUnlockPerk(JobInstance job, PerkDefinition perk)
        {
            if (!perks.TryUnlockPerk(job, perk))
            {
                return false;
            }

            AddPerkEffects(perk);
            return true;
        }

        public int GetNextAttributePointCost(CharacterAttributeType attributeType)
        {
            return Attributes.GetNextPointCost(attributeType);
        }

        public int GetAttributePointsCost(IReadOnlyDictionary<CharacterAttributeType, int> pendingPoints)
        {
            return Attributes.GetTotalCost(pendingPoints);
        }

        public bool CanBuyAttributePoint(CharacterAttributeType attributeType)
        {
            return Attributes.CanBuyAttributePoint(attributeType);
        }

        public AttributePurchaseResult TryBuyAttributePoint(CharacterAttributeType attributeType)
        {
            return Attributes.TryBuyAttributePoint(attributeType);
        }

        public AttributePurchaseResult TryBuyAttributePoints(IReadOnlyDictionary<CharacterAttributeType, int> pendingPoints)
        {
            return Attributes.TryBuyAttributePoints(pendingPoints);
        }

        private void OnValidate()
        {
            availableXP = Mathf.Max(0, availableXP);
        }

        private JobProgression GetJobs()
        {
            jobs ??= new JobProgression(jobContainer, GetAvailableXP, SpendExperience);
            return jobs;
        }

        private AttributeProgression GetAttributes()
        {
            ResolveAttributes();
            return new AttributeProgression(
                attributes,
                GetAvailableXP,
                SpendExperience);
        }

        private void SpendExperience(int amount)
        {
            int previousAvailableXP = availableXP;
            availableXP = Mathf.Max(0, availableXP - amount);
            if (previousAvailableXP != availableXP)
            {
                AvailableExperienceChanged?.Invoke();
            }
        }

        private void AddPerkEffects(PerkDefinition perk)
        {
            if (perk == null || perk.Effects.Count == 0)
            {
                return;
            }

            ResolveEffectAggregator();
            if (effectAggregator != null)
            {
                effectAggregator.AddRange(perk.Effects);
            }
        }

        private void ResolveEffectAggregator()
        {
            if (effectAggregator == null)
            {
                TryGetComponent(out effectAggregator);
            }

            if (effectAggregator == null)
            {
                effectAggregator = gameObject.AddComponent<EffectAggregator>();
            }
        }

        private void ResolveAttributes()
        {
            if (attributes == null)
            {
                TryGetComponent(out attributes);
            }
        }
    }
}
