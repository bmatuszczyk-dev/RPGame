using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using RPGame.Core.Spells.Symbols;
using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using RPGame.Player.Spells;
using TargetingController = RPGame.Player.Targeting.TargetingController;
using UnityEditor;
using UnityEngine;

namespace RPGame.Player.Tests
{
    public sealed class CastControllerTests
    {
        private GameObject playerObject;
        private TargetingController targeting;
        private CastController controller;
        private CaptureCasterDataSpell spell;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("CastControllerTests");
            targeting = playerObject.AddComponent<TargetingController>();
            controller = playerObject.AddComponent<CastController>();
            spell = ScriptableObject.CreateInstance<CaptureCasterDataSpell>();

            SetField(controller, "targeting", targeting);
            SetField(controller, "casterObject", playerObject);
            SetField(controller, "castOrigin", playerObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(spell);
        }

        [Test]
        public void CastSpell_BuildsCasterDataWithCurrentTarget()
        {
            GameObject targetObject = new("Target");
            try
            {
                SetCurrentTarget(new TestTargetable(targetObject.transform));

                InvokeCastSpell(spell);

                Assert.AreSame(targetObject.transform, spell.LastCasterData.Target);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void CastSpell_WithNoCurrentTarget_BuildsCasterDataWithNullTarget()
        {
            SetCurrentTarget(null);

            InvokeCastSpell(spell);

            Assert.IsNull(spell.LastCasterData.Target);
        }

        [Test]
        public void CreateCasterData_TargetIsSnapshotOfCurrentCast()
        {
            GameObject firstTargetObject = new("FirstTarget");
            GameObject secondTargetObject = new("SecondTarget");
            try
            {
                SetCurrentTarget(new TestTargetable(firstTargetObject.transform));

                CasterData casterData = InvokeCreateCasterData();
                SetCurrentTarget(new TestTargetable(secondTargetObject.transform));

                Assert.AreSame(firstTargetObject.transform, casterData.Target);
            }
            finally
            {
                Object.DestroyImmediate(firstTargetObject);
                Object.DestroyImmediate(secondTargetObject);
            }
        }

        [Test]
        public void CreateCasterData_IncludesPlayerStatistics()
        {
            StatisticsController statisticsController = playerObject.AddComponent<StatisticsController>();

            CasterData casterData = InvokeCreateCasterData();

            Assert.AreSame(statisticsController, casterData.Statistics);
        }

        [Test]
        public void SpellSymbolCaster_DoesNotDependOnTargeting()
        {
            FieldInfo[] fields = typeof(SpellSymbolCaster).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < fields.Length; i++)
            {
                Assert.AreNotSame(typeof(TargetingController), fields[i].FieldType);
            }
        }

        [Test]
        public void SpellSymbolCaster_EmitsSelectedSpell()
        {
            SpellSymbolCaster symbolCaster = playerObject.AddComponent<SpellSymbolCaster>();
            SetSpellSymbolEntries(symbolCaster, symbolId: 3, spell);
            Spell selectedSpell = null;
            symbolCaster.SpellSelected += emittedSpell => selectedSpell = emittedSpell;

            symbolCaster.ReceiveSymbol(new SymbolRecognitionResult(3, 1f));

            Assert.AreSame(spell, selectedSpell);
        }

        [Test]
        public void CastController_CastsSpellSelectedBySpellSymbolCaster()
        {
            SpellSymbolCaster symbolCaster = playerObject.AddComponent<SpellSymbolCaster>();
            SetSpellSymbolEntries(symbolCaster, symbolId: 5, spell);
            SetField(controller, "spellSymbolCaster", symbolCaster);

            InvokeLifecycle("OnEnable");
            symbolCaster.ReceiveSymbol(new SymbolRecognitionResult(5, 1f));
            InvokeLifecycle("OnDisable");

            Assert.AreSame(playerObject, spell.LastCasterData.CasterObject);
        }

        [Test]
        public void CastSpell_WhenSuccessful_UpdatesLastUsedSpellDamageRanges()
        {
            DamageRangeSpell damageRangeSpell = ScriptableObject.CreateInstance<DamageRangeSpell>();
            try
            {
                InvokeCastSpell(damageRangeSpell);

                Assert.IsTrue(controller.TryGetLastUsedSpellDamageRanges(out IReadOnlyList<PartialDamageRange> damageRanges));
                Assert.AreEqual(1, damageRanges.Count);
                Assert.AreEqual(3f, damageRanges[0].MinDamage);
                Assert.AreEqual(7f, damageRanges[0].MaxDamage);
            }
            finally
            {
                Object.DestroyImmediate(damageRangeSpell);
            }
        }

        [Test]
        public void CastSpell_WhenLastUsedSpellChanges_RaisesDamageRangeChanged()
        {
            int changedCount = 0;
            controller.LastUsedSpellDamageRangeChanged += () => changedCount++;

            InvokeCastSpell(spell);

            Assert.AreEqual(1, changedCount);
        }

        [Test]
        public void CastSpell_IncludesRuntimeBehaviorsFromProvider()
        {
            TestRuntimeBehaviorProvider provider = playerObject.AddComponent<TestRuntimeBehaviorProvider>();

            InvokeCastSpell(spell);

            Assert.AreEqual(1, spell.LastCasterData.RuntimeBehaviors.Count);
            Assert.AreSame(provider.Behavior, spell.LastCasterData.RuntimeBehaviors[0]);
        }

        [Test]
        public void CastSpell_IncludesSpellId()
        {
            SerializedObject serializedSpell = new(spell);
            serializedSpell.FindProperty("id").stringValue = "wave";
            serializedSpell.ApplyModifiedPropertiesWithoutUndo();

            InvokeCastSpell(spell);

            Assert.AreEqual(new SpellId("wave"), spell.LastCasterData.SpellId);
        }

        [Test]
        public void CastSpell_IncludesPropertyModifiersFromEffectAggregator()
        {
            EffectAggregator aggregator = playerObject.AddComponent<EffectAggregator>();
            SpellPropertyModifierEffectDefinition effect =
                ScriptableObject.CreateInstance<SpellPropertyModifierEffectDefinition>();
            SerializedObject serializedEffect = new(effect);
            serializedEffect.FindProperty("property").enumValueIndex = (int)SpellProperty.Radius;
            serializedEffect.FindProperty("value").floatValue = 2f;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            aggregator.Add(effect);

            InvokeCastSpell(spell);

            Assert.AreEqual(2f, spell.LastCasterData.PropertyModifiers.GetValue(SpellProperty.Radius));
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void ActivationPreview_IncludesPropertyModifiersWithoutRuntimeBehaviors()
        {
            EffectAggregator aggregator = playerObject.AddComponent<EffectAggregator>();
            playerObject.AddComponent<TestRuntimeBehaviorProvider>();
            SpellPropertyModifierEffectDefinition effect =
                ScriptableObject.CreateInstance<SpellPropertyModifierEffectDefinition>();
            SerializedObject serializedEffect = new(effect);
            serializedEffect.FindProperty("property").enumValueIndex = (int)SpellProperty.Radius;
            serializedEffect.FindProperty("value").floatValue = 2f;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            aggregator.Add(effect);
            ActivationCaptureSpell activationSpell = ScriptableObject.CreateInstance<ActivationCaptureSpell>();

            InvokeSpellSelected(activationSpell);

            Assert.AreEqual(2f, activationSpell.ActivationCasterData.PropertyModifiers.GetValue(SpellProperty.Radius));
            Assert.AreEqual(0, activationSpell.ActivationCasterData.RuntimeBehaviors.Count);
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(activationSpell);
        }

        private void InvokeCastSpell(Spell selectedSpell)
        {
            MethodInfo method = typeof(CastController).GetMethod("CastSpell", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(controller, new object[] { selectedSpell });
        }

        private void InvokeSpellSelected(Spell selectedSpell)
        {
            MethodInfo method = typeof(CastController).GetMethod("OnSpellSelected", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(controller, new object[] { selectedSpell });
        }

        private CasterData InvokeCreateCasterData()
        {
            MethodInfo method = typeof(CastController).GetMethod(
                "CreateCasterData",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: System.Type.EmptyTypes,
                modifiers: null);
            return (CasterData)method.Invoke(controller, null);
        }

        private void InvokeLifecycle(string methodName)
        {
            MethodInfo method = typeof(CastController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(controller, null);
        }

        private void SetCurrentTarget(ITargetable target)
        {
            FieldInfo field = typeof(TargetingController).GetField("<CurrentTarget>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(targeting, target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void SetSpellSymbolEntries(SpellSymbolCaster symbolCaster, int symbolId, Spell configuredSpell)
        {
            System.Type entryType = typeof(SpellSymbolEntry);
            object entry = System.Activator.CreateInstance(entryType, nonPublic: true);
            SetField(entry, "symbolIds", new[] { symbolId });
            SetField(entry, "spell", configuredSpell);

            System.Array entries = System.Array.CreateInstance(entryType, 1);
            entries.SetValue(entry, 0);
            SetField(symbolCaster, "spellsBySymbol", entries);
            SetField(symbolCaster, "terminatorSymbolIds", new[] { symbolId });

            MethodInfo awake = typeof(SpellSymbolCaster).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(symbolCaster, null);
        }

        private sealed class TestTargetable : ITargetable
        {
            public TestTargetable(Transform targetPoint)
            {
                TargetPoint = targetPoint;
            }

            public Transform TargetPoint { get; }
        }

        private sealed class CaptureCasterDataSpell : Spell, IAoECapability
        {
            public CasterData LastCasterData { get; private set; }

            public float Radius => 1f;

            public override void OnCast(CasterData casterData)
            {
                LastCasterData = casterData;
            }
        }

        private sealed class DamageRangeSpell : Spell, ICasterDamageRangeProvider
        {
            private static readonly IReadOnlyList<PartialDamageRange> DamageRanges = new[]
            {
                new PartialDamageRange(3f, 7f, DamageType.Magical, DamageElement.Fire)
            };

            public override void OnCast(CasterData casterData)
            {
            }

            public IReadOnlyList<PartialDamageRange> GetDamageRanges(CasterData casterData)
            {
                return DamageRanges;
            }
        }

        private sealed class ActivationCaptureSpell : Spell, IAoECapability
        {
            public CasterData ActivationCasterData { get; private set; }

            public float Radius => 1f;

            public override ISpellActivationHandle OnActivation(CasterData casterData)
            {
                ActivationCasterData = casterData;
                return new TestActivationHandle();
            }

            public override void OnCast(CasterData casterData)
            {
            }
        }

        private sealed class TestActivationHandle : ISpellActivationHandle
        {
            public void Activate(ISpellActivationService service, CasterData casterData)
            {
            }

            public void Deactivate(ISpellActivationService service)
            {
            }

            public bool TryCreateCasterData(CasterData casterData, out CasterData activatedCasterData)
            {
                activatedCasterData = casterData;
                return true;
            }
        }

        private sealed class TestRuntimeBehaviorProvider : MonoBehaviour, IRuntimeSpellBehaviorProvider
        {
            public IRuntimeSpellBehavior Behavior { get; } = new TestRuntimeBehavior();

            public IReadOnlyList<IRuntimeSpellBehavior> CreateRuntimeBehaviors(
                Spell spell,
                GameObject casterObject)
            {
                return new[] { Behavior };
            }
        }

        private sealed class TestRuntimeBehavior : IRuntimeSpellBehavior
        {
        }
    }
}
