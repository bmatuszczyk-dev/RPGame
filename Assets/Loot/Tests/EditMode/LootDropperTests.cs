using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Inventory;
using RPGame.Inventory.Data;
using UnityEditor;
using UnityEngine;

namespace RPGame.Loot.Tests
{
    public sealed class LootDropperTests
    {
        private readonly List<GameObject> gameObjects = new();
        private readonly List<ItemDefinition> itemDefinitions = new();
        private readonly List<LootTable> lootTables = new();

        [TearDown]
        public void TearDown()
        {
            DestroySpawnedPickups();

            for (int i = 0; i < gameObjects.Count; i++)
            {
                if (gameObjects[i] != null)
                {
                    Object.DestroyImmediate(gameObjects[i]);
                }
            }

            for (int i = 0; i < lootTables.Count; i++)
            {
                Object.DestroyImmediate(lootTables[i]);
            }

            for (int i = 0; i < itemDefinitions.Count; i++)
            {
                Object.DestroyImmediate(itemDefinitions[i]);
            }

            gameObjects.Clear();
            lootTables.Clear();
            itemDefinitions.Clear();
        }

        [Test]
        public void DropLoot_WhenTableIsEmpty_DoesNotCreatePickups()
        {
            LootDropper dropper = CreateDropper(CreateLootTable(), CreatePickupPrefab());
            HashSet<ItemPickup> existingPickups = GetExistingPickups();

            dropper.DropLoot();

            Assert.IsEmpty(GetNewPickups(existingPickups));
        }

        [Test]
        public void DropLoot_WhenTableHasOneResult_CreatesOnePickup()
        {
            ItemDefinition item = CreateItemDefinition();
            LootDropper dropper = CreateDropper(CreateLootTable((item, 1, 1)), CreatePickupPrefab());
            HashSet<ItemPickup> existingPickups = GetExistingPickups();

            dropper.DropLoot();

            Assert.AreEqual(1, GetNewPickups(existingPickups).Count);
        }

        [Test]
        public void DropLoot_WhenTableHasMultipleResults_CreatesOnePickupPerResult()
        {
            LootTable table = CreateLootTable(
                (CreateItemDefinition(), 1, 1),
                (CreateItemDefinition(), 1, 1),
                (CreateItemDefinition(), 1, 1));
            LootDropper dropper = CreateDropper(table, CreatePickupPrefab());
            HashSet<ItemPickup> existingPickups = GetExistingPickups();

            dropper.DropLoot();

            List<ItemPickup> pickups = GetNewPickups(existingPickups);
            Assert.AreEqual(3, pickups.Count);
            Assert.AreNotEqual(pickups[0].transform.position, pickups[1].transform.position);
            Assert.AreNotEqual(pickups[1].transform.position, pickups[2].transform.position);
        }

        [Test]
        public void DropLoot_InitializesPickupWithLootResultData()
        {
            ItemDefinition item = CreateItemDefinition();
            LootDropper dropper = CreateDropper(CreateLootTable((item, 4, 4)), CreatePickupPrefab());
            HashSet<ItemPickup> existingPickups = GetExistingPickups();

            dropper.DropLoot();

            List<ItemPickup> pickups = GetNewPickups(existingPickups);
            Assert.AreEqual(1, pickups.Count);
            Assert.AreSame(item, pickups[0].Item);
            Assert.AreEqual(4, pickups[0].Amount);
        }

        [Test]
        public void DropLoot_WhenCalledTwice_CreatesLootOnlyOnce()
        {
            ItemDefinition item = CreateItemDefinition();
            LootDropper dropper = CreateDropper(CreateLootTable((item, 1, 1)), CreatePickupPrefab());
            HashSet<ItemPickup> existingPickups = GetExistingPickups();

            dropper.DropLoot();
            dropper.DropLoot();

            Assert.AreEqual(1, GetNewPickups(existingPickups).Count);
        }

        [Test]
        public void PooledDropper_WhenRespawned_CanDropLootAgainOnce()
        {
            ItemDefinition item = CreateItemDefinition();
            LootDropper dropper = CreateDropper(CreateLootTable((item, 1, 1)), CreatePickupPrefab());
            HashSet<ItemPickup> existingPickups = GetExistingPickups();

            dropper.ResetForSpawn();
            dropper.DropLoot();
            dropper.DropLoot();
            dropper.ResetForDespawn();
            dropper.ResetForSpawn();
            dropper.DropLoot();

            Assert.AreEqual(2, GetNewPickups(existingPickups).Count);
        }

        [Test]
        public void ResetForDespawn_DoesNotCreatePickups()
        {
            ItemDefinition item = CreateItemDefinition();
            LootDropper dropper = CreateDropper(CreateLootTable((item, 1, 1)), CreatePickupPrefab());
            HashSet<ItemPickup> existingPickups = GetExistingPickups();

            dropper.ResetForDespawn();

            Assert.IsEmpty(GetNewPickups(existingPickups));
        }

