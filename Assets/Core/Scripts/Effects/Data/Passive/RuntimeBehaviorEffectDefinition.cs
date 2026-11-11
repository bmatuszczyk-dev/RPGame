using System;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Core.Effects
{
    [CreateAssetMenu(
        fileName = "RuntimeBehaviorEffect",
        menuName = "RPGame/Progression/Effects/Runtime Behavior Effect")]
    public sealed class RuntimeBehaviorEffectDefinition : PassiveEffectDefinition
    {
        [SerializeReference] private IRuntimeSpellBehavior behavior;
        [SerializeField, Min(0)] private int executionOrder;

        public int ExecutionOrder => executionOrder;

        public IRuntimeSpellBehavior CreateRuntimeBehavior(
            Spell spell,
            GameObject casterObject)
        {
            IRuntimeSpellBehavior runtimeBehavior =
                RuntimeSpellBehaviorCloner.Clone(behavior);

            if (runtimeBehavior is not IInitializableRuntimeSpellBehavior initializable
                || !initializable.Supports(spell))
            {
                return null;
            }

            initializable.Initialize(casterObject);
            return runtimeBehavior;
        }

        public override string ToString()
        {
            return behavior != null ? behavior.GetType().Name : name;
        }
    }
}
