using NUnit.Framework;
using RPGame.Core.Effects;
using RPGame.Progression;
using UnityEditor;
using UnityEngine;

namespace RPGame.Progression.Tests
{
    public sealed class PerkProgressionTests
    {
        private JobDefinition jobDefinition;
        private PerkDefinition startingPerk;
        private PerkDefinition connectedPerk;
        private PerkDefinition outgoingConnectedPerk;
        private PerkDefinition isolatedPerk;
        private PerkDefinition foreignPerk;
        private StatEffectDefinition manaRegenerationEffect;
        private PerkProgression service;

        [SetUp]
        public void SetUp()
        {
            manaRegenerationEffect = CreateStatEffect(EffectStat.ManaRegeneration, EffectModifierType.Percent, 0.05f);
            startingPerk = CreatePerkDefinition("start", isStartingPerk: true, "outgoingConnected");
            SetPerkEffects(startingPerk, manaRegenerationEffect);
            connectedPerk = CreatePerkDefinition("connected", isStartingPerk: false, "start");
            outgoingConnectedPerk = CreatePerkDefinition("outgoingConnected", isStartingPerk: false);
            isolatedPerk = CreatePerkDefinition("isolated", isStartingPerk: false);
            foreignPerk = CreatePerkDefinition("foreign", isStartingPerk: true);
            jobDefinition = CreateJobDefinition("Wizard", startingPerk, connectedPerk, outgoingConnectedPerk, isolatedPerk);
            service = new PerkProgression();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(jobDefinition);
            Object.DestroyImmediate(startingPerk);
            Object.DestroyImmediate(connectedPerk);
            Object.DestroyImmediate(outgoingConnectedPerk);
            Object.DestroyImmediate(isolatedPerk);
            Object.DestroyImmediate(foreignPerk);
            Object.DestroyImmediate(manaRegenerationEffect);
        }

        [Test]
        public void GetUnlockState_WhenStartingPerkHasEnoughJobPoints_ReturnsAvailable()
        {
            JobInstance job = CreateJob(jobPoints: 1);

            PerkUnlockState state = service.GetUnlockState(job, startingPerk);

            Assert.AreEqual(PerkUnlockState.Available, state);
        }

        [Test]
        public void GetUnlockState_WhenUnlockedPerkListsThisPerkAsNeighbor_ReturnsAvailable()
        {
            JobInstance job = CreateJob(jobPoints: 2);
            service.TryUnlockPerk(job, startingPerk);

            PerkUnlockState state = service.GetUnlockState(job, outgoingConnectedPerk);

            Assert.AreEqual(PerkUnlockState.Available, state);
        }

        [Test]
        public void TryUnlockPerk_WhenAvailable_SpendsJobPointStoresPerkAndRaisesEvent()
        {
            JobInstance job = CreateJob(jobPoints: 1);
            JobInstance eventJob = null;
            PerkDefinition eventPerk = null;
            service.PerkUnlocked += (unlockedJob, unlockedPerk) =>
            {
                eventJob = unlockedJob;
                eventPerk = unlockedPerk;
            };

            bool unlocked = service.TryUnlockPerk(job, startingPerk);

            Assert.IsTrue(unlocked);
            Assert.AreEqual(0, job.JobPoints);
            CollectionAssert.Contains(job.UnlockedPerkIds, startingPerk.PerkId);
            Assert.AreSame(job, eventJob);
            Assert.AreSame(startingPerk, eventPerk);
        }

        [Test]
        public void GetUnlockState_WhenConnectedPerkNeighborIsUnlocked_ReturnsAvailable()
        {
            JobInstance job = CreateJob(jobPoints: 2);
            service.TryUnlockPerk(job, startingPerk);

            PerkUnlockState state = service.GetUnlockState(job, connectedPerk);

            Assert.AreEqual(PerkUnlockState.Available, state);
        }

        [Test]
        public void GetUnlockState_WhenPerkHasNoPredecessor_ReturnsAvailable()
        {
            JobInstance job = CreateJob(jobPoints: 1);

            PerkUnlockState state = service.GetUnlockState(job, isolatedPerk);

            Assert.AreEqual(PerkUnlockState.Available, state);
        }

        [Test]
        public void GetUnlockState_WhenPredecessorIsLocked_ReturnsLocked()
        {
            JobInstance job = CreateJob(jobPoints: 1);

            PerkUnlockState state = service.GetUnlockState(job, outgoingConnectedPerk);

            Assert.AreEqual(PerkUnlockState.Locked, state);
        }

        [Test]
        public void TryUnlockPerk_WhenAlreadyUnlocked_ReturnsFalse()
        {
            JobInstance job = CreateJob(jobPoints: 2);

            bool firstUnlock = service.TryUnlockPerk(job, startingPerk);
            bool secondUnlock = service.TryUnlockPerk(job, startingPerk);

            Assert.IsTrue(firstUnlock);
            Assert.IsFalse(secondUnlock);
            Assert.AreEqual(1, job.JobPoints);
        }

