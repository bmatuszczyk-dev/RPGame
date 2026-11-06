using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Player.Spells
{
    [RequireComponent(typeof(SpellPlacementController))]
    public sealed class SpellActivationController : MonoBehaviour, ISpellActivationService
    {
        [SerializeField] private SpellPlacementController spellPlacementController;

        private ISpellActivationHandle activeHandle;

        public bool HasValidPlacement =>
            spellPlacementController != null && spellPlacementController.HasValidPlacement;

        private void Awake()
        {
            ResolveReferences();
        }

        public void Activate(ISpellActivationHandle handle, CasterData casterData)
        {
            Deactivate();

            activeHandle = handle;
            activeHandle?.Activate(this, casterData);
        }

        public bool TryCreateCasterData(CasterData casterData, out CasterData activatedCasterData)
        {
            activatedCasterData = casterData;
            return activeHandle != null && activeHandle.TryCreateCasterData(casterData, out activatedCasterData);
        }

        public void Deactivate()
        {
            activeHandle?.Deactivate(this);
            activeHandle = null;
        }

        public void BeginPlacement(IIndicatorSpell spell, Transform caster, float placementRadius)
        {
            ResolveReferences();
            spellPlacementController?.Begin(spell, caster, placementRadius);
        }

        public void EndPlacement()
        {
            spellPlacementController?.Cancel();
        }

        public bool TryGetPlacement(out Vector3 position)
        {
            position = default;
            return spellPlacementController != null && spellPlacementController.TryGetPlacement(out position);
        }

        private void ResolveReferences()
        {
            if (spellPlacementController == null)
            {
                spellPlacementController = GetComponent<SpellPlacementController>();
            }
        }
    }
}
