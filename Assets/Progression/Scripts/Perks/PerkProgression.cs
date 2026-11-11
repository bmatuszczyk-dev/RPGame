using System;
using System.Linq;
using RPGame.Core.Effects;

namespace RPGame.Progression
{
    public sealed class PerkProgression
    {
        public event Action<JobInstance, PerkDefinition> PerkUnlocked;

        public PerkUnlockState GetUnlockState(JobInstance job, PerkDefinition perk)
        {
            if (job == null || perk == null || !IsPerkDefinedForJob(job, perk))
            {
                return PerkUnlockState.Locked;
            }

            if (job.UnlockedPerkIds.Contains(perk.PerkId))
            {
                return PerkUnlockState.Unlocked;
            }

            if (job.JobPoints < perk.Cost)
            {
                return PerkUnlockState.Locked;
            }

            if (perk.IsStartingPerk
                || !HasPredecessor(job, perk)
                || HasUnlockedPredecessor(job, perk))
            {
                return PerkUnlockState.Available;
            }

            return PerkUnlockState.Locked;
        }

        public bool TryUnlockPerk(JobInstance job, PerkDefinition perk)
        {
            if (GetUnlockState(job, perk) != PerkUnlockState.Available)
            {
                return false;
            }

            if (!job.SpendJobPoints(perk.Cost))
            {
                return false;
            }

            job.UnlockPerk(perk.PerkId);
            PerkUnlocked?.Invoke(job, perk);
            return true;
        }

        public PermanentEffectContainer CreateEffectContainer(JobInstance job)
        {
            PermanentEffectContainer container = new PermanentEffectContainer();
            if (job?.Definition == null)
            {
                return container;
            }

            foreach (PerkDefinition perk in job.Definition.JobPerks)
            {
                if (perk == null || !job.UnlockedPerkIds.Contains(perk.PerkId))
                {
                    continue;
                }

                container.AddRange(perk.Effects);
            }

            return container;
        }

        private static bool IsPerkDefinedForJob(JobInstance job, PerkDefinition perk)
        {
            return job.Definition != null
                && job.Definition.JobPerks.Any(jobPerk => jobPerk != null && jobPerk.PerkId == perk.PerkId);
        }

        private static bool HasPredecessor(JobInstance job, PerkDefinition perk)
        {
            foreach (PerkDefinition jobPerk in job.Definition.JobPerks)
            {
                if (jobPerk != null && jobPerk.NextPerkIds.Contains(perk.PerkId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUnlockedPredecessor(JobInstance job, PerkDefinition perk)
        {
            foreach (PerkDefinition jobPerk in job.Definition.JobPerks)
            {
                if (jobPerk != null
                    && job.UnlockedPerkIds.Contains(jobPerk.PerkId)
                    && jobPerk.NextPerkIds.Contains(perk.PerkId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
