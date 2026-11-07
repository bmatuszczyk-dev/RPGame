using System.Collections.Generic;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Core.Effects
{
    public sealed class EffectAggregator : MonoBehaviour, IRuntimeSpellBehaviorProvider, ISpellPropertyModifierProvider
    {
        private readonly PermanentEffectContainer permanentContainer = new();
        private IStatisticsController statisticsController;

        public IReadOnlyList<EffectInstance> Effects => permanentContainer.Effects;
        public IStatisticsController StatisticsController => GetStatisticsController();

        private void Awake()
        {
            statisticsController = GetComponent<IStatisticsController>();
        }

        public void Add(PassiveEffectDefinition definition)
        {
            permanentContainer.Add(definition);
        }

        public void AddRange(IEnumerable<PassiveEffectDefinition> definitions)
        {
            permanentContainer.AddRange(definitions);
        }

        public float GetEffectValue(EffectStat stat, EffectModifierType modifierType)
        {
            return permanentContainer.GetEffectValue(stat, modifierType);
        }

        public float ResolveStatusDuration(StatusDefinition status, float duration)
        {
            return permanentContainer.ResolveStatusDuration(status, duration);
        }

        public IReadOnlyList<IRuntimeSpellBehavior> CreateRuntimeBehaviors(
            Spell spell,
            GameObject casterObject)
        {
            return permanentContainer.CreateRuntimeBehaviors(spell, casterObject);
        }

        public SpellPropertyModifiers CreateSpellPropertyModifiers(Spell spell)
        {
            return permanentContainer.CreateSpellPropertyModifiers(spell);
        }

        private IStatisticsController GetStatisticsController()
        {
            if (statisticsController == null)
            {
                statisticsController = GetComponent<IStatisticsController>();
            }

            return statisticsController;
        }
    }
}
