using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Progression;
using RPGame.UI.Jobs;
using UnityEditor;
using UnityEngine;

namespace RPGame.UI.Tests
{
    public sealed class PerkTreeBuilderTests
    {
        private readonly List<Object> objectsToDestroy = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = objectsToDestroy.Count - 1; i >= 0; i--)
            {
                if (objectsToDestroy[i] != null)
                {
                    Object.DestroyImmediate(objectsToDestroy[i]);
                }
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void Rebuild_CreatesNodesUnderParentAtPerkPositions()
        {
            PerkDefinition startingPerk = CreatePerk("start", isStartingPerk: true, new Vector2(10f, 20f), "normal");
            PerkDefinition normalPerk = CreatePerk("normal", isStartingPerk: false, new Vector2(120f, -35f));
            PerkDefinition disconnectedPerk = CreatePerk("disconnected", isStartingPerk: false, new Vector2(300f, 300f));
            JobDefinition jobDefinition = CreateJobDefinition("Wizard", startingPerk, normalPerk, disconnectedPerk);
            JobInstance job = new JobInstance(jobDefinition);
            RectTransform parent = CreateRectTransform("Parent");
            PerkTreeConnectionsGraphic connectionsGraphic = parent.gameObject.AddComponent<PerkTreeConnectionsGraphic>();
            RectTransform normalPrefab = CreateRectTransform("NormalPrefab", "NormalMarker");
            List<GameObject> spawnedNodes = new();

            PerkTreeBuilder.Rebuild(
                job,
                parent,
                connectionsGraphic,
                normalPrefab,
                spawnedNodes,
                perk => perk == startingPerk ? PerkUnlockState.Available : PerkUnlockState.Locked,
                perk => false,
                perk => { },
                (perk, position) => { },
                () => { },
                refreshRequested: null);

            Assert.AreEqual(3, spawnedNodes.Count);
            Assert.AreEqual(3, parent.childCount);
            Assert.AreEqual(1, connectionsGraphic.Connections.Count);
            Assert.AreEqual(startingPerk.UIPosition, connectionsGraphic.Connections[0].From);
            Assert.AreEqual(normalPerk.UIPosition, connectionsGraphic.Connections[0].To);
            Assert.AreEqual(PerkTreeConnectionState.Available, connectionsGraphic.Connections[0].State);
            Assert.AreEqual("start", spawnedNodes[0].name);
            Assert.AreEqual("normal", spawnedNodes[1].name);
            Assert.AreSame(parent, spawnedNodes[0].transform.parent);
            Assert.AreSame(parent, spawnedNodes[1].transform.parent);
            Assert.AreSame(parent, spawnedNodes[2].transform.parent);
            Assert.AreEqual(startingPerk.UIPosition, spawnedNodes[0].GetComponent<RectTransform>().anchoredPosition);
            Assert.AreEqual(normalPerk.UIPosition, spawnedNodes[1].GetComponent<RectTransform>().anchoredPosition);
            Assert.AreEqual(disconnectedPerk.UIPosition, spawnedNodes[2].GetComponent<RectTransform>().anchoredPosition);
            Assert.IsNotNull(spawnedNodes[0].transform.Find("NormalMarker"));
            Assert.IsNotNull(spawnedNodes[1].transform.Find("NormalMarker"));
            Assert.IsNotNull(spawnedNodes[0].GetComponent<PerkNodeUI>());
            Assert.IsNotNull(spawnedNodes[1].GetComponent<PerkNodeUI>());
            Assert.IsNotNull(spawnedNodes[2].GetComponent<PerkNodeUI>());
            Assert.IsNotNull(parent.Find("disconnected"));
        }

        [Test]
        public void Rebuild_WithoutStartingPerk_CreatesEveryJobPerkAndItsConnections()
        {
            PerkDefinition firstPerk = CreatePerk("first", isStartingPerk: false, new Vector2(10f, 20f));
            PerkDefinition secondPerk = CreatePerk("second", isStartingPerk: false, new Vector2(120f, -35f), "first");
            JobDefinition jobDefinition = CreateJobDefinition("Wizard", firstPerk, secondPerk);
            JobInstance job = new JobInstance(jobDefinition);
            RectTransform parent = CreateRectTransform("Parent");
            PerkTreeConnectionsGraphic connectionsGraphic = parent.gameObject.AddComponent<PerkTreeConnectionsGraphic>();
            RectTransform normalPrefab = CreateRectTransform("NormalPrefab");
            List<GameObject> spawnedNodes = new();

            PerkTreeBuilder.Rebuild(
                job,
                parent,
                connectionsGraphic,
                normalPrefab,
                spawnedNodes,
                perk => PerkUnlockState.Locked,
                perk => false,
                perk => { },
                (perk, position) => { },
                () => { },
                refreshRequested: null);

            Assert.AreEqual(2, spawnedNodes.Count);
            Assert.IsNotNull(parent.Find("first"));
            Assert.IsNotNull(parent.Find("second"));
            Assert.AreEqual(1, connectionsGraphic.Connections.Count);
            Assert.AreEqual(secondPerk.UIPosition, connectionsGraphic.Connections[0].From);
            Assert.AreEqual(firstPerk.UIPosition, connectionsGraphic.Connections[0].To);
        }

        [Test]
        public void Rebuild_WhenCalledAgain_ClearsPreviouslySpawnedNodes()
        {
            PerkDefinition startingPerk = CreatePerk("start", isStartingPerk: true, Vector2.zero);
            JobDefinition jobDefinition = CreateJobDefinition("Wizard", startingPerk);
            JobInstance job = new JobInstance(jobDefinition);
            RectTransform parent = CreateRectTransform("Parent");
            PerkTreeConnectionsGraphic connectionsGraphic = parent.gameObject.AddComponent<PerkTreeConnectionsGraphic>();
            RectTransform normalPrefab = CreateRectTransform("NormalPrefab");
            List<GameObject> spawnedNodes = new();

            PerkTreeBuilder.Rebuild(
                job,
                parent,
                connectionsGraphic,
                normalPrefab,
                spawnedNodes,
                perk => PerkUnlockState.Locked,
                perk => false,
                perk => { },
                (perk, position) => { },
                () => { },
                refreshRequested: null);
            GameObject firstNode = spawnedNodes[0];
            PerkTreeBuilder.Rebuild(
                job,
                parent,
                connectionsGraphic,
                normalPrefab,
                spawnedNodes,
                perk => PerkUnlockState.Locked,
                perk => false,
                perk => { },
                (perk, position) => { },
                () => { },
                refreshRequested: null);

            Assert.IsTrue(firstNode == null);
            Assert.AreEqual(1, spawnedNodes.Count);
            Assert.AreEqual(1, parent.childCount);
            Assert.AreEqual(0, connectionsGraphic.Connections.Count);
        }

        private PerkDefinition CreatePerk(
            string perkId,
            bool isStartingPerk,
            Vector2 uiPosition,
            params string[] connectedPerkIds)
        {
            PerkDefinition perk = ScriptableObject.CreateInstance<PerkDefinition>();
            objectsToDestroy.Add(perk);

            SerializedObject serializedPerk = new SerializedObject(perk);
            serializedPerk.FindProperty("perkId").stringValue = perkId;
            serializedPerk.FindProperty("displayName").stringValue = perkId;
            serializedPerk.FindProperty("description").stringValue = "Test perk";
            serializedPerk.FindProperty("cost").intValue = 1;
            serializedPerk.FindProperty("isStartingPerk").boolValue = isStartingPerk;
            serializedPerk.FindProperty("uiPosition").vector2Value = uiPosition;

            SerializedProperty connectedIds = serializedPerk.FindProperty("connectedPerkIds");
            connectedIds.arraySize = connectedPerkIds.Length;
            for (int i = 0; i < connectedPerkIds.Length; i++)
            {
                connectedIds.GetArrayElementAtIndex(i).stringValue = connectedPerkIds[i];
            }

            serializedPerk.ApplyModifiedPropertiesWithoutUndo();

            return perk;
        }

        private JobDefinition CreateJobDefinition(string jobId, params PerkDefinition[] perks)
        {
            JobDefinition definition = ScriptableObject.CreateInstance<JobDefinition>();
            objectsToDestroy.Add(definition);

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

        private RectTransform CreateRectTransform(string objectName, string childName = null)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            objectsToDestroy.Add(gameObject);

            if (!string.IsNullOrWhiteSpace(childName))
            {
                GameObject child = new GameObject(childName, typeof(RectTransform));
                child.transform.SetParent(gameObject.transform);
            }

            return gameObject.GetComponent<RectTransform>();
        }
    }
}
