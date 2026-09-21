using NUnit.Framework;
using RPGame.Core.Effects;
using RPGame.Core.Spells;
using UnityEditor;
using UnityEngine;

namespace RPGame.Progression.Tests
{
    public sealed class CharacterProgressionRuntimeEffectTests
    {
        private GameObject gameObject;
        private JobDefinition jobDefinition;
        private PerkDefinition perk;
        private RuntimeBehaviorEffectDefinition effect;
        private CharacterProgression progression;
        private EffectAggregator aggregator;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Character Progression Runtime Effect Tests");
            progression = gameObject.AddComponent<CharacterProgression>();
            aggregator = gameObject.AddComponent<EffectAggregator>();
            effect = ScriptableObject.CreateInstance<RuntimeBehaviorEffectDefinition>();
            SetBehavior(effect, new TestRuntimeSpellBehaviorDefinition());
            perk = CreatePerk();
            jobDefinition = CreateJobDefinition();
            progression.Jobs.UnlockJob(jobDefinition);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(perk);
            Object.DestroyImmediate(jobDefinition);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void LockedPerk_DoesNotReachEffectAggregator()
        {
            Assert.IsEmpty(aggregator.Effects);
        }

        [Test]
        public void UnlockedPerk_ReachesEffectAggregatorThroughEffects()
        {
            JobInstance job = progression.Jobs.GetJob(jobDefinition.JobId);
            job.AddExperience(job.GetXPToNextLevel());

            Assert.IsTrue(progression.TryUnlockPerk(job, perk));
            Assert.AreEqual(1, aggregator.Effects.Count);
            Assert.AreSame(effect, aggregator.Effects[0].Definition);
        }

        private PerkDefinition CreatePerk()
        {
            PerkDefinition definition = ScriptableObject.CreateInstance<PerkDefinition>();
            SerializedObject serializedDefinition = new(definition);
            SerializedProperty effects = serializedDefinition.FindProperty("effects");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = effect;
            serializedDefinition.FindProperty("isStartingPerk").boolValue = true;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private JobDefinition CreateJobDefinition()
        {
            JobDefinition definition = ScriptableObject.CreateInstance<JobDefinition>();
            SerializedObject serializedDefinition = new(definition);
            serializedDefinition.FindProperty("jobId").stringValue = "Wizard";
            serializedDefinition.FindProperty("maxLevel").intValue = 3;
            serializedDefinition.FindProperty("baseXP").intValue = 50;
            serializedDefinition.FindProperty("xpGrowthRate").floatValue = 2f;
            SerializedProperty jobPerks = serializedDefinition.FindProperty("jobPerks");
            jobPerks.arraySize = 1;
            jobPerks.GetArrayElementAtIndex(0).objectReferenceValue = perk;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void SetBehavior(
            RuntimeBehaviorEffectDefinition target,
            RuntimeSpellBehaviorDefinition behavior)
        {
            SerializedObject serializedObject = new(target);
            serializedObject.FindProperty("behavior").managedReferenceValue = behavior;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        [System.Serializable]
        private sealed class TestRuntimeSpellBehaviorDefinition : RuntimeSpellBehaviorDefinition
        {
            public override bool TryCreate(
                Spell spell,
                GameObject casterObject,
                out IRuntimeSpellBehavior behavior)
            {
                behavior = new TestRuntimeBehavior();
                return true;
            }
        }

        private sealed class TestRuntimeBehavior : IRuntimeSpellBehavior
        {
        }
    }
}
