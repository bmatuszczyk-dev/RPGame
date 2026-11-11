using NUnit.Framework;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RPGame.UI.Tests
{
    public sealed class StatusIndicatorUITests
    {
        private GameObject root;
        private StatusIndicatorUI indicator;
        private Image fill;
        private Image icon;
        private TestStatusDefinition definition;
        private Sprite sprite;
        private Texture2D texture;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Status Indicator");
            fill = CreateImage("Fill");
            icon = CreateImage("Icon");
            indicator = root.AddComponent<StatusIndicatorUI>();
            SerializedObject serialized = new(indicator);
            serialized.FindProperty("fill").objectReferenceValue = fill;
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            texture = new Texture2D(1, 1);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
            definition = ScriptableObject.CreateInstance<TestStatusDefinition>();
            SerializedObject definitionSerialized = new(definition);
            definitionSerialized.FindProperty("icon").objectReferenceValue = sprite;
            definitionSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Initialize_AssignsIconAndStartsWithFullRadialFill()
        {
            StatusInstance status = CreateStatus(4f);

            indicator.Initialize(status);

            Assert.AreEqual(sprite, icon.sprite);
            Assert.AreEqual(Image.Type.Filled, fill.type);
            Assert.AreEqual(Image.FillMethod.Radial360, fill.fillMethod);
            Assert.AreEqual(1f, fill.fillAmount);
            Assert.IsFalse(fill.raycastTarget);
            Assert.IsFalse(icon.raycastTarget);
        }

        [Test]
        public void Refresh_UsesRemainingDurationAfterStatusChanges()
        {
            StatusInstance status = CreateStatus(4f);
            indicator.Initialize(status);
            status.Tick(1f, new StatusTarget(null, null, null));

            indicator.Refresh(status);

            Assert.AreEqual(0.75f, fill.fillAmount, 0.0001f);
        }

        [Test]
        public void Initialize_WithZeroDurationStatus_SetsEmptyFill()
        {
            indicator.Initialize(CreateStatus(0f));

            Assert.AreEqual(0f, fill.fillAmount);
        }

        private StatusInstance CreateStatus(float duration)
        {
            return new StatusInstance(
                definition,
                duration,
                new StatusContext(new StatusSourceId("UI Test"), root));
        }

        private Image CreateImage(string name)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(root.transform);
            return imageObject.GetComponent<Image>();
        }

        private sealed class TestStatusDefinition : StatusDefinition
        {
            public override string ToString()
            {
                return "Test Status";
            }
        }
    }
}
