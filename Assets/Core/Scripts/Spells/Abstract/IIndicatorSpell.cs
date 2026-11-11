using UnityEngine;

namespace RPGame.Core.Spells
{
    public interface IIndicatorSpell
    {
        GameObject IndicatorPrefab { get; }
        float PlacementRange { get; }
    }
}
