using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using RPGame.Core.Statistics.Attributes;
using UnityEngine;

namespace RPGame.Progression
{
    public sealed class ProgressionTelemetry : MonoBehaviour
    {
        private const int SchemaVersion = 1;

        [SerializeField] private bool monitoringEnabled;
        [SerializeField] private CharacterProgression progression;

        private DateTime sessionStartedUtc;
        private string sessionId;
        private string sessionFilePath;
        private long sequenceNumber;
        private bool sessionStarted;
        private bool sessionFinished;
        private bool writeFailed;

        private void Start()
        {
            if (!monitoringEnabled)
            {
                return;
            }

            ResolveProgression();
            if (progression == null)
            {
                return;
            }

            sessionStartedUtc = DateTime.UtcNow;
            sessionId = Guid.NewGuid().ToString("N");
            sessionFilePath = Path.Combine(
                Application.persistentDataPath,
                "ProgressionTelemetry",
                $"progression_{sessionStartedUtc:yyyyMMddTHHmmssfffZ}_{sessionId}.jsonl");
            sessionStarted = true;
            WriteSnapshot("InitialSnapshot");
            Subscribe();
        }

        private void OnEnable()
        {
            if (sessionStarted && !sessionFinished)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            FinishSession();
        }

        private void OnApplicationQuit()
        {
            FinishSession();
        }

        private void OnExperienceGained(int amount)
        {
            WriteRecord(new TelemetryRecord
            {
                eventType = "ExperienceGained",
                experienceAmount = amount,
                availableExperience = progression.AvailableExperience
            });
        }

        private void OnJobExperienceAllocated(JobExperienceAllocation allocation)
        {
            WriteRecord(new TelemetryRecord
            {
                eventType = "JobExperienceAllocated",
                jobId = allocation.Job.JobId,
                experienceAmount = allocation.Experience,
                previousLevel = allocation.PreviousLevel,
                currentLevel = allocation.Job.CurrentLevel,
                previousJobPoints = allocation.PreviousJobPoints,
                currentJobPoints = allocation.Job.JobPoints,
                availableExperience = progression.AvailableExperience
            });
        }

        private void OnAttributePointsPurchased(IReadOnlyList<AttributePurchase> purchases)
        {
            TelemetryRecord record = new TelemetryRecord
            {
                eventType = "AttributePointsPurchased",
                availableExperience = progression.AvailableExperience,
                attributePurchases = new List<AttributePurchaseRecord>()
            };

            foreach (AttributePurchase purchase in purchases)
            {
                record.attributePurchases.Add(new AttributePurchaseRecord
                {
                    attribute = purchase.AttributeType.ToString(),
                    points = purchase.Points,
                    previousPurchasedPoints = purchase.PreviousPurchasedPoints,
                    currentPurchasedPoints = purchase.CurrentPurchasedPoints,
                    previousValue = purchase.PreviousValue,
                    currentValue = purchase.CurrentValue,
                    experienceCost = purchase.Cost
                });
            }

            WriteRecord(record);
        }

        private void OnPerkUnlocked(JobInstance job, PerkDefinition perk, int remainingJobPoints)
        {
            WriteRecord(new TelemetryRecord
            {
                eventType = "PerkUnlocked",
                jobId = job.JobId,
                perkId = perk.PerkId,
                currentLevel = job.CurrentLevel,
                jobPointCost = perk.Cost,
                currentJobPoints = remainingJobPoints,
                availableExperience = progression.AvailableExperience
            });
        }

        private void OnJobUnlocked(JobInstance job)
        {
            WriteRecord(new TelemetryRecord
            {
                eventType = "JobUnlocked",
                jobId = job.JobId,
                currentLevel = job.CurrentLevel,
                currentJobPoints = job.JobPoints,
                availableExperience = progression.AvailableExperience
            });
        }

