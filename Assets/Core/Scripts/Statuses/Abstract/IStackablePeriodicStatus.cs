using UnityEngine;

namespace RPGame.Core.Statuses
{
    public interface IStackablePeriodicStatus
    {
        float TickInterval { get; }

        void Tick(StatusTarget target, int currentStacks, GameObject source);
    }
}
