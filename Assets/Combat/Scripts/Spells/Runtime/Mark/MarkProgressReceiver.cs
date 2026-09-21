using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [RequireComponent(typeof(StatusAggregator))]
    public sealed class MarkProgressReceiver : MonoBehaviour
    {
        [SerializeField] private MarkStatusDefinition markDefinition;

        private StatusAggregator statusAggregator;
        private MarkProgressTracker tracker;

        private void Awake()
        {
            ResolveTracker();
        }

        public bool RecordSuccessfulSpell(SpellId spellId, StatusContext context)
        {
            ResolveTracker();
            return tracker != null && tracker.RecordSuccessfulSpell(spellId, context);
        }

        private void ResolveTracker()
        {
            if (tracker != null)
            {
                return;
            }

            if (statusAggregator == null)
            {
                statusAggregator = GetComponent<StatusAggregator>();
            }

            if (statusAggregator != null && markDefinition != null)
            {
                tracker = new MarkProgressTracker(statusAggregator, markDefinition);
            }
        }
    }
}
