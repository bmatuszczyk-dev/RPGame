using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RPGame.UI.Tests
{
    public sealed class StatusPoolUITests
    {
        private GameObject root;
        private Transform container;
        private StatusAggregator statusAggregator;
        private StatusPoolUI statusPool;
        private StatusIndicatorUI indicatorPrefab;
        private TestStatusDefinition definition;
        private Sprite sprite;
        private Texture2D texture;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Enemy");
            root.SetActive(false);
            statusAggregator = root.AddComponent<StatusAggregator>();
            GameObject containerObject = new("Status Pool", typeof(RectTransform));
            containerObject.transform.SetParent(root.transform);
            container = containerObject.transform;
            statusPool = containerObject.AddComponent<StatusPoolUI>();
            indicatorPrefab = CreateIndicatorPrefab();
            definition = CreateDefinition<TestStatusDefinition>();

            SerializedObject poolSerialized = new(statusPool);
            poolSerialized.FindProperty("statusAggregator").objectReferenceValue = statusAggregator;
            poolSerialized.FindProperty("statusIndicatorPrefab").objectReferenceValue = indicatorPrefab;
            poolSerialized.FindProperty("container").objectReferenceValue = container;
            poolSerialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            InvokeAwake(statusAggregator);
            InvokeOnEnable();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(indicatorPrefab.gameObject);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ApplyStatus_CreatesIndicatorWithConfiguredIcon()
        {
            statusAggregator.ApplyStatus(definition, 4f, CreateContext(root));

            StatusIndicatorUI[] indicators = container.GetComponentsInChildren<StatusIndicatorUI>(true);

            Assert.AreEqual(1, indicators.Length);
            Assert.AreEqual(sprite, indicators[0].transform.Find("Icon").GetComponent<Image>().sprite);
            Assert.IsTrue(statusPool.HasIndicators);
        }

        [Test]
        public void ConsumeStatus_RemovesIndicatorImmediately()
        {
            statusAggregator.ApplyStatus(definition, 4f, CreateContext(root));

            Assert.IsTrue(statusAggregator.TryConsumeStatus(definition));

            StatusIndicatorUI[] indicators = container.GetComponentsInChildren<StatusIndicatorUI>(true);
            Assert.AreEqual(0, indicators.Length);
            Assert.IsFalse(statusPool.HasIndicators);
        }

        [Test]
        public void ApplyStatus_IgnoresMarkAndKeepsIndependentInstances()
        {
            GameObject secondSource = new("Second Source");
            MarkStatusDefinition mark = CreateDefinition<MarkStatusDefinition>();
            try
            {
                statusAggregator.ApplyStatus(definition, 4f, CreateContext(root));
                statusAggregator.ApplyStatus(definition, 4f, CreateContext(secondSource));
                statusAggregator.ApplyStatus(mark, 4f, CreateContext(root));

                StatusIndicatorUI[] indicators = container.GetComponentsInChildren<StatusIndicatorUI>(true);

                Assert.AreEqual(2, indicators.Length);
            }
            finally
            {
                Object.DestroyImmediate(mark);
                Object.DestroyImmediate(secondSource);
            }
        }

        [Test]
        public void Refresh_ReapplicationUpdatesFillWithoutAddingIndicator()
        {
            RefreshStatusDefinition refresh = CreateDefinition<RefreshStatusDefinition>();
            try
            {
                statusAggregator.ApplyStatus(refresh, 4f, CreateContext(root));
                TickStatuses(1f);
                statusAggregator.ApplyStatus(refresh, 8f, CreateContext(root));

                Assert.AreEqual(1, GetIndicators().Count);
                Assert.AreEqual(1f, GetFill(statusAggregator.Statuses[0]).fillAmount);
            }
            finally
            {
                Object.DestroyImmediate(refresh);
            }
        }

        [Test]
        public void Accumulate_ReapplicationExtendsIndicatorCountdown()
        {
            AccumulateStatusDefinition accumulate = CreateDefinition<AccumulateStatusDefinition>();
            try
            {
                statusAggregator.ApplyStatus(accumulate, 4f, CreateContext(root));
                TickStatuses(1f);
                statusAggregator.ApplyStatus(accumulate, 4f, CreateContext(root));

                Assert.AreEqual(1, GetIndicators().Count);
                Assert.AreEqual(0.875f, GetFill(statusAggregator.Statuses[0]).fillAmount, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(accumulate);
            }
        }

        [Test]
        public void StackAndRefresh_ReapplicationResetsCountdownWithoutAddingIndicator()
        {
            StackAndRefreshStatusDefinition stackAndRefresh =
                CreateDefinition<StackAndRefreshStatusDefinition>();
            try
            {
                statusAggregator.ApplyStatus(stackAndRefresh, 4f, CreateContext(root));
                TickStatuses(1f);
                statusAggregator.ApplyStatus(stackAndRefresh, 8f, CreateContext(root));

                StatusInstance status = statusAggregator.Statuses[0];
                Assert.AreEqual(1, GetIndicators().Count);
                Assert.AreEqual(2, status.CurrentStacks);
                Assert.AreEqual(1f, GetFill(status).fillAmount);
            }
            finally
            {
                Object.DestroyImmediate(stackAndRefresh);
            }
        }

        [Test]
        public void Ignore_ReapplicationKeepsExistingIndicatorCountdown()
        {
            IgnoreStatusDefinition ignore = CreateDefinition<IgnoreStatusDefinition>();
            try
            {
                statusAggregator.ApplyStatus(ignore, 4f, CreateContext(root));
                TickStatuses(1f);
                statusAggregator.ApplyStatus(ignore, 8f, CreateContext(root));

                Assert.AreEqual(1, GetIndicators().Count);
                Assert.AreEqual(0.75f, GetFill(statusAggregator.Statuses[0]).fillAmount, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(ignore);
            }
        }

        [Test]
        public void Expiration_RemovesIndicatorAndClearsHasIndicators()
        {
            statusAggregator.ApplyStatus(definition, 1f, CreateContext(root));

            TickStatuses(1f);

            Assert.AreEqual(0, GetIndicators().Count);
            Assert.IsFalse(statusPool.HasIndicators);
        }

        [Test]
        public void ResetForDespawnThenSpawn_ClearsIndicatorsBeforeDisplayingNewStatuses()
        {
            statusAggregator.ApplyStatus(definition, 4f, CreateContext(root));

            statusAggregator.ResetForDespawn();
            statusPool.ResetForDespawn();
            statusAggregator.ResetForSpawn();
            statusPool.ResetForSpawn();
            statusAggregator.ApplyStatus(definition, 4f, CreateContext(root));

            Assert.AreEqual(1, GetIndicators().Count);
            Assert.IsTrue(statusPool.HasIndicators);
        }

        [Test]
        public void OnEnable_DisplaysStatusesAddedWhilePoolWasDisabled()
        {
            InvokeOnDisable();
            statusAggregator.ApplyStatus(definition, 4f, CreateContext(root));

            Assert.AreEqual(0, GetIndicators().Count);

            InvokeOnEnable();

            Assert.AreEqual(1, GetIndicators().Count);
            Assert.IsTrue(statusPool.HasIndicators);
        }

        [Test]
        public void ApplyStatus_WithoutIconDoesNotCreateIndicator()
        {
            NoIconStatusDefinition noIcon = ScriptableObject.CreateInstance<NoIconStatusDefinition>();
            try
            {
                statusAggregator.ApplyStatus(noIcon, 4f, CreateContext(root));

                Assert.AreEqual(0, GetIndicators().Count);
                Assert.IsFalse(statusPool.HasIndicators);
            }
            finally
            {
                Object.DestroyImmediate(noIcon);
            }
        }

        [Test]
        public void IndependentInstances_MaintainSeparateIndicatorsAndCountdowns()
        {
            GameObject secondSource = new("Second Source");
            try
            {
                statusAggregator.ApplyStatus(definition, 4f, CreateContext(root));
                StatusInstance firstStatus = statusAggregator.Statuses[0];
                TickStatuses(1f);
                RefreshIndicators();
                statusAggregator.ApplyStatus(definition, 8f, CreateContext(secondSource));
                StatusInstance secondStatus = statusAggregator.Statuses[1];

                Assert.AreEqual(2, GetIndicators().Count);
                Assert.AreEqual(0.75f, GetFill(firstStatus).fillAmount, 0.0001f);
                Assert.AreEqual(1f, GetFill(secondStatus).fillAmount);
            }
            finally
            {
                Object.DestroyImmediate(secondSource);
            }
        }

        private StatusIndicatorUI CreateIndicatorPrefab()
        {
            GameObject prefab = new("Status Indicator");
            prefab.SetActive(false);
            Image fill = CreateImage("Fill", prefab.transform);
            Image icon = CreateImage("Icon", prefab.transform);
            StatusIndicatorUI indicator = prefab.AddComponent<StatusIndicatorUI>();
            SerializedObject serialized = new(indicator);
            serialized.FindProperty("fill").objectReferenceValue = fill;
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return indicator;
        }

        private T CreateDefinition<T>() where T : StatusDefinition
        {
            T statusDefinition = ScriptableObject.CreateInstance<T>();
            if (texture == null)
            {
                texture = new Texture2D(1, 1);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
            }

            SerializedObject serialized = new(statusDefinition);
            serialized.FindProperty("icon").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return statusDefinition;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent);
            return imageObject.GetComponent<Image>();
        }

        private static StatusContext CreateContext(GameObject source)
        {
            return new StatusContext(new StatusSourceId(source.name), source);
        }

        private void InvokeOnEnable()
        {
            typeof(StatusPoolUI)
                .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(statusPool, null);
        }

        private void InvokeOnDisable()
        {
            typeof(StatusPoolUI)
                .GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(statusPool, null);
        }

        private void TickStatuses(float deltaTime)
        {
            StatusContainer statusContainer = (StatusContainer)typeof(StatusAggregator)
                .GetField("statusContainer", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(statusAggregator);
            statusContainer.Tick(deltaTime);
        }

        private Dictionary<StatusInstance, StatusIndicatorUI> GetIndicators()
        {
            return (Dictionary<StatusInstance, StatusIndicatorUI>)typeof(StatusPoolUI)
                .GetField("indicators", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(statusPool);
        }

        private Image GetFill(StatusInstance status)
        {
            StatusIndicatorUI indicator = GetIndicators()[status];
            return indicator.transform.Find("Fill").GetComponent<Image>();
        }

        private void RefreshIndicators()
        {
            foreach (StatusIndicatorUI indicator in GetIndicators().Values)
            {
                typeof(StatusIndicatorUI)
                    .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(indicator, null);
            }
        }

        private static void InvokeAwake(StatusAggregator aggregator)
        {
            typeof(StatusAggregator)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(aggregator, null);
        }

        private sealed class TestStatusDefinition : StatusDefinition
        {
            public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.Independent;

            public override string ToString()
            {
                return "Test Status";
            }
        }

        private sealed class RefreshStatusDefinition : StatusDefinition
        {
            public override string ToString()
            {
                return "Refresh Status";
            }
        }

        private sealed class AccumulateStatusDefinition : StatusDefinition
        {
            public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Accumulate;

            public override string ToString()
            {
                return "Accumulate Status";
            }
        }

        private sealed class StackAndRefreshStatusDefinition : StatusDefinition, IStackableStatus
        {
            public int MaxStacks => 3;
            public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.StackAndRefresh;

            public override string ToString()
            {
                return "Stack And Refresh Status";
            }
        }

        private sealed class IgnoreStatusDefinition : StatusDefinition
        {
            public override ReapplyPolicy ReapplyPolicy => ReapplyPolicy.Ignore;

            public override string ToString()
            {
                return "Ignore Status";
            }
        }

        private sealed class NoIconStatusDefinition : StatusDefinition
        {
            public override string ToString()
            {
                return "No Icon Status";
            }
        }
    }
}
