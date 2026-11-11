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
            Assert.AreEqual(1, indicators.Length);
            Assert.IsFalse(indicators[0].gameObject.activeSelf);
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

        private sealed class TestStatusDefinition : StatusDefinition
        {
            public override ConcurrentStatusPolicy ConcurrentStatusPolicy => ConcurrentStatusPolicy.Independent;

            public override string ToString()
            {
                return "Test Status";
            }
        }
    }
}
