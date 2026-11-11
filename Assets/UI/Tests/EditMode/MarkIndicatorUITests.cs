using System;
using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RPGame.UI.Tests
{
    public sealed class MarkIndicatorUITests
    {
        private GameObject root;
        private GameObject enemyUI;
        private TestMarkProgressReader progressReader;
        private Image center;
        private Image left;
        private Image right;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Enemy UI");
            root.SetActive(false);
            progressReader = root.AddComponent<TestMarkProgressReader>();
            enemyUI = new GameObject("Enemy UI Canvas", typeof(RectTransform), typeof(Canvas));
            enemyUI.transform.SetParent(root.transform);
            center = CreatePart("Center");
            left = CreatePart("Left");
            right = CreatePart("Right");

            MarkIndicatorUI indicator = enemyUI.AddComponent<MarkIndicatorUI>();
            SerializedObject serialized = new(indicator);
            serialized.FindProperty("center").objectReferenceValue = center;
            serialized.FindProperty("left").objectReferenceValue = left;
            serialized.FindProperty("right").objectReferenceValue = right;
            serialized.FindProperty("fadeDuration").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InvokeLifecycleMethod(indicator, "Awake");
            root.SetActive(true);
            InvokeLifecycleMethod(indicator, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ProgressChanged_RevealsPartsInMarkProgressOrder()
        {
            progressReader.SetState(1, false);

            Assert.AreEqual(1f, center.color.a);
            Assert.AreEqual(0f, left.color.a);
            Assert.AreEqual(0f, right.color.a);

            progressReader.SetState(2, false);

            Assert.AreEqual(1f, center.color.a);
            Assert.AreEqual(1f, left.color.a);
            Assert.AreEqual(0f, right.color.a);

            progressReader.SetState(0, true);

            Assert.AreEqual(1f, right.color.a);
        }

        [Test]
        public void ProgressChanged_WhenMarkIsConsumed_HidesAllParts()
        {
            progressReader.SetState(3, true);
            progressReader.SetState(0, false);

            AssertPartsHidden();
        }

        private Image CreatePart(string name)
        {
            GameObject part = new(name, typeof(RectTransform), typeof(Image));
            part.transform.SetParent(enemyUI.transform);
            return part.GetComponent<Image>();
        }

        private void AssertPartsHidden()
        {
            Assert.AreEqual(0f, center.color.a);
            Assert.AreEqual(0f, left.color.a);
            Assert.AreEqual(0f, right.color.a);
        }

        private static void InvokeLifecycleMethod(MarkIndicatorUI indicator, string methodName)
        {
            typeof(MarkIndicatorUI).GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(indicator, null);
        }

        private sealed class TestMarkProgressReader : MonoBehaviour, IMarkProgressReader
        {
            public int CurrentUniqueSpellProgress { get; private set; }
            public float ComboWindowExpiresAt { get; private set; } = -1f;
            public bool IsMarkActive { get; private set; }
            public event Action ProgressChanged;
#pragma warning disable CS0067
            public event Action ProgressExpired;
#pragma warning restore CS0067

            public void SetState(int progress, bool isMarkActive)
            {
                CurrentUniqueSpellProgress = progress;
                IsMarkActive = isMarkActive;
                ProgressChanged?.Invoke();
            }
        }
    }
}
