using UnityEngine;

namespace RPGame.Core.Spells
{
    public sealed class PlacementActivationHandle : ISpellActivationHandle
    {
        private readonly IIndicatorSpell spell;
        private ISpellActivationService service;

        public PlacementActivationHandle(IIndicatorSpell spell)
        {
            this.spell = spell;
        }

        public void Activate(ISpellActivationService service, CasterData casterData)
        {
            if (service == null || spell == null)
            {
                return;
            }

            this.service = service;
            Transform caster = casterData.CasterObject != null ? casterData.CasterObject.transform : null;
            service.BeginPlacement(spell, caster);
        }

        public void Deactivate(ISpellActivationService service)
        {
            service?.EndPlacement();
            if (this.service == service)
            {
                this.service = null;
            }
        }

        public bool TryCreateCasterData(CasterData casterData, out CasterData activatedCasterData)
        {
            activatedCasterData = casterData;
            if (service == null || !service.HasValidPlacement || !service.TryGetPlacement(out Vector3 position))
            {
                return false;
            }

            activatedCasterData = new CasterData(
                casterData.CasterObject,
                casterData.CastOrigin,
                casterData.Target,
                casterData.Attributes,
                casterData.Statistics,
                casterData.DamageRanges,
                position,
                casterData.RuntimeBehaviors,
                casterData.PropertyModifiers,
                casterData.SpellId);
            return true;
        }
    }
}
