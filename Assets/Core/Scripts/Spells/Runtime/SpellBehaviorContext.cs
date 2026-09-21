using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Core.Spells
{
    public readonly struct SpellBehaviorContext
    {
        public SpellBehaviorContext(
            GameObject target,
            GameObject source,
            SpellId spellId,
            IStatusReceiver statusReceiver)
        {
            Target = target;
            Source = source;
            SpellId = spellId;
            StatusReceiver = statusReceiver;
        }

        public GameObject Target { get; }
        public GameObject Source { get; }
        public SpellId SpellId { get; }
        public IStatusReceiver StatusReceiver { get; }
    }
}
