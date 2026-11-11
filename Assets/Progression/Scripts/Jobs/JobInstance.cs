using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGame.Progression
{
    [Serializable]
    public sealed class JobInstance
    {
        private const int StartingLevel = 1;
        private const int StartingJobPoints = 1;

        [SerializeField] private JobDefinition definition;
        [SerializeField] private string jobId;
        [SerializeField] private int currentLevel;
        [SerializeField] private int currentXP;
        [SerializeField] private int totalInvestedXP;
        [SerializeField] private int jobPoints;
        [SerializeField] private List<string> unlockedPerkIds = new();

        private readonly Action<JobInstance> onAdvanced;

        public JobInstance(JobDefinition definition, Action<JobInstance> onAdvanced = null)
            : this(definition, StartingLevel, 0, 0, StartingJobPoints, onAdvanced)
        {
        }

        public JobInstance(
            JobDefinition definition,
            int currentLevel,
            int currentXP,
            int totalInvestedXP,
            Action<JobInstance> onAdvanced = null)
            : this(definition, currentLevel, currentXP, totalInvestedXP, 0, onAdvanced)
        {
        }

        public JobInstance(
            JobDefinition definition,
            int currentLevel,
            int currentXP,
            int totalInvestedXP,
            int jobPoints,
            Action<JobInstance> onAdvanced = null)
        {
            this.definition = definition;
            jobId = definition.JobId;
            this.currentLevel = Mathf.Max(StartingLevel, currentLevel);
            this.currentXP = Mathf.Max(0, currentXP);
            this.totalInvestedXP = Mathf.Max(0, totalInvestedXP);
            this.jobPoints = Mathf.Max(0, jobPoints);
            this.onAdvanced = onAdvanced;
        }

        public JobDefinition Definition => definition;
        public string JobId => jobId;
        public int CurrentLevel => currentLevel;
        public int CurrentXP => currentXP;
        public int TotalInvestedXP => totalInvestedXP;
        public int JobPoints => jobPoints;
        public IReadOnlyCollection<string> UnlockedPerkIds => GetUnlockedPerkIds();
        public bool IsMaxLevel => currentLevel >= definition.MaxLevel;

        public int AddExperience(int amount)
        {
            if (amount <= 0 || IsMaxLevel)
            {
                return amount > 0 ? amount : 0;
            }

            int acceptedXP = Mathf.Min(amount, GetRemainingXPToMaxLevel());
            if (acceptedXP <= 0)
            {
                return amount;
            }

            currentXP = AddClamped(currentXP, acceptedXP);
            totalInvestedXP = AddClamped(totalInvestedXP, acceptedXP);
            ApplyLevelUps();

            return amount - acceptedXP;
        }

        public int GetXPToNextLevel()
        {
            return IsMaxLevel ? 0 : definition.GetRequiredExperience(currentLevel);
        }

        public int GetRemainingXPToMaxLevel()
        {
            long remainingXP = 0;
            int simulatedLevel = currentLevel;
            int simulatedCurrentXP = currentXP;

            while (simulatedLevel < definition.MaxLevel)
            {
                int requiredXP = definition.GetRequiredExperience(simulatedLevel);
                remainingXP += Math.Max(0, requiredXP - simulatedCurrentXP);
                simulatedCurrentXP = 0;
                simulatedLevel++;
            }

            return remainingXP >= int.MaxValue ? int.MaxValue : (int)remainingXP;
        }

        private void ApplyLevelUps()
        {
            while (!IsMaxLevel)
            {
                int requiredXP = GetXPToNextLevel();
                if (currentXP < requiredXP)
                {
                    return;
                }

                currentXP = Mathf.Max(0, currentXP - requiredXP);
                currentLevel++;
                jobPoints = AddClamped(jobPoints, 1);
                onAdvanced?.Invoke(this);
            }
        }

        internal bool SpendJobPoints(int amount)
        {
            if (amount <= 0 || jobPoints < amount)
            {
                return false;
            }

            jobPoints -= amount;
            return true;
        }

        internal void UnlockPerk(string perkId)
        {
            List<string> perkIds = GetUnlockedPerkIds();
            if (string.IsNullOrWhiteSpace(perkId) || perkIds.Contains(perkId))
            {
                return;
            }

            perkIds.Add(perkId);
        }

        private List<string> GetUnlockedPerkIds()
        {
            unlockedPerkIds ??= new List<string>();
            return unlockedPerkIds;
        }

        private static int AddClamped(int currentValue, int amount)
        {
            return amount > int.MaxValue - currentValue ? int.MaxValue : currentValue + amount;
        }
    }
}
