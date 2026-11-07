using System.Collections.Generic;
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
        private readonly Dictionary<GameObject, MarkProgressTracker> trackers = new();

        private void Awake()
        {
            ResolveStatusAggregator();
        }

        public bool RecordSuccessfulSpell(SpellId spellId, StatusContext context)
        {
            ResolveStatusAggregator();
            if (statusAggregator == null || markDefinition == null || context.Source == null)
            {
                return false;
            }

            if (!trackers.TryGetValue(context.Source, out MarkProgressTracker tracker))
            {
                tracker = new MarkProgressTracker(statusAggregator, markDefinition);
                trackers.Add(context.Source, tracker);
            }

            bool recorded = tracker.RecordSuccessfulSpell(spellId, context);
            if (statusAggregator.HasStatus(markDefinition))
            {
                foreach (MarkProgressTracker progressTracker in trackers.Values)
                {
                    progressTracker.ClearProgress();
                }
            }

            return recorded;
        }

        private void ResolveStatusAggregator()
        {
            if (statusAggregator == null)
            {
                statusAggregator = GetComponent<StatusAggregator>();
            }
        }
    }
}
