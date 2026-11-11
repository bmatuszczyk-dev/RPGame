using UnityEngine;

namespace RPGame.Core.Statuses
{
    public readonly struct StatusContext
    {
        public StatusContext(
            StatusSourceId sourceId,
            GameObject source,
            ReapplyPolicy? reapplyPolicyOverride = null,
            float? valueOverride = null,
            object applicationData = null)
        {
            SourceId = sourceId;
            Source = source;
            ReapplyPolicyOverride = reapplyPolicyOverride;
            ValueOverride = valueOverride;
            ApplicationData = applicationData;
        }

        public StatusSourceId SourceId { get; }
        public GameObject Source { get; }
        public ReapplyPolicy? ReapplyPolicyOverride { get; }
        public float? ValueOverride { get; }
        public object ApplicationData { get; }

        public StatusContext WithReapplyPolicyOverride(ReapplyPolicy reapplyPolicy)
        {
            return new StatusContext(
                SourceId,
                Source,
                reapplyPolicy,
                ValueOverride,
                ApplicationData);
        }

        public StatusContext WithApplicationData(object applicationData)
        {
            return new StatusContext(
                SourceId,
                Source,
                ReapplyPolicyOverride,
                ValueOverride,
                applicationData);
        }
    }
}