        [Test]
        public void DropLoot_WithoutDropOrigin_SpawnsAtDropperPosition()
        {
            ItemDefinition item = CreateItemDefinition();
            LootDropper dropper = CreateDropper(CreateLootTable((item, 1, 1)), CreatePickupPrefab());
            dropper.transform.position = new Vector3(2f, 3f, 4f);
            HashSet<ItemPickup> existingPickups = GetExistingPickups();

            dropper.DropLoot();

            List<ItemPickup> pickups = GetNewPickups(existingPickups);
            Assert.AreEqual(1, pickups.Count);
            Assert.AreEqual(dropper.transform.position, pickups[0].transform.position);
        }

        [Test]
        public void DropLoot_WithDropOrigin_UsesDropOriginPosition()
        {
            ItemDefinition item = CreateItemDefinition();
            Transform dropOrigin = CreateDropOrigin(new Vector3(5f, 6f, 7f));
            LootDropper dropper = CreateDropper(
                CreateLootTable((item, 1, 1)),
                CreatePickupPrefab(),
                dropOrigin);
            dropper.transform.position = new Vector3(2f, 3f, 4f);
            HashSet<ItemPickup> existingPickups = GetExistingPickups();

            dropper.DropLoot();

            List<ItemPickup> pickups = GetNewPickups(existingPickups);
            Assert.AreEqual(1, pickups.Count);
            Assert.AreEqual(dropOrigin.position, pickups[0].transform.position);
        }

        private LootDropper CreateDropper(
            LootTable table,
            ItemPickup pickupPrefab,
            Transform dropOrigin = null)
        {
            GameObject gameObject = new GameObject("LootDropperTests_Dropper");
            gameObjects.Add(gameObject);

            LootDropper dropper = gameObject.AddComponent<LootDropper>();
            SerializedObject serializedDropper = new SerializedObject(dropper);
            serializedDropper.FindProperty("lootTable").objectReferenceValue = table;
            serializedDropper.FindProperty("pickupPrefab").objectReferenceValue = pickupPrefab;
            serializedDropper.FindProperty("dropOrigin").objectReferenceValue = dropOrigin;
            serializedDropper.FindProperty("dropRadius").floatValue = 1f;
            serializedDropper.ApplyModifiedPropertiesWithoutUndo();
            return dropper;
        }

        private ItemPickup CreatePickupPrefab()
        {
            GameObject gameObject = new GameObject("LootDropperTests_PickupPrefab");
            gameObjects.Add(gameObject);
            return gameObject.AddComponent<ItemPickup>();
        }

        private Transform CreateDropOrigin(Vector3 position)
        {
            GameObject gameObject = new GameObject("LootDropperTests_DropOrigin");
            gameObject.transform.position = position;
            gameObjects.Add(gameObject);
            return gameObject.transform;
        }

        private ItemDefinition CreateItemDefinition()
        {
            ItemDefinition itemDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            itemDefinitions.Add(itemDefinition);
            return itemDefinition;
        }

        private LootTable CreateLootTable(params (ItemDefinition item, int minAmount, int maxAmount)[] entries)
        {
            LootTable table = ScriptableObject.CreateInstance<LootTable>();
            lootTables.Add(table);

            SerializedObject serializedTable = new SerializedObject(table);
            SerializedProperty serializedGroups = serializedTable.FindProperty("independentGroups");
            serializedGroups.arraySize = entries.Length > 0 ? 1 : 0;
            if (entries.Length == 0)
            {
                serializedTable.FindProperty("weightedGroups").arraySize = 0;
                serializedTable.ApplyModifiedPropertiesWithoutUndo();
                return table;
            }

            SerializedProperty serializedEntries = serializedGroups
                .GetArrayElementAtIndex(0)
                .FindPropertyRelative("entries");
            serializedEntries.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty entry = serializedEntries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("item").objectReferenceValue = entries[i].item;
                entry.FindPropertyRelative("minAmount").intValue = entries[i].minAmount;
                entry.FindPropertyRelative("maxAmount").intValue = entries[i].maxAmount;
                entry.FindPropertyRelative("chance").floatValue = 1f;
            }

            serializedTable.FindProperty("weightedGroups").arraySize = 0;
            serializedTable.ApplyModifiedPropertiesWithoutUndo();
            return table;
        }

        private static HashSet<ItemPickup> GetExistingPickups()
        {
            return new HashSet<ItemPickup>(Object.FindObjectsByType<ItemPickup>());
        }

        private static List<ItemPickup> GetNewPickups(HashSet<ItemPickup> existingPickups)
        {
            ItemPickup[] pickups = Object.FindObjectsByType<ItemPickup>();
            List<ItemPickup> newPickups = new();
            for (int i = 0; i < pickups.Length; i++)
            {
                if (!existingPickups.Contains(pickups[i]))
                {
                    newPickups.Add(pickups[i]);
                }
            }

            return newPickups;
        }

        private void DestroySpawnedPickups()
        {
            ItemPickup[] pickups = Object.FindObjectsByType<ItemPickup>();
            for (int i = 0; i < pickups.Length; i++)
            {
                GameObject pickupObject = pickups[i].gameObject;
                if (!gameObjects.Contains(pickupObject)
                    && pickupObject.name.StartsWith("LootDropperTests_PickupPrefab"))
                {
                    Object.DestroyImmediate(pickupObject);
                }
            }
        }
    }
}
