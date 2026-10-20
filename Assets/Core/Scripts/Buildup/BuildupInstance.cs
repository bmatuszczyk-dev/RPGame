using UnityEngine;

namespace RPGame.Core.Buildup
{
    public sealed class BuildupInstance
    {
        public BuildupInstance(BuildupDefinition definition)
        {
            Definition = definition;
        }

        public BuildupDefinition Definition { get; }
        public float CurrentAmount { get; private set; }
        public float DecayDelayRemaining { get; private set; }
        public BuildupState State { get; private set; }
        public float EffectActiveTimeRemaining { get; private set; }

        public void Add(float amount)
        {
            CurrentAmount += amount;
            DecayDelayRemaining = Definition.DecayDelay;
        }

        public void Reset()
        {
            CurrentAmount = 0f;
            DecayDelayRemaining = 0f;
            State = BuildupState.Accumulating;
            EffectActiveTimeRemaining = 0f;
        }

        public void ActivateEffect()
        {
            State = BuildupState.EffectActive;
            CurrentAmount = Definition.Threshold;
            EffectActiveTimeRemaining = Definition.TriggeredStatusDuration;
        }

        public void Tick(float deltaTime)
        {
            if (State == BuildupState.EffectActive)
            {
                EffectActiveTimeRemaining = Mathf.Max(0f, EffectActiveTimeRemaining - deltaTime);
                CurrentAmount = Definition.TriggeredStatusDuration > 0f
                    ? Definition.Threshold * EffectActiveTimeRemaining / Definition.TriggeredStatusDuration
                    : 0f;
                return;
            }

            if (DecayDelayRemaining > 0f)
            {
                DecayDelayRemaining = Mathf.Max(0f, DecayDelayRemaining - deltaTime);
                return;
            }

            CurrentAmount = Mathf.Max(0f, CurrentAmount - Definition.DecayPerSecond * deltaTime);
        }
    }
}
