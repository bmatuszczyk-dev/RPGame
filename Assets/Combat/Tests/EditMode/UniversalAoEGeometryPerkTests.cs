using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class UniversalAoEGeometryPerkTests
    {
        private readonly List<Object> createdObjects = new();
        private EffectAggregator effects;

        [SetUp]
        public void SetUp()
        {
            GameObject effectOwner = new("AoE Geometry Perk Tests");
            createdObjects.Add(effectOwner);
            effects = effectOwner.AddComponent<EffectAggregator>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        [Test]
        public void AreaExpansion_IncreasesWaveAngleAndAllZoneAreas()
        {
            effects.Add(CreateModifier(SpellProperty.Area, EffectModifierType.Percent, 0.2f));
            WaveSpell wave = CreateWave(5f, 90f);

            Assert.AreEqual(108f, ResolveWaveAngle(
                wave,
                effects.CreateSpellPropertyModifiers(wave)), 0.0001f);

            AssertZoneAreaExpansion(CreateZone<EarthZoneSpell>());
            AssertZoneAreaExpansion(CreateZone<FireZoneSpell>());
            AssertZoneAreaExpansion(CreateZone<LightningZoneSpell>());
        }

        [Test]
        public void WideWave_AddsTwentyDegreesWithoutChangingRange()
        {
            effects.Add(CreateModifier(SpellProperty.WaveAngle, EffectModifierType.Flat, 20f));
            WaveSpell wave = CreateWave(5f, 90f);
            SpellPropertyModifiers modifiers = effects.CreateSpellPropertyModifiers(wave);

            Assert.AreEqual(110f, ResolveWaveAngle(wave, modifiers));
            Assert.AreEqual(5f, ResolveWaveRange(wave, modifiers));
        }

        [Test]
        public void ExtendedWave_IncreasesRangeByFifteenPercentWithoutChangingAngle()
        {
            effects.Add(CreateModifier(SpellProperty.WaveRange, EffectModifierType.Percent, 0.15f));
            WaveSpell wave = CreateWave(5f, 90f);
            SpellPropertyModifiers modifiers = effects.CreateSpellPropertyModifiers(wave);

            Assert.AreEqual(5.75f, ResolveWaveRange(wave, modifiers));
            Assert.AreEqual(90f, ResolveWaveAngle(wave, modifiers));
        }

        [Test]
        public void ExpandedZone_IncreasesRadiusByFifteenPercent()
        {
            effects.Add(CreateModifier(SpellProperty.ZoneRadius, EffectModifierType.Percent, 0.15f));
            EarthZoneSpell zone = CreateZone<EarthZoneSpell>();

            Assert.AreEqual(5.75f, ResolveZoneRadius(
                zone,
                effects.CreateSpellPropertyModifiers(zone)), 0.0001f);
        }

        [Test]
        public void AreaExpansion_CombinesWithWideWaveAndExtendedWave()
        {
            effects.Add(CreateModifier(SpellProperty.Area, EffectModifierType.Percent, 0.2f));
            effects.Add(CreateModifier(SpellProperty.WaveAngle, EffectModifierType.Flat, 20f));
            effects.Add(CreateModifier(SpellProperty.WaveRange, EffectModifierType.Percent, 0.15f));
            WaveSpell wave = CreateWave(5f, 90f);
            SpellPropertyModifiers modifiers = effects.CreateSpellPropertyModifiers(wave);

            Assert.AreEqual(132f, ResolveWaveAngle(wave, modifiers));
            Assert.AreEqual(5.75f, ResolveWaveRange(wave, modifiers));
        }

        [Test]
        public void AreaExpansion_CombinesWithExpandedZoneAfterFlatAndPercentModifiers()
        {
            effects.Add(CreateModifier(SpellProperty.Area, EffectModifierType.Percent, 0.2f));
            effects.Add(CreateModifier(SpellProperty.ZoneRadius, EffectModifierType.Flat, 2f));
            effects.Add(CreateModifier(SpellProperty.ZoneRadius, EffectModifierType.Percent, 0.15f));
            EarthZoneSpell zone = CreateZone<EarthZoneSpell>();

            float expectedRadius = (5f + 2f) * 1.15f * Mathf.Sqrt(1.2f);
            Assert.AreEqual(expectedRadius, ResolveZoneRadius(
                zone,
                effects.CreateSpellPropertyModifiers(zone)), 0.0001f);
        }

        [Test]
        public void PropertyModifiers_ApplyFlatBeforePercentAndFilterByGeometryCapability()
        {
            effects.Add(CreateModifier(SpellProperty.WaveRange, EffectModifierType.Flat, 2f));
            effects.Add(CreateModifier(SpellProperty.WaveRange, EffectModifierType.Percent, 0.15f));
            effects.Add(CreateModifier(SpellProperty.ZoneRadius, EffectModifierType.Percent, 0.15f));
            WaveSpell wave = CreateWave(5f, 90f);
            EarthZoneSpell zone = CreateZone<EarthZoneSpell>();

            Assert.AreEqual(8.05f, ResolveWaveRange(
                wave,
                effects.CreateSpellPropertyModifiers(wave)), 0.0001f);
            SpellPropertyModifiers zoneModifiers = effects.CreateSpellPropertyModifiers(zone);
            Assert.AreEqual(0f, zoneModifiers.GetFlatValue(SpellProperty.WaveRange));
            Assert.AreEqual(0.15f, zoneModifiers.GetPercentValue(SpellProperty.ZoneRadius));
        }

        [Test]
        public void WaveAngle_IsClampedToThreeHundredSixtyDegrees()
        {
            effects.Add(CreateModifier(SpellProperty.WaveAngle, EffectModifierType.Flat, 20f));
            WaveSpell wave = CreateWave(5f, 350f);

            Assert.AreEqual(360f, ResolveWaveAngle(
                wave,
                effects.CreateSpellPropertyModifiers(wave)));
        }

        [Test]
        public void GeometryPerks_DoNotApplyToProjectileSpells()
        {
            effects.Add(CreateModifier(SpellProperty.Area, EffectModifierType.Percent, 0.2f));
            effects.Add(CreateModifier(SpellProperty.WaveAngle, EffectModifierType.Flat, 20f));
            effects.Add(CreateModifier(SpellProperty.ZoneRadius, EffectModifierType.Percent, 0.15f));
            ProjectileSpell projectile = ScriptableObject.CreateInstance<ProjectileSpell>();
            createdObjects.Add(projectile);

            Assert.AreSame(SpellPropertyModifiers.Empty,
                effects.CreateSpellPropertyModifiers(projectile));
        }

        [Test]
        public void ZoneActivation_UsesEffectiveRadiusForPlacementIndicator()
        {
            effects.Add(CreateModifier(SpellProperty.Area, EffectModifierType.Percent, 0.2f));
            EarthZoneSpell zone = CreateZone<EarthZoneSpell>();
            GameObject caster = new("Caster");
            createdObjects.Add(caster);
            TestActivationService service = new();
            CasterData casterData = new CasterDataBuilder(caster, caster.transform, null)
                .WithPropertyModifiers(effects.CreateSpellPropertyModifiers(zone))
                .Build();

            zone.OnActivation(casterData).Activate(service, casterData);

            Assert.AreSame(zone, service.Spell);
            Assert.AreEqual(5f * Mathf.Sqrt(1.2f), service.ReceivedRadius, 0.0001f);
        }

        private void AssertZoneAreaExpansion(ZoneSpell zone)
        {
            float effectiveRadius = ResolveZoneRadius(
                zone,
                effects.CreateSpellPropertyModifiers(zone));
            Assert.AreEqual(5f * Mathf.Sqrt(1.2f), effectiveRadius, 0.0001f);
            Assert.AreEqual(Mathf.PI * 5f * 5f * 1.2f,
                Mathf.PI * effectiveRadius * effectiveRadius,
                0.0001f);
        }

        private static float ResolveWaveRange(WaveSpell wave, SpellPropertyModifiers modifiers)
        {
            return modifiers.Resolve(
                wave.Radius,
                SpellProperty.Radius,
                SpellProperty.WaveRange);
        }

        private static float ResolveWaveAngle(WaveSpell wave, SpellPropertyModifiers modifiers)
        {
            return Mathf.Clamp(
                modifiers.Resolve(wave.WaveAngle, SpellProperty.WaveAngle)
                * modifiers.GetPercentMultiplier(SpellProperty.Area),
                0f,
                360f);
        }

        private static float ResolveZoneRadius(IZoneCapability zone, SpellPropertyModifiers modifiers)
        {
            return modifiers.Resolve(
                       zone.Radius,
                       SpellProperty.Radius,
                       SpellProperty.ZoneRadius)
                   * Mathf.Sqrt(modifiers.GetPercentMultiplier(SpellProperty.Area));
        }

        private WaveSpell CreateWave(float range, float angle)
        {
            WaveSpell wave = ScriptableObject.CreateInstance<WaveSpell>();
            createdObjects.Add(wave);
            SerializedObject serializedWave = new(wave);
            serializedWave.FindProperty("range").floatValue = range;
            serializedWave.FindProperty("angle").floatValue = angle;
            serializedWave.ApplyModifiedPropertiesWithoutUndo();
            return wave;
        }

        private T CreateZone<T>() where T : ZoneSpell
        {
            T zone = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(zone);
            SerializedObject serializedZone = new(zone);
            serializedZone.FindProperty("radius").floatValue = 5f;
            serializedZone.ApplyModifiedPropertiesWithoutUndo();
            return zone;
        }

        private SpellPropertyModifierEffectDefinition CreateModifier(
            SpellProperty property,
            EffectModifierType modifierType,
            float value)
        {
            SpellPropertyModifierEffectDefinition effect =
                ScriptableObject.CreateInstance<SpellPropertyModifierEffectDefinition>();
            createdObjects.Add(effect);
            SerializedObject serializedEffect = new(effect);
            serializedEffect.FindProperty("property").enumValueIndex = (int)property;
            serializedEffect.FindProperty("modifierType").enumValueIndex = (int)modifierType;
            serializedEffect.FindProperty("value").floatValue = value;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            return effect;
        }

        private sealed class TestActivationService : ISpellActivationService
        {
            public IIndicatorSpell Spell { get; private set; }
            public float ReceivedRadius { get; private set; }
            public bool HasValidPlacement => false;

            public void BeginPlacement(IIndicatorSpell spell, Transform caster, float placementRadius)
            {
                Spell = spell;
                ReceivedRadius = placementRadius;
            }

            public void EndPlacement()
            {
            }

            public bool TryGetPlacement(out Vector3 position)
            {
                position = default;
                return false;
            }
        }
    }
}
