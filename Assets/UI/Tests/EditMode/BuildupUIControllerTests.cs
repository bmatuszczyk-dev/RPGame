using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Buildup;
using RPGame.Core.Statuses;
using RPGame.UI.Buildup;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RPGame.UI.Tests
{
    public sealed class BuildupUIControllerTests
    {
        private readonly List<Object> createdObjects = new();
        private BuildupAggregator buildupAggregator;
        private BuildupUIController controller;
        private Transform entriesRoot;

        [SetUp]
        public void SetUp()
        {
            CreateController();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void BuildupChanged_WithPositiveAmount_CreatesEntry()
        {
            buildupAggregator.AddBuildup(CreateDefinition(), 25f, CreateContext());

            Assert.That(entriesRoot.childCount, Is.EqualTo(1));
            Assert.That(
                GetFill(entriesRoot.GetChild(0).GetComponent<BuildupBarUI>()).color,
                Is.EqualTo(Color.green));
        }

        [Test]
        public void BuildupChanged_ForSameDefinition_UpdatesExistingEntry()
        {
            BuildupDefinition definition = CreateDefinition();
            buildupAggregator.AddBuildup(definition, 25f, CreateContext());
            buildupAggregator.AddBuildup(definition, 25f, CreateContext());

            Assert.That(entriesRoot.childCount, Is.EqualTo(1));
            Assert.That(GetFill(entriesRoot.GetChild(0).GetComponent<BuildupBarUI>()).fillAmount, Is.EqualTo(0.5f));
        }

        [Test]
        public void BuildupChanged_ForDifferentDefinitions_CreatesEntriesForBoth()
        {
            buildupAggregator.AddBuildup(CreateDefinition(), 25f, CreateContext());
            buildupAggregator.AddBuildup(CreateDefinition(), 25f, CreateContext());

            Assert.That(entriesRoot.childCount, Is.EqualTo(2));
        }

        [Test]
        public void BuildupChanged_AtThresholdKeepsEntryAndUsesEffectActiveColor()
        {
            Color effectActiveColor = Color.magenta;
            BuildupDefinition definition = CreateDefinition(effectActiveColor: effectActiveColor);
            buildupAggregator.AddBuildup(definition, 25f, CreateContext());
            buildupAggregator.AddBuildup(definition, 75f, CreateContext());

            Assert.That(entriesRoot.childCount, Is.EqualTo(1));
            Image fill = GetFill(entriesRoot.GetChild(0).GetComponent<BuildupBarUI>());
            Assert.That(fill.fillAmount, Is.EqualTo(1f));
            Assert.That(fill.color, Is.EqualTo(effectActiveColor));
        }

        [Test]
        public void EffectActive_UpdatesProgressAndRemovesEntryAtZero()
        {
            BuildupDefinition definition = CreateDefinition(triggeredStatusDuration: 8f);
            buildupAggregator.AddBuildup(definition, 100f, CreateContext());

            TickBuildup(4f);

            Assert.That(entriesRoot.childCount, Is.EqualTo(1));
            Assert.That(GetFill(entriesRoot.GetChild(0).GetComponent<BuildupBarUI>()).fillAmount, Is.EqualTo(0.5f));

            TickBuildup(4f);

            Assert.That(entriesRoot.childCount, Is.Zero);
        }

        [Test]
        public void BuildupsCleared_RemovesAllEntries()
        {
            buildupAggregator.AddBuildup(CreateDefinition(), 25f, CreateContext());
            buildupAggregator.AddBuildup(CreateDefinition(), 25f, CreateContext());

            buildupAggregator.ClearBuildup();

            Assert.That(entriesRoot.childCount, Is.Zero);
        }

        [Test]
        public void OnEnable_SynchronizesExistingBuildups()
        {
            controller.gameObject.SetActive(false);
            InvokeOnDisable();
            buildupAggregator.AddBuildup(CreateDefinition(), 25f, CreateContext());

            controller.gameObject.SetActive(true);
            InvokeOnEnable();

            Assert.That(entriesRoot.childCount, Is.EqualTo(1));
        }

        [Test]
        public void OnEnable_SynchronizesEffectActiveStateAndColor()
        {
            Color effectActiveColor = Color.magenta;
            BuildupDefinition definition = CreateDefinition(effectActiveColor: effectActiveColor);
            controller.gameObject.SetActive(false);
            InvokeOnDisable();
            buildupAggregator.AddBuildup(definition, 100f, CreateContext());

            controller.gameObject.SetActive(true);
            InvokeOnEnable();

            Image fill = GetFill(entriesRoot.GetChild(0).GetComponent<BuildupBarUI>());
            Assert.That(fill.fillAmount, Is.EqualTo(1f));
            Assert.That(fill.color, Is.EqualTo(effectActiveColor));
        }

        [Test]
        public void ResetForDespawn_RemovesEffectActiveEntry()
        {
            buildupAggregator.AddBuildup(CreateDefinition(), 100f, CreateContext());

            buildupAggregator.ResetForDespawn();

            Assert.That(entriesRoot.childCount, Is.Zero);
        }

        [Test]
        public void ZeroDurationEffect_DoesNotLeaveEntryVisible()
        {
            buildupAggregator.AddBuildup(
                CreateDefinition(triggeredStatusDuration: 0f),
                100f,
                CreateContext());

            Assert.That(entriesRoot.childCount, Is.Zero);
        }

        [Test]
        public void OnDisable_UnsubscribesFromBuildupChanges()
        {
            controller.gameObject.SetActive(false);
            InvokeOnDisable();

            buildupAggregator.AddBuildup(CreateDefinition(), 25f, CreateContext());

            Assert.That(entriesRoot.childCount, Is.Zero);
        }

        private void CreateController()
        {
            GameObject player = CreateObject("Player");
            player.AddComponent<FakeStatusReceiver>();
            buildupAggregator = player.AddComponent<BuildupAggregator>();

            GameObject controllerObject = CreateObject("BuildupContainer");
            controllerObject.SetActive(false);
            controller = controllerObject.AddComponent<BuildupUIController>();
            entriesRoot = CreateObject("EntriesRoot").transform;
            entriesRoot.SetParent(controller.transform);
            BuildupBarUI prefab = CreateBuildupBarPrefab();

            SetPrivateField(controller, "buildupAggregator", buildupAggregator);
            SetPrivateField(controller, "entriesRoot", entriesRoot);
            SetPrivateField(controller, "buildupBarPrefab", prefab);
            controllerObject.SetActive(true);
            InvokeOnEnable();
        }

        private BuildupBarUI CreateBuildupBarPrefab()
        {
            BuildupBarUI prefab = CreateObject("BuildupBar").AddComponent<BuildupBarUI>();
            Image icon = CreateObject("Icon").AddComponent<Image>();
            Image fill = CreateObject("Fill").AddComponent<Image>();
            SetPrivateField(prefab, "icon", icon);
            SetPrivateField(prefab, "fill", fill);
            return prefab;
        }

        private BuildupDefinition CreateDefinition(
            float triggeredStatusDuration = 8f,
            Color? effectActiveColor = null)
        {
            BuildupDefinition definition = ScriptableObject.CreateInstance<BuildupDefinition>();
            createdObjects.Add(definition);
            SerializedObject serializedDefinition = new(definition);
            serializedDefinition.FindProperty("threshold").floatValue = 100f;
            serializedDefinition.FindProperty("fillColor").colorValue = Color.green;
            serializedDefinition.FindProperty("effectActiveColor").colorValue = effectActiveColor ?? Color.red;
            serializedDefinition.FindProperty("triggeredStatusDuration").floatValue = triggeredStatusDuration;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private void TickBuildup(float deltaTime)
        {
            GetPrivateField<BuildupContainer>(buildupAggregator, "buildupContainer").Tick(deltaTime);
        }

        private void InvokeOnEnable()
        {
            InvokeControllerMethod("OnEnable");
        }

        private void InvokeOnDisable()
        {
            InvokeControllerMethod("OnDisable");
        }

        private void InvokeControllerMethod(string methodName)
        {
            typeof(BuildupUIController).GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
        }

        private StatusContext CreateContext()
        {
            return new StatusContext(new StatusSourceId("Test"), null);
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static Image GetFill(BuildupBarUI entry)
        {
            return GetPrivateField<Image>(entry, "fill");
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            return (T)target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(target);
        }

        private sealed class FakeStatusReceiver : MonoBehaviour, IStatusReceiver
        {
            public void ApplyStatus(StatusDefinition status, float duration, StatusContext context) { }
            public bool HasStatus(StatusDefinition status) => false;
            public bool TryConsumeStatus(StatusDefinition status) => false;
        }
    }
}
