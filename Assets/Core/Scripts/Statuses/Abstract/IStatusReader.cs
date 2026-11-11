using System.Collections.Generic;

namespace RPGame.Core.Statuses
{
    public interface IStatusReader
    {
        IReadOnlyList<StatusInstance> Statuses { get; }
    }
}
