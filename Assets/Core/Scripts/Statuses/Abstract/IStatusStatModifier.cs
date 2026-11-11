using RPGame.Core.Effects;

namespace RPGame.Core.Statuses
{
    public interface IStatusStatModifier
    {
        EffectStat Stat { get; }
        EffectModifierType ModifierType { get; }
        float GetValue(StatusInstance instance);
    }
}
