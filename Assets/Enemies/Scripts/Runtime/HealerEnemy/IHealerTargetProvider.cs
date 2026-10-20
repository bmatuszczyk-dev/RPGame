using System.Collections.Generic;

namespace RPGame.Enemies
{
    public interface IHealerTargetProvider
    {
        IReadOnlyList<HealerTarget> Targets { get; }
    }
}
