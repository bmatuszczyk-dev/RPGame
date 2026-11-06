using UnityEngine;

namespace RPGame.Core.Spells
{
    public interface ISpellActivationService
    {
        void BeginPlacement(IIndicatorSpell spell, Transform caster, float placementRadius);
        void EndPlacement();
        bool HasValidPlacement { get; }
        bool TryGetPlacement(out Vector3 position);
    }
}
