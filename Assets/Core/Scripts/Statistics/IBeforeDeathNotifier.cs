using System;

namespace RPGame.Core.Statistics
{
    public interface IBeforeDeathNotifier
    {
        event Action BeforeDied;
    }
}
