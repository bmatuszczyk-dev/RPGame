using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Damage;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class UniversalPerkMechanicsTests
    {
        private GameObject source;
        private GameObject otherSource;
        private GameObject target;
        private EffectAggregator sourceEffects;
        private StatusAggregator statuses;
        private MarkStatusDefinition mark;
        private SuppressionStatusDefinition suppression;
        private ManaSurgeStatusDefinition manaSurge;

        [SetUp]
        public void SetUp()
        {
            source = new GameObject("Perk Source");
            sourceEffects = source.AddComponent<EffectAggregator>();
            otherSource = new GameObject("Other Perk Source");
            target = new GameObject("Perk Target");
            statuses = target.AddComponent<StatusAggregator>();
            InvokeAwake(statuses);
            mark = ScriptableObject.CreateInstance<MarkStatusDefinition>();
            suppression = ScriptableObject.CreateInstance<SuppressionStatusDefinition>();
            manaSurge = ScriptableObject.CreateInstance<ManaSurgeStatusDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(mark);
            Object.DestroyImmediate(suppression);
            Object.DestroyImmediate(manaSurge);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(otherSource);
            Object.DestroyImmediate(source);
        }

        [Test]
        public void ArcaneTempo_ExtendsComboWindowToSixSeconds()
        {
            float time = 0f;
            sourceEffects.Add(CreateStatEffect(EffectStat.MarkComboWindow, 0.2f));
            MarkProgressTracker tracker = new(statuses, mark, () => time);
            StatusContext context = CreateContext(source);

            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            time = 5.5f;
            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);
            tracker.RecordSuccessfulSpell(new SpellId("fire"), context);

            Assert.IsTrue(statuses.HasStatus(mark));
        }

        [Test]
        public void MarkProgress_ExpiresAfterBaseWindowAndIsolatedPerCaster()
        {
            float time = 0f;
            MarkProgressTracker first = new(statuses, mark, () => time);
            MarkProgressTracker second = new(statuses, mark, () => time);
            StatusContext firstContext = CreateContext(source);
            StatusContext secondContext = CreateContext(otherSource);

            first.RecordSuccessfulSpell(new SpellId("wave"), firstContext);
            second.RecordSuccessfulSpell(new SpellId("orbit"), secondContext);
            second.RecordSuccessfulSpell(new SpellId("fire"), secondContext);

            Assert.IsFalse(statuses.HasStatus(mark));

            time = 5.1f;
            first.RecordSuccessfulSpell(new SpellId("fire"), firstContext);
            first.RecordSuccessfulSpell(new SpellId("lightning"), firstContext);

            Assert.IsFalse(statuses.HasStatus(mark));
        }

        [Test]
        public void MarkProgressReceiver_DoesNotShareProgressBetweenCasters()
        {
            MarkProgressReceiver receiver = target.AddComponent<MarkProgressReceiver>();
            SerializedObject serialized = new(receiver);
            serialized.FindProperty("markDefinition").objectReferenceValue = mark;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            receiver.RecordSuccessfulSpell(new SpellId("wave"), CreateContext(source));
            receiver.RecordSuccessfulSpell(new SpellId("orbit"), CreateContext(otherSource));
            receiver.RecordSuccessfulSpell(new SpellId("fire"), CreateContext(otherSource));

            Assert.IsFalse(statuses.HasStatus(mark));
        }

        [Test]
        public void ExtendedMark_AppliesSixSecondDuration()
        {
            sourceEffects.Add(CreateStatEffect(EffectStat.MarkDuration, 0.2f));
            MarkProgressTracker tracker = new(statuses, mark, () => 0f);
            StatusContext context = CreateContext(source);

            tracker.RecordSuccessfulSpell(new SpellId("wave"), context);
            tracker.RecordSuccessfulSpell(new SpellId("orbit"), context);
            tracker.RecordSuccessfulSpell(new SpellId("fire"), context);

            Assert.AreEqual(6f, statuses.Statuses[0].Duration, 0.0001f);
        }

        [Test]
        public void LingeringEffects_ExtendsOnlyNegativeStatusesAndRefreshesFromBaseDuration()
        {
            sourceEffects.Add(CreateStatEffect(EffectStat.NegativeStatusDuration, 0.15f));
            StatusContext context = CreateContext(source);

            statuses.ApplyStatus(suppression, 2f, context);
            Assert.AreEqual(2.3f, statuses.Statuses[0].Duration, 0.0001f);

            statuses.ApplyStatus(suppression, 2f, context);
            Assert.AreEqual(2.3f, statuses.Statuses[0].Duration, 0.0001f);

            statuses.ClearStatuses();
            statuses.ApplyStatus(mark, 2f, context);
            Assert.AreEqual(2f, statuses.Statuses[0].Duration, 0.0001f);

            statuses.ClearStatuses();
            statuses.ApplyStatus(manaSurge, 2f, context);
            Assert.AreEqual(2f, statuses.Statuses[0].Duration, 0.0001f);

            statuses.ClearStatuses();
            statuses.ApplyStatus(suppression, 2f, CreateContext(otherSource));
            Assert.AreEqual(2f, statuses.Statuses[0].Duration, 0.0001f);
        }

        [Test]
        public void ArcanePowerAndBurningPotency_ApplyOncePerDamageEvent()
        {
            sourceEffects.Add(CreateStatEffect(EffectStat.SpellDamage, 0.1f));
            sourceEffects.Add(CreateStatEffect(EffectStat.PeriodicSpellDamage, 0.2f));
            PartialDamage[] parts = { new(100f, DamageType.Magical, DamageElement.Fire) };

            DamageData direct = new(parts, source);
            DamageData periodic = new(parts, source, DamageApplicationType.Periodic);

            Assert.AreEqual(110f, direct.Amount, 0.0001f);
            Assert.AreEqual(132f, periodic.Amount, 0.0001f);
            Assert.AreEqual(110f, direct.WithParts(direct.Parts).Amount, 0.0001f);
            Assert.AreEqual(100f, new DamageData(parts, otherSource).Amount, 0.0001f);
        }

        [Test]
        public void SourceEffectValues_ApplyFlatBeforePercent()
        {
            sourceEffects.Add(CreateStatEffect(EffectStat.SpellDamage, EffectModifierType.Flat, 10f));
            sourceEffects.Add(CreateStatEffect(EffectStat.SpellDamage, EffectModifierType.Percent, 0.1f));
            PartialDamage[] parts = { new(100f, DamageType.Magical, DamageElement.Fire) };

            Assert.AreEqual(121f, new DamageData(parts, source).Amount, 0.0001f);
        }

        private static StatusContext CreateContext(GameObject caster)
        {
            return new StatusContext(new StatusSourceId("Perk"), caster);
        }

        private static StatEffectDefinition CreateStatEffect(EffectStat stat, float percent)
        {
            return CreateStatEffect(stat, EffectModifierType.Percent, percent);
        }

        private static StatEffectDefinition CreateStatEffect(
            EffectStat stat,
            EffectModifierType modifierType,
            float value)
        {
            StatEffectDefinition effect = ScriptableObject.CreateInstance<StatEffectDefinition>();
            SerializedObject serialized = new(effect);
            serialized.FindProperty("stat").enumValueIndex = (int)stat;
            serialized.FindProperty("modifierType").enumValueIndex = (int)modifierType;
            serialized.FindProperty("value").floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return effect;
        }

        private static void InvokeAwake(StatusAggregator targetStatuses)
        {
            typeof(StatusAggregator)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(targetStatuses, null);
        }
    }
}