        [Test]
        public void GetUnlockState_WhenJobDoesNotHavePerk_ReturnsLocked()
        {
            JobInstance job = CreateJob(jobPoints: 1);

            PerkUnlockState state = service.GetUnlockState(job, foreignPerk);

            Assert.AreEqual(PerkUnlockState.Locked, state);
        }

        [Test]
        public void GetUnlockState_WhenJobPointsAreInsufficient_ReturnsLocked()
        {
            JobInstance job = CreateJob(jobPoints: 0);

            PerkUnlockState state = service.GetUnlockState(job, startingPerk);

            Assert.AreEqual(PerkUnlockState.Locked, state);
        }

        [Test]
        public void CreateEffectContainer_WhenPerkIsUnlocked_ContainsUnlockedPerkEffects()
        {
            JobInstance job = CreateJob(jobPoints: 1);
            service.TryUnlockPerk(job, startingPerk);

            PermanentEffectContainer container = service.CreateEffectContainer(job);

            float value = container.GetEffectValue(EffectStat.ManaRegeneration, EffectModifierType.Percent);
            Assert.AreEqual(0.05f, value, 0.0001f);
        }

        [Test]
        public void CreateEffectContainer_WhenPerkIsLocked_DoesNotContainLockedPerkEffects()
        {
            JobInstance job = CreateJob(jobPoints: 1);

            PermanentEffectContainer container = service.CreateEffectContainer(job);

            float value = container.GetEffectValue(EffectStat.ManaRegeneration, EffectModifierType.Percent);
            Assert.AreEqual(0f, value, 0.0001f);
        }

        private JobInstance CreateJob(int jobPoints)
        {
            return new JobInstance(jobDefinition, currentLevel: 1, currentXP: 0, totalInvestedXP: 0, jobPoints: jobPoints);
        }

        private static JobDefinition CreateJobDefinition(string jobId, params PerkDefinition[] perks)
        {
            JobDefinition definition = ScriptableObject.CreateInstance<JobDefinition>();
            SerializedObject serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("jobId").stringValue = jobId;
            serializedDefinition.FindProperty("displayName").stringValue = jobId;
            serializedDefinition.FindProperty("description").stringValue = "Test job";
            serializedDefinition.FindProperty("tier").enumValueIndex = (int)JobTier.Basic;
            serializedDefinition.FindProperty("maxLevel").intValue = 3;
            serializedDefinition.FindProperty("baseXP").intValue = 50;
            serializedDefinition.FindProperty("xpGrowthRate").floatValue = 2f;

            SerializedProperty jobPerks = serializedDefinition.FindProperty("jobPerks");
            jobPerks.arraySize = perks.Length;
            for (int i = 0; i < perks.Length; i++)
            {
                jobPerks.GetArrayElementAtIndex(i).objectReferenceValue = perks[i];
            }

            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static PerkDefinition CreatePerkDefinition(
            string perkId,
            bool isStartingPerk,
            params string[] connectedPerkIds)
        {
            PerkDefinition definition = ScriptableObject.CreateInstance<PerkDefinition>();
            SerializedObject serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("perkId").stringValue = perkId;
            serializedDefinition.FindProperty("displayName").stringValue = perkId;
            serializedDefinition.FindProperty("description").stringValue = "Test perk";
            serializedDefinition.FindProperty("cost").intValue = 1;
            serializedDefinition.FindProperty("isStartingPerk").boolValue = isStartingPerk;

            SerializedProperty connectedIds = serializedDefinition.FindProperty("connectedPerkIds");
            connectedIds.arraySize = connectedPerkIds.Length;
            for (int i = 0; i < connectedPerkIds.Length; i++)
            {
                connectedIds.GetArrayElementAtIndex(i).stringValue = connectedPerkIds[i];
            }

            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static StatEffectDefinition CreateStatEffect(
            EffectStat stat,
            EffectModifierType modifierType,
            float value)
        {
            StatEffectDefinition definition = ScriptableObject.CreateInstance<StatEffectDefinition>();
            SerializedObject serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("stat").enumValueIndex = (int)stat;
            serializedDefinition.FindProperty("modifierType").enumValueIndex = (int)modifierType;
            serializedDefinition.FindProperty("value").floatValue = value;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void SetPerkEffects(PerkDefinition perk, params PassiveEffectDefinition[] effects)
        {
            SerializedObject serializedPerk = new SerializedObject(perk);
            SerializedProperty perkEffects = serializedPerk.FindProperty("effects");
            perkEffects.arraySize = effects.Length;
            for (int i = 0; i < effects.Length; i++)
            {
                perkEffects.GetArrayElementAtIndex(i).objectReferenceValue = effects[i];
            }

            serializedPerk.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
