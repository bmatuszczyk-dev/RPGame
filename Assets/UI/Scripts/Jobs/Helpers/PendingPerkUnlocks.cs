using System.Collections.Generic;
using System.Linq;
using RPGame.Progression;
using UnityEngine;

namespace RPGame.UI.Jobs
{
    public sealed class PendingPerkUnlocks
    {
        private readonly List<PerkDefinition> pendingPerks = new();
        private readonly HashSet<string> pendingPerkIds = new();

        public IReadOnlyList<PerkDefinition> PendingPerks => pendingPerks;
        public bool HasPendingPerks => pendingPerks.Count > 0;

        public bool IsPending(PerkDefinition perk)
        {
            return perk != null && pendingPerkIds.Contains(perk.PerkId);
        }

        public void Toggle(JobInstance job, PerkDefinition perk, CharacterProgression progression)
        {
            if (perk == null || string.IsNullOrWhiteSpace(perk.PerkId))
            {
                return;
            }

            if (pendingPerkIds.Contains(perk.PerkId))
            {
                Remove(perk);
                RemoveInvalidPendingPerks(job, progression);
                return;
            }

            if (GetPreviewUnlockState(job, perk, progression) != PerkUnlockState.Available)
            {
                return;
            }

            if (GetPendingCost() + perk.Cost > (job != null ? job.JobPoints : 0))
            {
                return;
            }

            pendingPerks.Add(perk);
            pendingPerkIds.Add(perk.PerkId);
        }

        public void Clear()
        {
            pendingPerks.Clear();
            pendingPerkIds.Clear();
        }

        public int GetPendingCost()
        {
            int cost = 0;
            for (int i = 0; i < pendingPerks.Count; i++)
            {
                if (pendingPerks[i] != null)
                {
                    cost += pendingPerks[i].Cost;
                }
            }

            return cost;
        }

        public PerkUnlockState GetPreviewUnlockState(
            JobInstance job,
            PerkDefinition perk,
            CharacterProgression progression)
        {
            if (progression == null || job == null || perk == null || !IsPerkDefinedForJob(job, perk))
            {
                return PerkUnlockState.Locked;
            }

            PerkUnlockState currentState = progression.GetPerkUnlockState(job, perk);
            if (currentState == PerkUnlockState.Unlocked)
            {
                return PerkUnlockState.Unlocked;
            }

            if (IsPending(perk))
            {
                return PerkUnlockState.Available;
            }

            int remainingJobPoints = Mathf.Max(0, job.JobPoints - GetPendingCost());
            if (remainingJobPoints < perk.Cost)
            {
                return PerkUnlockState.Locked;
            }

            return !HasPredecessor(job, perk) || HasUnlockedOrPendingPredecessor(job, perk)
                ? PerkUnlockState.Available
                : PerkUnlockState.Locked;
        }

        private void Remove(PerkDefinition perk)
        {
            pendingPerkIds.Remove(perk.PerkId);
            pendingPerks.Remove(perk);
        }

        private void RemoveInvalidPendingPerks(JobInstance job, CharacterProgression progression)
        {
            bool removed;
            do
            {
                removed = false;
                for (int i = pendingPerks.Count - 1; i >= 0; i--)
                {
                    PerkDefinition pendingPerk = pendingPerks[i];
                    if (pendingPerk == null || !CanRemainPending(job, pendingPerk, progression))
                    {
                        if (pendingPerk != null)
                        {
                            pendingPerkIds.Remove(pendingPerk.PerkId);
                        }

                        pendingPerks.RemoveAt(i);
                        removed = true;
                    }
                }
            }
            while (removed);
        }

        private bool CanRemainPending(JobInstance job, PerkDefinition perk, CharacterProgression progression)
        {
            if (progression == null || job == null || perk == null || !IsPerkDefinedForJob(job, perk))
            {
                return false;
            }

            if (progression.GetPerkUnlockState(job, perk) == PerkUnlockState.Unlocked)
            {
                return false;
            }

            return !HasPredecessor(job, perk) || HasUnlockedOrPendingPredecessor(job, perk);
        }

        private static bool IsPerkDefinedForJob(JobInstance job, PerkDefinition perk)
        {
            if (job?.Definition == null)
            {
                return false;
            }

            IReadOnlyList<PerkDefinition> jobPerks = job.Definition.JobPerks;
            for (int i = 0; i < jobPerks.Count; i++)
            {
                if (jobPerks[i] != null && jobPerks[i].PerkId == perk.PerkId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPredecessor(JobInstance job, PerkDefinition perk)
        {
            IReadOnlyList<PerkDefinition> jobPerks = job.Definition.JobPerks;
            for (int i = 0; i < jobPerks.Count; i++)
            {
                PerkDefinition candidate = jobPerks[i];
                if (candidate != null && candidate.NextPerkIds.Contains(perk.PerkId))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasUnlockedOrPendingPredecessor(JobInstance job, PerkDefinition perk)
        {
            IReadOnlyList<PerkDefinition> jobPerks = job.Definition.JobPerks;
            for (int i = 0; i < jobPerks.Count; i++)
            {
                PerkDefinition predecessor = jobPerks[i];
                if (predecessor == null || !predecessor.NextPerkIds.Contains(perk.PerkId))
                {
                    continue;
                }

                if (IsPerkUnlocked(job, predecessor.PerkId) || pendingPerkIds.Contains(predecessor.PerkId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPerkUnlocked(JobInstance job, string perkId)
        {
            foreach (string unlockedPerkId in job.UnlockedPerkIds)
            {
                if (unlockedPerkId == perkId)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
