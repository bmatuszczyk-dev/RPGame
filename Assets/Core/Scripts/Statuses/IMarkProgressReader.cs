using System;

namespace RPGame.Core.Statuses
{
    public interface IMarkProgressReader
    {
        int CurrentUniqueSpellProgress { get; }
        float ComboWindowExpiresAt { get; }
        bool IsMarkActive { get; }

        event Action ProgressChanged;
        event Action ProgressExpired;
    }
}
