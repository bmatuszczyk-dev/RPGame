using System;
using System.Collections.Generic;
using RPGame.Core.Statuses;

namespace RPGame.Core.Buildup
{
    public sealed class BuildupContainer
    {
        private readonly Dictionary<BuildupDefinition, BuildupInstance> instances = new();
        private readonly List<BuildupDefinition> emptyDefinitions = new();
        private readonly List<BuildupSnapshot> activeBuildups = new();
        private readonly IStatusReceiver statusReceiver;

        public event Action<BuildupDefinition, float, float> BuildupChanged;
        public event Action BuildupsCleared;

        public IReadOnlyList<BuildupSnapshot> ActiveBuildups
        {
            get
            {
                activeBuildups.Clear();
                foreach (KeyValuePair<BuildupDefinition, BuildupInstance> entry in instances)
                {
                    activeBuildups.Add(new BuildupSnapshot(
                        entry.Key,
                        entry.Value.CurrentAmount,
                        entry.Key.Threshold));
                }

                return activeBuildups;
            }
        }

        public BuildupContainer(IStatusReceiver statusReceiver)
        {
            this.statusReceiver = statusReceiver;
        }

        public void Add(BuildupDefinition definition, float amount, StatusContext context)
        {
            if (definition == null || amount <= 0f || statusReceiver == null)
            {
                return;
            }

            if (statusReceiver.HasStatus(definition.TriggeredStatus))
            {
                if (instances.Remove(definition))
                {
                    BuildupChanged?.Invoke(definition, 0f, definition.Threshold);
                }

                return;
            }

            if (!instances.TryGetValue(definition, out BuildupInstance instance))
            {
                instance = new BuildupInstance(definition);
                instances.Add(definition, instance);
            }

            instance.Add(amount);
            if (instance.CurrentAmount < definition.Threshold)
            {
                BuildupChanged?.Invoke(definition, instance.CurrentAmount, definition.Threshold);
                return;
            }

            instances.Remove(definition);
            BuildupChanged?.Invoke(definition, 0f, definition.Threshold);
            statusReceiver.ApplyStatus(
                definition.TriggeredStatus,
                definition.TriggeredStatusDuration,
                context);
        }

        public void Tick(float deltaTime)
        {
            emptyDefinitions.Clear();
            foreach (KeyValuePair<BuildupDefinition, BuildupInstance> entry in instances)
            {
                float currentAmount = entry.Value.CurrentAmount;
                entry.Value.Tick(deltaTime);
                if (entry.Value.CurrentAmount != currentAmount)
                {
                    BuildupChanged?.Invoke(entry.Key, entry.Value.CurrentAmount, entry.Key.Threshold);
                }

                if (entry.Value.CurrentAmount <= 0f)
                {
                    emptyDefinitions.Add(entry.Key);
                }
            }

            for (int i = 0; i < emptyDefinitions.Count; i++)
            {
                instances.Remove(emptyDefinitions[i]);
            }
        }

        public float GetCurrentAmount(BuildupDefinition definition)
        {
            return definition != null && instances.TryGetValue(definition, out BuildupInstance instance)
                ? instance.CurrentAmount
                : 0f;
        }

        public void Clear()
        {
            if (instances.Count == 0)
            {
                return;
            }

            instances.Clear();
            emptyDefinitions.Clear();
            BuildupsCleared?.Invoke();
        }
    }
}
