using UnityEngine;

namespace RPGame.Core.Statuses
{
    public readonly struct StatusContext
    {
        public StatusContext(
            StatusSourceId sourceId,
            GameObject source,
            ReapplyPolicy? reapplyPolicyOverride = null)
        {
            SourceId = sourceId;
            Source = source;
            ReapplyPolicyOverride = reapplyPolicyOverride;
        }

        public StatusSourceId SourceId { get; }
        public GameObject Source { get; }
        public ReapplyPolicy? ReapplyPolicyOverride { get; }
    }
}
