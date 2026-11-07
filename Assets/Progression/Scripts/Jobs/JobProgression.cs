using System;
using System.Collections.Generic;

namespace RPGame.Progression
{
    [Serializable]
    public sealed class JobProgression
    {
        private readonly JobContainer jobs;
        private readonly Func<int> getAvailableXP;
        private readonly Action<int> spendXP;

        public event Action<JobExperienceAllocation> ExperienceAllocated;
        public event Action<JobInstance> JobUnlocked;

        public JobProgression(JobContainer jobs, Func<int> getAvailableXP, Action<int> spendXP)
        {
            this.jobs = jobs;
            this.getAvailableXP = getAvailableXP;
            this.spendXP = spendXP;
        }

        public IReadOnlyDictionary<string, JobInstance> JobsById => jobs.JobsById;

        public void UnlockJob(JobDefinition definition, Action<JobInstance> onAdvanced = null)
        {
            if (definition == null || jobs.HasJob(definition.JobId))
            {
                return;
            }

            jobs.AddJob(definition, onAdvanced);
            JobUnlocked?.Invoke(jobs.GetJob(definition.JobId));
        }

        public bool HasJob(string jobId)
        {
            return jobs.HasJob(jobId);
        }

        public JobInstance GetJob(string jobId)
        {
            return jobs.GetJob(jobId);
        }

        public IEnumerable<JobInstance> GetAllJobs()
        {
            return jobs.GetAllJobs();
        }

        public bool AddXPToJob(string jobId, int amount)
        {
            if (!CanAddXPToJob(jobId, amount))
            {
                return false;
            }

            JobInstance job = jobs.GetJob(jobId);
            int previousLevel = job.CurrentLevel;
            int previousJobPoints = job.JobPoints;
            int unspentXP = jobs.AddExperience(jobId, amount);
            int spentXP = amount - unspentXP;

            if (spentXP <= 0)
            {
                return false;
            }

            spendXP(spentXP);
            ExperienceAllocated?.Invoke(new JobExperienceAllocation(
                job,
                spentXP,
                previousLevel,
                previousJobPoints));
            return true;
        }

        public bool CanAddXPToJob(string jobId, int amount)
        {
            JobInstance job = jobs.GetJob(jobId);
            return amount > 0
                && amount <= getAvailableXP()
                && job != null
                && !job.IsMaxLevel
                && amount <= job.GetRemainingXPToMaxLevel();
        }

        public int GetJobLevel(string jobId)
        {
            JobInstance job = jobs.GetJob(jobId);
            return job != null ? job.CurrentLevel : 0;
        }

        public void Clear()
        {
            jobs.Clear();
        }
    }

    public readonly struct JobExperienceAllocation
    {
        public JobExperienceAllocation(JobInstance job, int experience, int previousLevel, int previousJobPoints)
        {
            Job = job;
            Experience = experience;
            PreviousLevel = previousLevel;
            PreviousJobPoints = previousJobPoints;
        }

        public JobInstance Job { get; }
        public int Experience { get; }
        public int PreviousLevel { get; }
        public int PreviousJobPoints { get; }
    }
}
