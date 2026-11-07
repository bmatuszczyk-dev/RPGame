using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Statistics.Attributes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Progression.Tests
{
    public sealed class ProgressionTelemetryTests
    {
        private GameObject gameObject;
        private CharacterAttributesConfig attributesConfig;
        private CharacterProgression progression;
        private ProgressionTelemetry telemetry;
        private string sessionFilePath;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("ProgressionTelemetryTests");
            CharacterAttributes attributes = gameObject.AddComponent<CharacterAttributes>();
            progression = gameObject.AddComponent<CharacterProgression>();
            telemetry = gameObject.AddComponent<ProgressionTelemetry>();
            attributesConfig = CreateAttributesConfig();
            SetReference(attributes, "config", attributesConfig);
            SetReference(progression, "attributes", attributes);
            SetValue(progression, "availableXP", 1_000);
            SetReference(telemetry, "progression", progression);
            SetValue(telemetry, "monitoringEnabled", true);
            Invoke(telemetry, "Start");
            sessionFilePath = (string)GetValue(telemetry, "sessionFilePath");
        }

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }

            if (attributesConfig != null)
            {
                Object.DestroyImmediate(attributesConfig);
            }

            if (!string.IsNullOrEmpty(sessionFilePath) && File.Exists(sessionFilePath))
            {
                File.Delete(sessionFilePath);
            }
        }

        [Test]
        public void RecordsCommittedAttributeJobAndPerkDecisionsInOrder()
        {
            JobDefinition job = CreateJob("Wizard");
            PerkDefinition perk = CreatePerk("spark");
            SetJobPerks(job, perk);

            progression.TryBuyAttributePoints(new Dictionary<CharacterAttributeType, int>
            {
                { CharacterAttributeType.Strength, 1 },
                { CharacterAttributeType.Power, 2 }
            });
            progression.Jobs.UnlockJob(job);
            progression.Jobs.AddXPToJob(job.JobId, 10);
            progression.TryUnlockPerk(progression.Jobs.GetJob(job.JobId), perk);

            string[] lines = File.ReadAllLines(sessionFilePath);
            Assert.That(lines[0], Does.Contain("\"eventType\":\"InitialSnapshot\""));
            Assert.That(lines[1], Does.Contain("\"eventType\":\"AttributePointsPurchased\""));
            Assert.That(lines[1], Does.Contain("\"attribute\":\"Strength\""));
            Assert.That(lines[1], Does.Contain("\"attribute\":\"Power\""));
            Assert.That(lines[2], Does.Contain("\"eventType\":\"JobUnlocked\""));
            Assert.That(lines[3], Does.Contain("\"eventType\":\"JobExperienceAllocated\""));
            Assert.That(lines[4], Does.Contain("\"eventType\":\"PerkUnlocked\""));
            Assert.That(lines[4], Does.Contain("\"perkId\":\"spark\""));

            Object.DestroyImmediate(job);
            Object.DestroyImmediate(perk);
        }

        [Test]
        public void DoesNotRecordRejectedTransactions()
        {
            int initialLineCount = File.ReadAllLines(sessionFilePath).Length;

            Assert.IsFalse(progression.Jobs.AddXPToJob("missing", 10));
            Assert.IsFalse(progression.TryBuyAttributePoints(new Dictionary<CharacterAttributeType, int>
            {
                { CharacterAttributeType.Strength, 20 }
            }).Success);

            Assert.AreEqual(initialLineCount, File.ReadAllLines(sessionFilePath).Length);
        }

        [Test]
        public void Destroy_WritesFinalSnapshot()
        {
            Object.DestroyImmediate(gameObject);
            gameObject = null;

            string[] lines = File.ReadAllLines(sessionFilePath);
            Assert.That(lines[lines.Length - 1], Does.Contain("\"eventType\":\"FinalSnapshot\""));
        }

        private static CharacterAttributesConfig CreateAttributesConfig()
        {
            CharacterAttributesConfig config = ScriptableObject.CreateInstance<CharacterAttributesConfig>();
            SerializedObject serialized = new(config);
            serialized.FindProperty("strength").intValue = 10;
            serialized.FindProperty("dexterity").intValue = 10;
            serialized.FindProperty("endurance").intValue = 10;
            serialized.FindProperty("vitality").intValue = 10;
            serialized.FindProperty("intelligence").intValue = 10;
            serialized.FindProperty("power").intValue = 10;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static JobDefinition CreateJob(string jobId)
        {
            JobDefinition definition = ScriptableObject.CreateInstance<JobDefinition>();
            SerializedObject serialized = new(definition);
            serialized.FindProperty("jobId").stringValue = jobId;
            serialized.FindProperty("maxLevel").intValue = 3;
            serialized.FindProperty("baseXP").intValue = 10;
            serialized.FindProperty("xpGrowthRate").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static PerkDefinition CreatePerk(string perkId)
        {
            PerkDefinition definition = ScriptableObject.CreateInstance<PerkDefinition>();
            SerializedObject serialized = new(definition);
            serialized.FindProperty("perkId").stringValue = perkId;
            serialized.FindProperty("cost").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void SetJobPerks(JobDefinition job, PerkDefinition perk)
        {
            SerializedObject serialized = new(job);
            SerializedProperty perks = serialized.FindProperty("jobPerks");
            perks.arraySize = 1;
            perks.GetArrayElementAtIndex(0).objectReferenceValue = perk;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(Object target, string propertyName, object value)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            if (value is bool boolean)
            {
                property.boolValue = boolean;
            }
            else
            {
                property.intValue = (int)value;
            }

            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static object GetValue(object target, string fieldName)
        {
            return target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(target);
        }

        private static void Invoke(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
        }
    }
}
