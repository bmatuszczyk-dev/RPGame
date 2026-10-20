using System.Collections.Generic;
using UnityEngine;

namespace RPGame.Core.Movement
{
    public sealed class KnockbackModifiers
    {
        private readonly Dictionary<int, float> modifiers = new();
        private int nextModifierId;

        public float Multiplier
        {
            get
            {
                float multiplier = 1f;
                foreach (float modifier in modifiers.Values)
                {
                    multiplier *= modifier;
                }

                return multiplier;
            }
        }

        public int AddResistance(float resistance)
        {
            int modifierId = GetNextModifierId();
            modifiers.Add(modifierId, 1f - Mathf.Clamp01(resistance));
            return modifierId;
        }

        public bool Remove(int modifierId)
        {
            return modifiers.Remove(modifierId);
        }

        public void Clear()
        {
            modifiers.Clear();
        }

        private int GetNextModifierId()
        {
            for (int i = 0; i < int.MaxValue; i++)
            {
                if (nextModifierId == int.MaxValue)
                {
                    nextModifierId = 0;
                }

                nextModifierId++;
                if (!modifiers.ContainsKey(nextModifierId))
                {
                    return nextModifierId;
                }
            }

            return 0;
        }
    }
}
