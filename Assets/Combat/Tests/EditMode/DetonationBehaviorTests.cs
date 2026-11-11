using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using RPGame.Core.Statistics;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class DetonationBehaviorTests
    {
        private readonly List<Object> createdObjects = new();
        private GameObject caster;
        private BurningStatusDefinition burn;
        private StatisticsConfig statisticsConfig;

        [SetUp]
        public void SetUp()
        {
            TestDamageable.ResetDamageApplicationState();
            caster = CreateGameObject("Caster");
            burn = ScriptableObject.CreateInstance<BurningStatusDefinition>();
            createdObjects.Add(burn);
            statisticsConfig = CreateStatisticsConfig();
            createdObjects.Add(statisticsConfig);
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
        public void Death_WithDetonationBurn_ExplodesWithCurrentBurnDamageAndSource()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            StatusAggregator nearbyStatuses = CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);
            FireZoneBehaviour zone = CreateZone(new IRuntimeSpellBehavior[]
            {
                new IntensifyBehavior(),
                new DetonationBehavior()
            });

            ApplyBurn(zone, origin);
            ApplyBurn(zone, origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(9f, nearby.TotalDamage, 0.0001f);
            Assert.AreSame(caster, nearby.LastDamage.Source);
            Assert.AreEqual(DamageType.Magical, nearby.LastDamage.Parts[0].DamageType);
            Assert.AreEqual(DamageElement.Fire, nearby.LastDamage.Parts[0].DamageElement);
            Assert.IsFalse(nearbyStatuses.HasStatus(burn));
        }

        [Test]
        public void Death_WithoutBurnOrWithoutDetonation_DoesNotExplode()
        {
            StatusAggregator withoutBurn = CreateTarget("Without Burn", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);

            Physics.SyncTransforms();
            TriggerDeath(withoutBurn);
            Assert.AreEqual(0f, nearby.TotalDamage, 0.0001f);

            StatusAggregator withoutDetonation = CreateTarget("Without Detonation", new Vector3(10f, 0f, 0f), out _);
            CreateTarget("Other Nearby", new Vector3(11f, 0f, 0f), out TestDamageable otherNearby);
            ApplyBurn(CreateZone(System.Array.Empty<IRuntimeSpellBehavior>()), withoutDetonation);

            Physics.SyncTransforms();
            TriggerDeath(withoutDetonation);
            Assert.AreEqual(0f, otherNearby.TotalDamage, 0.0001f);
        }

        [Test]
        public void Death_UsesRemainingDurationAndCurrentIntensity()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);
            FireZoneBehaviour zone = CreateZone(new IRuntimeSpellBehavior[]
            {
                new IntensifyBehavior(),
                new DetonationBehavior()
            });

            ApplyBurn(zone, origin);
            ApplyBurn(zone, origin);
            ApplyBurn(zone, origin);
            TickStatuses(origin, 1f);

            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(9f, nearby.TotalDamage, 0.0001f);
        }

        [Test]
        public void Death_DetonatesBeforeStandardDeathCleanup()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);
            ApplyBurn(CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() }), origin);
            StatisticsController statistics = origin.GetComponent<StatisticsController>();
            bool cleanupObservedDetonation = false;
            statistics.Died += () =>
            {
                cleanupObservedDetonation = nearby.TotalDamage > 0f;
                origin.ResetForDespawn();
            };

            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.IsTrue(cleanupObservedDetonation);
        }

        [Test]
        public void Explosion_HitsTargetsOnlyOnceAndOnlyInsideRadius()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            GameObject inside = CreateGameObject("Inside");
            inside.transform.position = Vector3.one;
            TestDamageable insideDamageable = inside.AddComponent<TestDamageable>();
            inside.AddComponent<SphereCollider>();
            GameObject secondCollider = CreateGameObject("Second Collider");
            secondCollider.transform.SetParent(inside.transform);
            secondCollider.AddComponent<SphereCollider>();
            CreateTarget("Outside", new Vector3(4f, 0f, 0f), out TestDamageable outside);

            ApplyBurn(CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() }), origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(4.5f, insideDamageable.TotalDamage, 0.0001f);
            Assert.AreEqual(0f, outside.TotalDamage, 0.0001f);
        }

        [Test]
        public void Explosion_WithoutBlastRadius_DoesNotHitTargetOutsideBaseRadius()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Outside Base Radius", new Vector3(4f, 0f, 0f), out TestDamageable outside);

            ApplyBurn(CreateZone(CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: false)), origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(0f, outside.TotalDamage, 0.0001f);
        }

        [Test]
        public void Explosion_WithBlastRadius_HitsTargetInsideFourPointFiveMetersOnly()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Inside Blast Radius", new Vector3(4f, 0f, 0f), out TestDamageable inside);
            CreateTarget("Outside Blast Radius", new Vector3(5.1f, 0f, 0f), out TestDamageable outside);

            ApplyBurn(CreateZone(CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: true)), origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(4.5f, inside.TotalDamage, 0.0001f);
            Assert.AreEqual(0f, outside.TotalDamage, 0.0001f);
        }

        [Test]
        public void Explosion_WithBlastRadius_DoesNotIncreaseDamage()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);

            ApplyBurn(CreateZone(CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: true)), origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(4.5f, nearby.TotalDamage, 0.0001f);
        }

        [Test]
        public void BlastRadius_WithoutDetonation_DoesNotExplode()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);

            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: false,
                includeBlastRadius: true);
            ApplyBurn(CreateZone(behaviors), origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.IsEmpty(behaviors);
            Assert.AreEqual(0f, nearby.TotalDamage, 0.0001f);
        }

        [Test]
        public void BlastRadius_SupportsOnlyFireZoneSpell()
        {
            BlastRadiusEffectDefinition blastRadius = CreateObject<BlastRadiusEffectDefinition>();
            FireZoneSpell fireZone = CreateObject<FireZoneSpell>();
            EarthZoneSpell earthZone = CreateObject<EarthZoneSpell>();

            Assert.IsTrue(blastRadius.Supports(fireZone));
            Assert.IsFalse(blastRadius.Supports(earthZone));
        }

        [Test]
        public void BlastRadius_CompositionOrder_DoesNotChangeExplosionRadius()
        {
            StatusAggregator firstOrigin = CreateTarget("First Origin", Vector3.zero, out _);
            CreateTarget("First Target", new Vector3(4f, 0f, 0f), out TestDamageable firstTarget);
            IReadOnlyList<IRuntimeSpellBehavior> modifierFirst = CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: true,
                modifierFirst: true);
            ApplyBurn(CreateZone(modifierFirst), firstOrigin);

            GameObject secondCaster = CreateGameObject("Second Caster");
            StatusAggregator secondOrigin = CreateTarget("Second Origin", new Vector3(10f, 0f, 0f), out _);
            CreateTarget("Second Target", new Vector3(14f, 0f, 0f), out TestDamageable secondTarget);
            IReadOnlyList<IRuntimeSpellBehavior> behaviorFirst = CreateRuntimeBehaviorsWithEffects(
                secondCaster,
                includeDetonation: true,
                includeBlastRadius: true,
                modifierFirst: false);
            ApplyBurn(CreateZone(behaviorFirst, caster: secondCaster), secondOrigin);

            Physics.SyncTransforms();
            TriggerDeath(firstOrigin);
            TriggerDeath(secondOrigin);

            Assert.AreEqual(4.5f, firstTarget.TotalDamage, 0.0001f);
            Assert.AreEqual(4.5f, secondTarget.TotalDamage, 0.0001f);
        }

        [Test]
        public void Explosion_WithBlastRadius_CanTriggerDetonationChain()
        {
            StatusAggregator first = CreateTarget("First", Vector3.zero, out _);
            StatusAggregator second = CreateTarget("Second", new Vector3(4f, 0f, 0f), out TestDamageable secondDamageable);
            CreateTarget("Third", new Vector3(8f, 0f, 0f), out TestDamageable thirdDamageable);
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: true);
            FireZoneBehaviour zone = CreateZone(behaviors);

            ApplyBurn(zone, first);
            ApplyBurn(zone, second);
            secondDamageable.DieWhenDamaged = true;

            Physics.SyncTransforms();
            TriggerDeath(first);

            Assert.AreEqual(4.5f, thirdDamageable.TotalDamage, 0.0001f);
        }

        [Test]
        public void Explosion_WithOverpressure_IncreasesDamageByFiftyPercent()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);

            ApplyBurn(CreateZone(CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: false,
                includeOverpressure: true)), origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(6.75f, nearby.TotalDamage, 0.0001f);
        }

        [Test]
        public void Explosion_WithOverpressure_DoesNotIncreaseRadiusOrBurnDamage()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out TestDamageable originDamageable);
            CreateTarget("Outside Base Radius", new Vector3(4f, 0f, 0f), out TestDamageable outside);
            FireZoneBehaviour zone = CreateZone(CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: false,
                includeOverpressure: true));

            ApplyBurn(zone, origin);
            TickStatuses(origin, 1f);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(1.5f, originDamageable.TotalDamage, 0.0001f);
            Assert.AreEqual(0f, outside.TotalDamage, 0.0001f);
        }

        [Test]
        public void Overpressure_WithoutDetonation_DoesNotExplode()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);

            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: false,
                includeBlastRadius: false,
                includeOverpressure: true);
            ApplyBurn(CreateZone(behaviors), origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.IsEmpty(behaviors);
            Assert.AreEqual(0f, nearby.TotalDamage, 0.0001f);
        }

        [Test]
        public void Overpressure_SupportsOnlyFireZoneSpell()
        {
            OverpressureEffectDefinition overpressure = CreateObject<OverpressureEffectDefinition>();
            FireZoneSpell fireZone = CreateObject<FireZoneSpell>();
            EarthZoneSpell earthZone = CreateObject<EarthZoneSpell>();

            Assert.IsTrue(overpressure.Supports(fireZone));
            Assert.IsFalse(overpressure.Supports(earthZone));
        }

        [Test]
        public void Explosion_WithIntensifyAndOverpressure_UsesCurrentIntensity()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);
            List<IRuntimeSpellBehavior> behaviors = new(CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: false,
                includeOverpressure: true))
            {
                new IntensifyBehavior()
            };
            FireZoneBehaviour zone = CreateZone(behaviors);

            ApplyBurn(zone, origin);
            ApplyBurn(zone, origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(13.5f, nearby.TotalDamage, 0.0001f);
        }

        [Test]
        public void Explosion_WithBlastRadiusAndOverpressure_ModifiesRadiusAndDamageIndependently()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Inside Blast Radius", new Vector3(4f, 0f, 0f), out TestDamageable inside);
            CreateTarget("Outside Blast Radius", new Vector3(5.1f, 0f, 0f), out TestDamageable outside);

            ApplyBurn(CreateZone(CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: true,
                includeOverpressure: true)), origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(6.75f, inside.TotalDamage, 0.0001f);
            Assert.AreEqual(0f, outside.TotalDamage, 0.0001f);
        }

        [Test]
        public void Overpressure_CompositionOrder_DoesNotChangeExplosionDamage()
        {
            StatusAggregator firstOrigin = CreateTarget("First Origin", Vector3.zero, out _);
            CreateTarget("First Target", Vector3.one, out TestDamageable firstTarget);
            ApplyBurn(CreateZone(CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: true,
                includeOverpressure: true,
                modifierFirst: true)), firstOrigin);

            GameObject secondCaster = CreateGameObject("Second Caster");
            StatusAggregator secondOrigin = CreateTarget("Second Origin", new Vector3(10f, 0f, 0f), out _);
            CreateTarget("Second Target", new Vector3(11f, 0f, 0f), out TestDamageable secondTarget);
            ApplyBurn(CreateZone(CreateRuntimeBehaviorsWithEffects(
                secondCaster,
                includeDetonation: true,
                includeBlastRadius: true,
                includeOverpressure: true,
                modifierFirst: false), caster: secondCaster), secondOrigin);

            Physics.SyncTransforms();
            TriggerDeath(firstOrigin);
            TriggerDeath(secondOrigin);

            Assert.AreEqual(6.75f, firstTarget.TotalDamage, 0.0001f);
            Assert.AreEqual(6.75f, secondTarget.TotalDamage, 0.0001f);
        }

        [Test]
        public void Explosion_WithOverpressure_CanTriggerDetonationChain()
        {
            StatusAggregator first = CreateTarget("First", Vector3.zero, out _);
            StatusAggregator second = CreateTarget("Second", Vector3.one, out TestDamageable secondDamageable);
            CreateTarget("Third", new Vector3(3.6f, 0f, 0f), out TestDamageable thirdDamageable);
            IReadOnlyList<IRuntimeSpellBehavior> behaviors = CreateRuntimeBehaviorsWithEffects(
                caster,
                includeDetonation: true,
                includeBlastRadius: false,
                includeOverpressure: true);
            FireZoneBehaviour zone = CreateZone(behaviors);

            ApplyBurn(zone, first);
            ApplyBurn(zone, second);
            secondDamageable.DieWhenDamaged = true;

            Physics.SyncTransforms();
            TriggerDeath(first);

            Assert.AreEqual(6.75f, thirdDamageable.TotalDamage, 0.0001f);
        }

        [Test]
        public void Explosion_CanTriggerOneDetonationChain()
        {
            StatusAggregator first = CreateTarget("First", Vector3.zero, out _);
            StatusAggregator second = CreateTarget("Second", new Vector3(1f, 0f, 0f), out TestDamageable secondDamageable);
            CreateTarget("Third", new Vector3(3.6f, 0f, 0f), out TestDamageable thirdDamageable);
            FireZoneBehaviour zone = CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() });

            ApplyBurn(zone, first);
            ApplyBurn(zone, second);
            secondDamageable.DieWhenDamaged = true;

            Physics.SyncTransforms();
            TriggerDeath(first);

            Assert.AreEqual(4.5f, thirdDamageable.TotalDamage, 0.0001f);
        }

        [Test]
        public void Explosion_ProcessesLongChainIterativelyInFifoOrder()
        {
            const int chainLength = 50;
            FireZoneBehaviour zone = CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() });
            List<StatusAggregator> chain = new();

            for (int i = 0; i < chainLength; i++)
            {
                StatusAggregator target = CreateTarget(
                    $"Chain {i}",
                    new Vector3(i * 3.1f, 0f, 0f),
                    out TestDamageable damageable);
                damageable.DieWhenDamaged = i > 0;
                ApplyBurn(zone, target);
                chain.Add(target);
            }

            Physics.SyncTransforms();
            TriggerDeath(chain[0]);

            Assert.AreEqual(chainLength - 1, TestDamageable.DamageRecipients.Count);
            Assert.AreEqual(1, TestDamageable.MaximumDamageApplicationDepth);
            for (int i = 1; i < chainLength; i++)
            {
                Assert.AreEqual($"Chain {i}", TestDamageable.DamageRecipients[i - 1]);
            }
        }

        [Test]
        public void ExplosionException_ClearsPendingExplosionsBeforeNextDeath()
        {
            StatusAggregator firstOrigin = CreateTarget("First Origin", Vector3.zero, out _);
            CreateTarget("Throwing Target", Vector3.one, out TestDamageable throwingDamageable);
            throwingDamageable.ThrowOnDamage = true;
            ApplyBurn(CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() }), firstOrigin);

            Physics.SyncTransforms();
            Assert.Throws<System.InvalidOperationException>(() => TriggerDeath(firstOrigin));

            StatusAggregator secondOrigin = CreateTarget("Second Origin", new Vector3(10f, 0f, 0f), out _);
            CreateTarget("Second Target", new Vector3(11f, 0f, 0f), out TestDamageable secondDamageable);
            ApplyBurn(CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() }), secondOrigin);

            Physics.SyncTransforms();
            TriggerDeath(secondOrigin);

            Assert.AreEqual(4.5f, secondDamageable.TotalDamage, 0.0001f);
        }

        [Test]
        public void Death_FromAnotherDamageSource_StillTriggersDetonation()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);
            ApplyBurn(CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() }), origin);

            Physics.SyncTransforms();
            origin.GetComponent<StatisticsController>().TakeDamage(100f);

            Assert.AreEqual(4.5f, nearby.TotalDamage, 0.0001f);
        }

        [Test]
        public void LastBurnApplicationWithoutDetonation_CancelsDetonation()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);
            GameObject secondCaster = CreateGameObject("Second Caster");

            ApplyBurn(CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() }), origin);
            ApplyBurn(CreateZone(System.Array.Empty<IRuntimeSpellBehavior>(), caster: secondCaster), origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(0f, nearby.TotalDamage, 0.0001f);
        }

        [Test]
        public void LastDetonationApplication_UsesItsCasterAsExplosionSource()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);
            GameObject secondCaster = CreateGameObject("Second Caster");

            ApplyBurn(CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() }), origin);
            ApplyBurn(CreateZone(
                new IRuntimeSpellBehavior[] { new DetonationBehavior() },
                caster: secondCaster), origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreSame(secondCaster, nearby.LastDamage.Source);
        }

        [Test]
        public void RepeatedDeathAndStatusClearing_DoNotCauseAnotherExplosion()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);
            ApplyBurn(CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() }), origin);

            Physics.SyncTransforms();
            TriggerDeath(origin);
            TriggerDeath(origin);
            origin.ClearStatuses();

            Assert.AreEqual(4.5f, nearby.TotalDamage, 0.0001f);
        }

        [Test]
        public void ClearStatusesAndDespawn_DoNotDetonateAndAllowReuse()
        {
            StatusAggregator origin = CreateTarget("Origin", Vector3.zero, out _);
            CreateTarget("Nearby", Vector3.one, out TestDamageable nearby);
            FireZoneBehaviour zone = CreateZone(new IRuntimeSpellBehavior[] { new DetonationBehavior() });

            ApplyBurn(zone, origin);
            origin.ClearStatuses();
            Assert.AreEqual(0f, nearby.TotalDamage, 0.0001f);

            ApplyBurn(zone, origin);
            origin.ResetForDespawn();
            Assert.AreEqual(0f, nearby.TotalDamage, 0.0001f);

            origin.GetComponent<StatisticsController>().ResetForSpawn();
            ApplyBurn(zone, origin);
            Physics.SyncTransforms();
            TriggerDeath(origin);

            Assert.AreEqual(4.5f, nearby.TotalDamage, 0.0001f);
        }

        private FireZoneBehaviour CreateZone(
            IReadOnlyList<IRuntimeSpellBehavior> behaviors,
            float burningDuration = 3f,
            GameObject caster = null)
        {
            FireZoneBehaviour zone = CreateGameObject("Fire Zone").AddComponent<FireZoneBehaviour>();
            SerializedObject serializedZone = new(zone);
            serializedZone.FindProperty("burningStatus").objectReferenceValue = burn;
            serializedZone.FindProperty("burningDuration").floatValue = burningDuration;
            serializedZone.ApplyModifiedPropertiesWithoutUndo();
            zone.Initialize(
                new CasterDataBuilder(
                    caster ?? this.caster,
                    (caster ?? this.caster).transform,
                    (caster ?? this.caster).transform)
                    .WithRuntimeBehaviors(behaviors)
                    .Build(),
                1f);
            return zone;
        }

        private IReadOnlyList<IRuntimeSpellBehavior> CreateRuntimeBehaviorsWithEffects(
            GameObject effectOwner,
            bool includeDetonation,
            bool includeBlastRadius,
            bool includeOverpressure = false,
            bool modifierFirst = false)
        {
            EffectAggregator effects = effectOwner.AddComponent<EffectAggregator>();
            RuntimeBehaviorEffectDefinition detonation = null;
            BlastRadiusEffectDefinition blastRadius = null;
            OverpressureEffectDefinition overpressure = null;

            if (includeDetonation)
            {
                detonation = CreateObject<RuntimeBehaviorEffectDefinition>();
                SetRuntimeBehavior(detonation, new DetonationBehavior());
            }

            if (includeBlastRadius)
            {
                blastRadius = CreateObject<BlastRadiusEffectDefinition>();
            }

            if (includeOverpressure)
            {
                overpressure = CreateObject<OverpressureEffectDefinition>();
            }

            if (modifierFirst)
            {
                effects.Add(blastRadius);
                effects.Add(overpressure);
                effects.Add(detonation);
            }
            else
            {
                effects.Add(detonation);
                effects.Add(blastRadius);
                effects.Add(overpressure);
            }

            FireZoneSpell spell = CreateObject<FireZoneSpell>();
            return effects.CreateRuntimeBehaviors(spell, effectOwner);
        }

        private static void ApplyBurn(FireZoneBehaviour zone, StatusAggregator target)
        {
            typeof(FireZoneBehaviour).GetMethod(
                "ApplyStatusTo",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(zone, new object[] { target });
        }

        private StatusAggregator CreateTarget(
            string targetName,
            Vector3 position,
            out TestDamageable damageable)
        {
            GameObject target = CreateGameObject(targetName);
            target.transform.position = position;
            StatisticsController statistics = target.AddComponent<StatisticsController>();
            SetStatisticsConfig(statistics, statisticsConfig);
            statistics.ResetToConfig();
            damageable = target.AddComponent<TestDamageable>();
            damageable.Initialize(statistics);
            target.AddComponent<SphereCollider>();
            StatusAggregator statuses = target.AddComponent<StatusAggregator>();
            InvokePrivate(statuses, "Awake");
            InvokePrivate(statuses, "OnEnable");
            return statuses;
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private T CreateObject<T>() where T : ScriptableObject
        {
            T createdObject = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(createdObject);
            return createdObject;
        }

        private static void SetRuntimeBehavior(
            RuntimeBehaviorEffectDefinition effect,
            IRuntimeSpellBehavior behavior)
        {
            SerializedObject serializedEffect = new(effect);
            serializedEffect.FindProperty("behavior").managedReferenceValue = behavior;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void TriggerDeath(StatusAggregator statuses)
        {
            StatisticsController statistics = statuses.GetComponent<StatisticsController>();
            statistics.TakeDamage(statistics.CurrentHealth);
        }

        private static void TickStatuses(StatusAggregator statuses, float deltaTime)
        {
            FieldInfo field = typeof(StatusAggregator).GetField(
                "statusContainer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            ((StatusContainer)field.GetValue(statuses)).Tick(deltaTime);
        }

        private static StatisticsConfig CreateStatisticsConfig()
        {
            StatisticsConfig config = ScriptableObject.CreateInstance<StatisticsConfig>();
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("maxHealth").floatValue = 100f;
            serializedConfig.FindProperty("maxStamina").floatValue = 0f;
            serializedConfig.FindProperty("maxMana").floatValue = 0f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static void SetStatisticsConfig(
            StatisticsController statistics,
            StatisticsConfig config)
        {
            SerializedObject serializedStatistics = new(statistics);
            serializedStatistics.FindProperty("config").objectReferenceValue = config;
            serializedStatistics.FindProperty("initializeOnAwake").boolValue = false;
            serializedStatistics.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            private static int damageApplicationDepth;
            private static readonly List<string> damageRecipients = new();
            private StatisticsController statistics;

            public bool DieWhenDamaged { get; set; }
            public bool ThrowOnDamage { get; set; }
            public bool CanReceiveDamage => statistics == null || statistics.IsAlive;
            public float TotalDamage { get; private set; }
            public DamageData LastDamage { get; private set; }
            public static int MaximumDamageApplicationDepth { get; private set; }
            public static IReadOnlyList<string> DamageRecipients => damageRecipients;

            public static void ResetDamageApplicationState()
            {
                damageApplicationDepth = 0;
                MaximumDamageApplicationDepth = 0;
                damageRecipients.Clear();
            }

            public void Initialize(StatisticsController statisticsController)
            {
                statistics = statisticsController;
            }

            public DamageResult ApplyDamage(DamageData data)
            {
                if (!CanReceiveDamage || !data.HasDamage)
                {
                    return DamageResult.Ignored(data, 0f);
                }

                damageApplicationDepth++;
                MaximumDamageApplicationDepth = Mathf.Max(
                    MaximumDamageApplicationDepth,
                    damageApplicationDepth);
                try
                {
                    if (ThrowOnDamage)
                    {
                        throw new System.InvalidOperationException("Damage application failed.");
                    }

                    TotalDamage += data.Amount;
                    LastDamage = data;
                    damageRecipients.Add(name);
                    if (DieWhenDamaged)
                    {
                        statistics.TakeDamage(statistics.CurrentHealth);
                    }
                }
                finally
                {
                    damageApplicationDepth--;
                }

                return DamageResult.Applied(data, data.Amount, 100f, 100f - data.Amount, false);
            }
        }
    }
}
