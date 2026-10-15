using System.Collections.Generic;
using RPGame.Core.Statuses;

namespace RPGame.Core.Buildup
{
    public sealed class BuildupContainer
    {
        private readonly Dictionary<BuildupDefinition, BuildupInstance> instances = new();
        private readonly List<BuildupDefinition> emptyDefinitions = new();
        private readonly IStatusReceiver statusReceiver;

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
                instances.Remove(definition);
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
                return;
            }

            instances.Remove(definition);
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
                entry.Value.Tick(deltaTime);
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
            instances.Clear();
            emptyDefinitions.Clear();
        }
    }
}
