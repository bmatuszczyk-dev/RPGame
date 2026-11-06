using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    public abstract class ZoneSpell : Spell, IIndicatorSpell, IZoneCapability
    {
        [SerializeField] private GameObject indicatorPrefab;
        [SerializeField] private float placementRange;
        [SerializeField] private float radius;
        [SerializeField] private float activationDelay;
        [SerializeField] private float activeDuration;

        public GameObject IndicatorPrefab => indicatorPrefab;
        public float PlacementRange => placementRange;
        public float Radius => radius;
        protected float ActiveDuration => activeDuration;

        public override bool SupportsProperty(SpellProperty property)
        {
            return property is SpellProperty.Radius
                or SpellProperty.Area
                or SpellProperty.ZoneRadius;
        }

        public override ISpellActivationHandle OnActivation(CasterData casterData)
        {
            float effectiveRadius = ResolveEffectiveRadius(casterData.PropertyModifiers);
            return new PlacementActivationHandle(this, effectiveRadius);
        }

        public override void OnCast(CasterData casterData)
        {
            if (!casterData.TargetPosition.HasValue)
            {
                Debug.LogWarning($"{name} cannot cast because target position is missing.", this);
                return;
            }

            if (SpellPrefab == null)
            {
                Debug.LogWarning($"{name} cannot cast because spell prefab is missing.", this);
                return;
            }

            GameObject zoneObject = Instantiate(SpellPrefab, casterData.TargetPosition.Value, Quaternion.identity);
            if (!zoneObject.TryGetComponent(out ZoneController zoneController))
            {
                Debug.LogWarning($"{name} spawned a prefab without {nameof(ZoneController)}.", zoneObject);
                Destroy(zoneObject);
                return;
            }

            float effectiveRadius = ResolveEffectiveRadius(casterData.PropertyModifiers);
            float effectiveDuration = activeDuration;
            if (this is IDurationCapability durationCapability)
            {
                effectiveDuration = casterData.PropertyModifiers.Resolve(
                    durationCapability.Duration,
                    SpellProperty.Duration);
            }

            zoneController.Initialize(casterData, effectiveRadius, activationDelay, effectiveDuration);
        }

        private void OnValidate()
        {
            placementRange = Mathf.Max(0f, placementRange);
            radius = Mathf.Max(0f, radius);
            activationDelay = Mathf.Max(0f, activationDelay);
            activeDuration = Mathf.Max(0f, activeDuration);
        }

        private float ResolveEffectiveRadius(SpellPropertyModifiers modifiers)
        {
            return modifiers.Resolve(
                       Radius,
                       SpellProperty.Radius,
                       SpellProperty.ZoneRadius)
                   * Mathf.Sqrt(modifiers.GetPercentMultiplier(SpellProperty.Area));
        }

    }
}