        private void WriteSnapshot(string eventType)
        {
            TelemetryRecord record = new TelemetryRecord
            {
                eventType = eventType,
                availableExperience = progression.AvailableExperience,
                jobs = new List<JobSnapshot>(),
                attributes = new List<AttributeSnapshot>()
            };

            foreach (JobInstance job in progression.Jobs.GetAllJobs())
            {
                record.jobs.Add(new JobSnapshot
                {
                    jobId = job.JobId,
                    level = job.CurrentLevel,
                    jobPoints = job.JobPoints,
                    unlockedPerkIds = new List<string>(job.UnlockedPerkIds)
                });
            }

            CharacterAttributes attributes = GetAttributes();
            if (attributes != null)
            {
                foreach (CharacterAttributeType attributeType in Enum.GetValues(typeof(CharacterAttributeType)))
                {
                    record.attributes.Add(new AttributeSnapshot
                    {
                        attribute = attributeType.ToString(),
                        baseValue = attributes.GetBaseValue(attributeType),
                        purchasedPoints = attributes.GetPurchasedPoints(attributeType)
                    });
                }
            }

            WriteRecord(record);
        }

        private void WriteRecord(TelemetryRecord record)
        {
            if (!sessionStarted || sessionFinished)
            {
                return;
            }

            record.schemaVersion = SchemaVersion;
            record.sessionId = sessionId;
            record.sequenceNumber = ++sequenceNumber;
            record.utcTimestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            record.sessionElapsedSeconds = (DateTime.UtcNow - sessionStartedUtc).TotalSeconds;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(sessionFilePath));
                File.AppendAllText(sessionFilePath, JsonUtility.ToJson(record) + Environment.NewLine);
            }
            catch (Exception exception)
            {
                if (!writeFailed)
                {
                    writeFailed = true;
                    Debug.LogWarning($"Could not write progression telemetry: {exception.Message}", this);
                }
            }
        }

        private void FinishSession()
        {
            if (!sessionStarted || sessionFinished)
            {
                return;
            }

            Unsubscribe();
            WriteSnapshot("FinalSnapshot");
            sessionFinished = true;
        }

        private void Subscribe()
        {
            Unsubscribe();
            progression.ExperienceGained += OnExperienceGained;
            progression.Jobs.ExperienceAllocated += OnJobExperienceAllocated;
            progression.Jobs.JobUnlocked += OnJobUnlocked;
            progression.Attributes.PointsPurchased += OnAttributePointsPurchased;
            progression.PerkUnlocked += OnPerkUnlocked;
        }

        private void Unsubscribe()
        {
            if (progression == null)
            {
                return;
            }

            progression.ExperienceGained -= OnExperienceGained;
            progression.Jobs.ExperienceAllocated -= OnJobExperienceAllocated;
            progression.Jobs.JobUnlocked -= OnJobUnlocked;
            progression.Attributes.PointsPurchased -= OnAttributePointsPurchased;
            progression.PerkUnlocked -= OnPerkUnlocked;
        }

        private CharacterAttributes GetAttributes()
        {
            return progression != null ? progression.GetComponent<CharacterAttributes>() : null;
        }

        private void ResolveProgression()
        {
            if (progression == null)
            {
                TryGetComponent(out progression);
            }
        }

        [Serializable]
        private sealed class TelemetryRecord
        {
            public int schemaVersion;
            public string sessionId;
            public long sequenceNumber;
            public string utcTimestamp;
            public double sessionElapsedSeconds;
            public string eventType;
            public int experienceAmount;
            public int availableExperience;
            public string jobId;
            public string perkId;
            public int previousLevel;
            public int currentLevel;
            public int previousJobPoints;
            public int currentJobPoints;
            public int jobPointCost;
            public List<AttributePurchaseRecord> attributePurchases;
            public List<JobSnapshot> jobs;
            public List<AttributeSnapshot> attributes;
        }

        [Serializable]
        private sealed class AttributePurchaseRecord
        {
            public string attribute;
            public int points;
            public int previousPurchasedPoints;
            public int currentPurchasedPoints;
            public int previousValue;
            public int currentValue;
            public int experienceCost;
        }

        [Serializable]
        private sealed class JobSnapshot
        {
            public string jobId;
            public int level;
            public int jobPoints;
            public List<string> unlockedPerkIds;
        }

        [Serializable]
        private sealed class AttributeSnapshot
        {
            public string attribute;
            public int baseValue;
            public int purchasedPoints;
        }
    }
}
