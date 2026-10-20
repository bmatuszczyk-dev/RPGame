using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Targeting;
using RPGame.Player.Targeting;
using TargetingController = RPGame.Player.Targeting.TargetingController;
using UnityEngine;

namespace RPGame.Player.Tests
{
    public sealed class TargetingTests
    {
        private readonly List<GameObject> createdObjects = new();
        private readonly List<ITargetable> targets = new();
        private Camera playerCamera;
        private TargetingController targeting;

        [SetUp]
        public void SetUp()
        {
            ClearTargetRegistry();

            GameObject cameraObject = CreateObject("PlayerCamera", Vector3.zero);
            playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.aspect = 1f;
            playerCamera.transform.rotation = Quaternion.identity;

            GameObject targetingObject = CreateObject("Targeting", Vector3.zero);
            targeting = targetingObject.AddComponent<TargetingController>();
            SetField(targeting, "playerCamera", playerCamera);
            SetField(targeting, "maxTargetDistance", 20f);
            SetField(targeting, "targetingRadius", 0.2f);
            SetField(targeting, "targetSwitchThreshold", 0.03f);
            SetField(targeting, "targetRetentionTime", 0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
            targets.Clear();
            ClearTargetRegistry();
        }

        [Test]
        public void SelectBest_SelectsTargetClosestToViewportCenter()
        {
            ITargetable centeredTarget = CreateTarget("Centered", new Vector3(0.05f, 0f, 8f));
            CreateTarget("Offset", new Vector3(1f, 0f, 8f));

            ITargetable selectedTarget = SelectBest();

            Assert.AreSame(centeredTarget, selectedTarget);
        }

        [Test]
        public void SelectBest_IgnoresTargetBeyondMaxTargetDistance()
        {
            CreateTarget("Far", new Vector3(0f, 0f, 8f));

            ITargetable selectedTarget = SelectBest(maxTargetDistance: 5f);

            Assert.IsNull(selectedTarget);
        }

        [Test]
        public void SelectBest_IgnoresTargetBehindCamera()
        {
            CreateTarget("Behind", new Vector3(0f, 0f, -5f));

            ITargetable selectedTarget = SelectBest();

            Assert.IsNull(selectedTarget);
        }

        [Test]
        public void SelectBest_IgnoresTargetOutsideTargetingRadius()
        {
            CreateTarget("OutsideRadius", new Vector3(2f, 0f, 8f));

            ITargetable selectedTarget = SelectBest(targetingRadius: 0.05f);

            Assert.IsNull(selectedTarget);
        }

        [Test]
        public void SelectBest_ReturnsNullWhenNoValidTargetsExist()
        {
            ITargetable selectedTarget = SelectBest();

            Assert.IsNull(selectedTarget);
        }

        [Test]
        public void SelectBest_ChangesSelectedTargetWhenAnotherTargetBecomesCloserToCenter()
        {
            TestTargetable firstTarget = CreateTarget("First", new Vector3(0.05f, 0f, 8f));
            TestTargetable secondTarget = CreateTarget("Second", new Vector3(1f, 0f, 8f));

            ITargetable selectedTarget = SelectBest();
            Assert.AreSame(firstTarget, selectedTarget);

            firstTarget.TargetPoint.position = new Vector3(1.5f, 0f, 8f);
            secondTarget.TargetPoint.position = Vector3.forward * 8f;

            selectedTarget = SelectBest();

            Assert.AreSame(secondTarget, selectedTarget);
        }

        [Test]
        public void SelectBest_UsesAspectRatioWhenComparingViewportDistance()
        {
            playerCamera.aspect = 16f / 9f;
            CreateTargetAtViewport("HorizontalOffset", new Vector3(0.62f, 0.5f, 8f));
            ITargetable verticalOffset = CreateTargetAtViewport("VerticalOffset", new Vector3(0.5f, 0.68f, 8f));

            ITargetable selectedTarget = SelectBest(targetingRadius: 0.3f);

            Assert.AreSame(verticalOffset, selectedTarget);
        }

        [TestCase(-0.1f)]
        [TestCase(1.1f)]
        public void SelectBest_IgnoresTargetOutsideHorizontalViewportBounds(float viewportX)
        {
            CreateTargetAtViewport("OutsideHorizontalBounds", new Vector3(viewportX, 0.5f, 8f));

            ITargetable selectedTarget = SelectBest(targetingRadius: 1f);

            Assert.IsNull(selectedTarget);
        }

        [TestCase(-0.1f)]
        [TestCase(1.1f)]
        public void SelectBest_IgnoresTargetOutsideVerticalViewportBounds(float viewportY)
        {
            CreateTargetAtViewport("OutsideVerticalBounds", new Vector3(0.5f, viewportY, 8f));

            ITargetable selectedTarget = SelectBest(targetingRadius: 1f);

            Assert.IsNull(selectedTarget);
        }

        [Test]
        public void Targeting_KeepsCurrentTargetWhenCandidateIsOnlySlightlyBetter()
        {
            EnemyTargetable currentTarget = CreateRegisteredTargetAtViewport("Current", new Vector3(0.62f, 0.5f, 8f));
            CreateRegisteredTargetAtViewport("Candidate", new Vector3(0.61f, 0.5f, 8f));
            SetCurrentTarget(currentTarget);

            InvokeTargetingUpdate(0.016f);

            Assert.AreSame(currentTarget, targeting.CurrentTarget);
        }

        [Test]
        public void Targeting_SwitchesTargetWhenCandidateExceedsSwitchThreshold()
        {
            EnemyTargetable currentTarget = CreateRegisteredTargetAtViewport("Current", new Vector3(0.62f, 0.5f, 8f));
            EnemyTargetable candidate = CreateRegisteredTargetAtViewport("Candidate", new Vector3(0.57f, 0.5f, 8f));
            SetCurrentTarget(currentTarget);

            InvokeTargetingUpdate(0.016f);

            Assert.AreSame(candidate, targeting.CurrentTarget);
        }

        [Test]
        public void Targeting_KeepsCurrentTargetDuringRetentionAfterSoftInvalidation()
        {
            EnemyTargetable currentTarget = CreateRegisteredTargetAtViewport("Current", new Vector3(0.75f, 0.5f, 8f));
            SetCurrentTarget(currentTarget);

            InvokeTargetingUpdate(0.25f);

            Assert.AreSame(currentTarget, targeting.CurrentTarget);
        }

        [Test]
        public void Targeting_DoesNotLoseRecoveredTargetBeforeRetentionExpires()
        {
            EnemyTargetable currentTarget = CreateRegisteredTargetAtViewport("Current", new Vector3(0.75f, 0.5f, 8f));
            SetCurrentTarget(currentTarget);

            InvokeTargetingUpdate(0.25f);
            currentTarget.transform.position = playerCamera.ViewportToWorldPoint(new Vector3(0.6f, 0.5f, 8f));
            InvokeTargetingUpdate(0.1f);
            currentTarget.transform.position = playerCamera.ViewportToWorldPoint(new Vector3(0.75f, 0.5f, 8f));
            InvokeTargetingUpdate(0.25f);

            Assert.AreSame(currentTarget, targeting.CurrentTarget);
        }

        [Test]
        public void Targeting_ReplacesCurrentTargetAfterRetentionExpires()
        {
            EnemyTargetable currentTarget = CreateRegisteredTargetAtViewport("Current", new Vector3(0.75f, 0.5f, 8f));
            EnemyTargetable replacement = CreateRegisteredTargetAtViewport("Replacement", new Vector3(0.5f, 0.5f, 8f));
            SetCurrentTarget(currentTarget);

            InvokeTargetingUpdate(0.51f);

            Assert.AreSame(replacement, targeting.CurrentTarget);
        }

        [Test]
        public void Targeting_RemovesCurrentTargetImmediatelyWhenBeyondMaxTargetDistance()
        {
            EnemyTargetable currentTarget = CreateRegisteredTarget("Current", new Vector3(0f, 0f, 30f));
            EnemyTargetable replacement = CreateRegisteredTargetAtViewport("Replacement", new Vector3(0.5f, 0.5f, 8f));
            SetCurrentTarget(currentTarget);

            InvokeTargetingUpdate(0.016f);

            Assert.AreSame(replacement, targeting.CurrentTarget);
        }

        [Test]
        public void Targeting_RemovesCurrentTargetImmediatelyWhenBehindCamera()
        {
            EnemyTargetable currentTarget = CreateRegisteredTarget("Current", new Vector3(0f, 0f, -5f));
            EnemyTargetable replacement = CreateRegisteredTargetAtViewport("Replacement", new Vector3(0.5f, 0.5f, 8f));
            SetCurrentTarget(currentTarget);

            InvokeTargetingUpdate(0.016f);

            Assert.AreSame(replacement, targeting.CurrentTarget);
        }

        [Test]
        public void Targeting_RemovesCurrentTargetImmediatelyWhenDeactivated()
        {
            EnemyTargetable currentTarget = CreateRegisteredTargetAtViewport("Current", new Vector3(0.5f, 0.5f, 8f));
            EnemyTargetable replacement = CreateRegisteredTargetAtViewport("Replacement", new Vector3(0.55f, 0.5f, 8f));
            SetCurrentTarget(currentTarget);

            currentTarget.gameObject.SetActive(false);
            InvokeTargetingUpdate(0.016f);

            Assert.AreSame(replacement, targeting.CurrentTarget);
        }

        [Test]
        public void Targeting_RemovesCurrentTargetImmediatelyWhenDestroyed()
        {
            EnemyTargetable currentTarget = CreateRegisteredTargetAtViewport("Current", new Vector3(0.5f, 0.5f, 8f));
            EnemyTargetable replacement = CreateRegisteredTargetAtViewport("Replacement", new Vector3(0.55f, 0.5f, 8f));
            SetCurrentTarget(currentTarget);

            GameObject currentTargetObject = currentTarget.gameObject;
            createdObjects.Remove(currentTargetObject);
            Object.DestroyImmediate(currentTargetObject);
            InvokeTargetingUpdate(0.016f);

            Assert.AreSame(replacement, targeting.CurrentTarget);
        }

        [Test]
        public void Targeting_WhenSwitchThresholdIsZero_SelectsBetterTarget()
        {
            EnemyTargetable currentTarget = CreateRegisteredTargetAtViewport("Current", new Vector3(0.62f, 0.5f, 8f));
            EnemyTargetable candidate = CreateRegisteredTargetAtViewport("Candidate", new Vector3(0.61f, 0.5f, 8f));
            SetField(targeting, "targetSwitchThreshold", 0f);
            SetCurrentTarget(currentTarget);

            InvokeTargetingUpdate(0.016f);

            Assert.AreSame(candidate, targeting.CurrentTarget);
        }

        [Test]
        public void SetCurrentTarget_WhenTargetChanges_RaisesTargetChanged()
        {
            TestTargetable target = CreateTarget("Target", Vector3.forward);
            ITargetable changedTarget = null;
            int eventCount = 0;
            targeting.TargetChanged += targetable =>
            {
                changedTarget = targetable;
                eventCount++;
            };

            InvokeSetCurrentTarget(target);
            InvokeSetCurrentTarget(target);

            Assert.AreEqual(1, eventCount);
            Assert.AreSame(target, changedTarget);
        }

        [Test]
        public void SetCurrentTarget_WhenTargetIsCleared_RaisesTargetChangedWithNull()
        {
            TestTargetable target = CreateTarget("Target", Vector3.forward);
            ITargetable changedTarget = target;
            targeting.TargetChanged += targetable => changedTarget = targetable;

            InvokeSetCurrentTarget(target);
            InvokeSetCurrentTarget(null);

            Assert.IsNull(changedTarget);
        }

        private ITargetable SelectBest(float maxTargetDistance = 20f, float targetingRadius = 0.25f)
        {
            return TargetSelector.SelectBest(
                targets,
                playerCamera,
                Vector3.zero,
                maxTargetDistance,
                targetingRadius);
        }

        private TestTargetable CreateTargetAtViewport(string objectName, Vector3 viewportPosition)
        {
            return CreateTarget(objectName, playerCamera.ViewportToWorldPoint(viewportPosition));
        }

        private TestTargetable CreateTarget(string objectName, Vector3 position)
        {
            GameObject gameObject = CreateObject(objectName, position);
            TestTargetable targetable = new TestTargetable(gameObject.transform);
            targets.Add(targetable);
            return targetable;
        }

        private EnemyTargetable CreateRegisteredTargetAtViewport(string objectName, Vector3 viewportPosition)
        {
            return CreateRegisteredTarget(objectName, playerCamera.ViewportToWorldPoint(viewportPosition));
        }

        private EnemyTargetable CreateRegisteredTarget(string objectName, Vector3 position)
        {
            GameObject gameObject = CreateObject(objectName, position);
            return gameObject.AddComponent<EnemyTargetable>();
        }

        private GameObject CreateObject(string objectName)
        {
            return CreateObject(objectName, Vector3.zero);
        }

        private GameObject CreateObject(string objectName, Vector3 position)
        {
            GameObject gameObject = new GameObject(objectName);
            gameObject.transform.position = position;
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private void InvokeTargetingUpdate(float deltaTime)
        {
            MethodInfo method = typeof(TargetingController).GetMethod("UpdateCurrentTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(targeting, new object[] { deltaTime });
        }

        private void SetCurrentTarget(ITargetable target)
        {
            FieldInfo field = typeof(TargetingController).GetField("<CurrentTarget>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(targeting, target);
        }

        private void InvokeSetCurrentTarget(ITargetable target)
        {
            MethodInfo method = typeof(TargetingController).GetMethod("SetCurrentTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(targeting, new object[] { target });
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void ClearTargetRegistry()
        {
            MethodInfo method = typeof(TargetRegistry).GetMethod("Clear", BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, null);
        }

        private sealed class TestTargetable : ITargetable
        {
            public TestTargetable(Transform targetPoint)
            {
                TargetPoint = targetPoint;
            }

            public Transform TargetPoint { get; }
        }
    }
}
