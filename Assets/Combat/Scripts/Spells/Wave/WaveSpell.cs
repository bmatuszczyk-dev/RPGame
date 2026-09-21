using System.Collections.Generic;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statistics.Attributes;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(fileName = "WaveSpell", menuName = "RPGame/Spells/Wave Spell")]
    public sealed class WaveSpell : Spell, ICasterDamageRangeProvider, IAoECapability, IControlCapability
    {
        [SerializeField] private PartialDamageRange baseDamageRange = new(1f, 3f, DamageType.Magical, DamageElement.None);
        [SerializeField] private float powerDamageScaling;
        [SerializeField] private float range = 5f;
        [SerializeField] private float angle = 90f;
        [SerializeField] private float propagationSpeed = 10f;
        [SerializeField] private float knockbackDistance = 2f;
        [SerializeField] private float knockbackDuration = 0.25f;

        public float Radius => range;
        public float ControlPower => knockbackDistance;

        public override void OnCast(CasterData casterData)
        {
            if (SpellPrefab == null)
            {
                Debug.LogWarning($"{name} cannot cast because spell prefab is missing.", this);
                return;
            }

            Transform castOrigin = casterData.CastOrigin;
            Vector3 position = castOrigin != null ? castOrigin.position : Vector3.zero;
            Quaternion rotation = castOrigin != null ? castOrigin.rotation : Quaternion.identity;
            GameObject waveObject = Instantiate(SpellPrefab, position, rotation);
            if (!waveObject.TryGetComponent(out WaveController waveController))
            {
                Debug.LogWarning($"{name} spawned a prefab without {nameof(WaveController)}.", waveObject);
                Destroy(waveObject);
                return;
            }

            Vector3 forward = castOrigin != null ? castOrigin.forward : Vector3.forward;
            float effectiveRange = SpellPropertyModifierResolver.ResolveRadius(this, casterData.PropertyModifiers);
            float effectiveKnockbackDistance = SpellPropertyModifierResolver.ResolveControlPower(
                this,
                casterData.PropertyModifiers);
            waveController.Initialize(
                position,
                forward,
                effectiveRange,
                angle,
                propagationSpeed,
                effectiveKnockbackDistance,
                knockbackDuration,
                CreateWaveCasterData(casterData));
        }

        public IReadOnlyList<PartialDamageRange> GetDamageRanges(CasterData casterData)
        {
            int power = casterData.Attributes != null ? casterData.Attributes.Power : 0;
            float powerDamageBonus = SpellDamageCalculator.CalculatePowerDamageBonus(power, powerDamageScaling);
            return new[]
            {
                new PartialDamageRange(
                    baseDamageRange.MinDamage + powerDamageBonus,
                    baseDamageRange.MaxDamage + powerDamageBonus,
                    baseDamageRange.DamageType,
                    baseDamageRange.DamageElement)
            };
        }

        private CasterData CreateWaveCasterData(CasterData casterData)
        {
            return new CasterDataBuilder(casterData.CasterObject, casterData.CastOrigin, casterData.Target)
                .WithAttributes(casterData.Attributes)
                .WithStatistics(casterData.Statistics)
                .WithDamageRanges(GetDamageRanges(casterData))
                .WithRuntimeBehaviors(casterData.RuntimeBehaviors)
                .WithPropertyModifiers(casterData.PropertyModifiers)
                .WithSpellId(casterData.SpellId)
                .Build();
        }

        private void OnValidate()
        {
            range = Mathf.Max(0f, range);
            angle = Mathf.Clamp(angle, 0f, 360f);
            propagationSpeed = Mathf.Max(0f, propagationSpeed);
            knockbackDistance = Mathf.Max(0f, knockbackDistance);
            knockbackDuration = Mathf.Max(0f, knockbackDuration);
        }
    }
}
