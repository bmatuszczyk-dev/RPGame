using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statistics.Attributes;
using RPGame.Core.Statuses;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class DischargeBehaviorTests
    {
        private readonly List<Object> createdObjects = new();
        private GameObject caster;
        private GameObject target;
        private MarkStatusDefinition markDefinition;
        private StatusAggregator statusReceiver;
        private TestAttributes casterAttributes;
        private TestDamageable targetDamageable;

        [SetUp]
        public void SetUp()
        {
            caster = CreateGameObject("Caster");
            casterAttributes = caster.AddComponent<TestAttributes>();
            target = CreateGameObject("Target");
            targetDamageable = target.AddComponent<TestDamageable>();
            targetDamageable.SetHealth(100f);
            statusReceiver = target.AddComponent<StatusAggregator>();
            InvokeAwake(statusReceiver);
            markDefinition = ScriptableObject.CreateInstance<MarkStatusDefinition>();
            createdObjects.Add(markDefinition);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void Resolve_WithoutMark_DoesNotDealDischargeDamage()
        {
            ResolveDischarge();

            Assert.AreEqual(0, targetDamageable.Results.Count);
            Assert.AreEqual(100f, targetDamageable.CurrentHealth);
        }

        [Test]
        public void Resolve_WithMark_ConsumesMarkAndDealsBaseDamage()
        {
            ApplyMark();

            ResolveDischarge();

            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
            Assert.AreEqual(1, targetDamageable.Results.Count);
            Assert.AreEqual(10f, targetDamageable.Results[0].AppliedAmount, 0.0001f);
        }

        [Test]
        public void Resolve_WithPower_AddsScaledDamage()
        {
            casterAttributes.Power = 20;
            ApplyMark();

            ResolveDischarge(baseDamage: 10f, powerScaling: 0.15f);

            Assert.AreEqual(13f, targetDamageable.Results[0].AppliedAmount, 0.0001f);
        }

        [Test]
        public void Resolve_DischargeUsesMagicalThunderDamage()
        {
            ApplyMark();

            ResolveDischarge();

            PartialDamage damage = targetDamageable.Results[0].Data.Parts[0];
            Assert.AreEqual(DamageType.Magical, damage.DamageType);
            Assert.AreEqual(DamageElement.Thunder, damage.DamageElement);
        }

        [Test]
        public void Resolve_WhenStunApplicationFails_DoesNotDealDischargeDamage()
        {
            ApplyMark();

            ResolveDischarge(resolveSucceeds: false);

            Assert.IsTrue(statusReceiver.HasStatus(markDefinition));
            Assert.AreEqual(0, targetDamageable.Results.Count);
            Assert.AreEqual(100f, targetDamageable.CurrentHealth);
        }

        [Test]
        public void Resolve_WhenAnotherConsumerUsesMark_DealsDischargeDamageOnce()
        {
            ApplyMark();
            SharedMarkConsumer firstConsumer = new(markDefinition);

            ResolveDischarge(runtimeBehaviors: new IRuntimeSpellBehavior[]
            {
                firstConsumer,
                new DischargeBehavior(markDefinition, 10f, 0.15f, caster)
            });

            Assert.IsTrue(firstConsumer.ConsumedMark);
            Assert.IsFalse(statusReceiver.HasStatus(markDefinition));
            Assert.AreEqual(1, targetDamageable.Results.Count);
            Assert.AreEqual(10f, targetDamageable.Results[0].AppliedAmount, 0.0001f);
        }

        [Test]
        public void Supports_OnlyLightningZoneSpell()
        {
            DischargeBehavior behavior = new(markDefinition, 10f, 0.15f, caster);
            LightningZoneSpell lightningZone = ScriptableObject.CreateInstance<LightningZoneSpell>();
            EarthZoneSpell earthZone = ScriptableObject.CreateInstance<EarthZoneSpell>();
            createdObjects.Add(lightningZone);
            createdObjects.Add(earthZone);

            Assert.IsTrue(behavior.Supports(lightningZone));
            Assert.IsFalse(behavior.Supports(earthZone));
        }

        private void ApplyMark()
        {
            statusReceiver.ApplyStatus(
                markDefinition,
                5f,
                new StatusContext(new StatusSourceId("Test"), caster));
        }

        private void ResolveDischarge(
            float baseDamage = 10f,
            float powerScaling = 0.15f,
            bool resolveSucceeds = true,
            IReadOnlyList<IRuntimeSpellBehavior> runtimeBehaviors = null)
        {
            runtimeBehaviors ??= new IRuntimeSpellBehavior[]
            {
                new DischargeBehavior(markDefinition, baseDamage, powerScaling, caster)
            };
            CasterData casterData = new CasterDataBuilder(caster, caster.transform, target.transform)
                .WithSpellId(new SpellId("lightning_zone"))
                .WithRuntimeBehaviors(runtimeBehaviors)
                .Build();

            SpellBehaviorPipelineExecutor.Resolve(
                casterData,
                target,
                new StunResolveBehavior(resolveSucceeds));
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeAwake(StatusAggregator statusReceiver)
        {
            MethodInfo awake = typeof(StatusAggregator).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(statusReceiver, null);
        }

        private sealed class StunResolveBehavior : ISpellResolveBehavior
        {
            private readonly bool succeeds;

            public StunResolveBehavior(bool succeeds)
            {
                this.succeeds = succeeds;
            }

            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;

            public bool Resolve(SpellBehaviorContext context)
            {
                return succeeds;
            }
        }

        private sealed class SharedMarkConsumer : IMarkConsumer
        {
            private readonly MarkStatusDefinition markDefinition;

            public SharedMarkConsumer(MarkStatusDefinition markDefinition)
            {
                this.markDefinition = markDefinition;
            }

            public bool ConsumedMark { get; private set; }

            public bool TryConsumeMark(SpellBehaviorContext context)
            {
                if (context.HasExecutionFlag(SpellExecutionFlag.MarkConsumed))
                {
                    return true;
                }

                if (context.StatusReceiver == null
                    || !context.StatusReceiver.TryConsumeStatus(markDefinition))
                {
                    return false;
                }

                context.SetExecutionFlag(SpellExecutionFlag.MarkConsumed);
                ConsumedMark = true;
                return true;
            }
        }

        private sealed class TestAttributes : MonoBehaviour, ICharacterAttributes
        {
            public int Strength => 0;
            public int Dexterity => 0;
            public int Endurance => 0;
            public int Vitality => 0;
            public int Intelligence => 0;
            public int Power { get; set; }

            public int GetValue(CharacterAttributeType attributeType)
            {
                return attributeType == CharacterAttributeType.Power ? Power : 0;
            }
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public List<DamageResult> Results { get; } = new();
            public bool CanReceiveDamage => CurrentHealth > 0f;
            public float CurrentHealth { get; private set; }

            public void SetHealth(float currentHealth)
            {
                CurrentHealth = currentHealth;
                Results.Clear();
            }

            public DamageResult ApplyDamage(DamageData data)
            {
                if (!CanReceiveDamage || !data.HasDamage)
                {
                    DamageResult ignored = DamageResult.Ignored(data, CurrentHealth);
                    Results.Add(ignored);
                    return ignored;
                }

                float previousHealth = CurrentHealth;
                CurrentHealth = Mathf.Max(0f, CurrentHealth - data.Amount);
                float appliedAmount = previousHealth - CurrentHealth;
                DamageResult result = DamageResult.Applied(
                    data,
                    appliedAmount,
                    previousHealth,
                    CurrentHealth,
                    CurrentHealth <= 0f);
                Results.Add(result);
                return result;
            }
        }
    }
}
