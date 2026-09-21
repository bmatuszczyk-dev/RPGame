using System.Collections.Generic;
using RPGame.Core.Damage;
using RPGame.Combat.Projectiles;
using RPGame.Core.Spells;
using RPGame.Core.Statistics.Attributes;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [CreateAssetMenu(fileName = "ProjectileSpell", menuName = "RPGame/Spells/Projectile Spell")]
    public sealed class ProjectileSpell : Spell, ICasterDamageRangeProvider, IProjectileCapability
    {
        [SerializeField] private PartialDamageRange baseDamageRange = new(10f, 10f, DamageType.Magical, DamageElement.None);
        [SerializeField] private float powerDamageScaling;

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

            GameObject projectileObject = Instantiate(SpellPrefab, position, rotation);
            if (!projectileObject.TryGetComponent(out ProjectileController projectile))
            {
                Debug.LogWarning($"{name} spawned a prefab without {nameof(ProjectileController)}.", projectileObject);
                Destroy(projectileObject);
                return;
            }

            projectile.Initialize(CreateProjectileCasterData(casterData));
        }

        private CasterData CreateProjectileCasterData(CasterData casterData)
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

        public IReadOnlyList<PartialDamageRange> GetDamageRanges(CasterData casterData)
        {
            float powerDamageBonus = CalculatePowerDamageBonus(casterData.Attributes);
            return new[]
            {
                new PartialDamageRange(
                    baseDamageRange.MinDamage + powerDamageBonus,
                    baseDamageRange.MaxDamage + powerDamageBonus,
                    baseDamageRange.DamageType,
                    baseDamageRange.DamageElement)
            };
        }

        private float CalculatePowerDamageBonus(ICharacterAttributes attributes)
        {
            int power = attributes != null ? attributes.Power : 0;
            return SpellDamageCalculator.CalculatePowerDamageBonus(power, powerDamageScaling);
        }
    }
}
