using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Movement;
using RPGame.Enemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace RPGame.Enemies.Tests
{
    public sealed class MovementTests
    {
        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void Start_PassesMoveSpeedToNavMeshAgent()
        {
            Movement movement = CreateMovement(out NavMeshAgent agent);
            SetMoveSpeed(movement, 4.75f);

            InvokeStart(movement);

            Assert.AreEqual(4.75f, agent.speed);
        }

        [Test]
        public void MoveTo_WhenAgentIsNotOnNavMesh_DoesNotThrow()
        {
            Movement movement = CreateMovement(out NavMeshAgent agent);
            InvokeStart(movement);

            Assert.DoesNotThrow(() => movement.MoveTo(new Vector3(2f, 0f, 2f)));
            Assert.DoesNotThrow(movement.Stop);
            Assert.IsFalse(agent.isOnNavMesh);
        }

        [Test]
        public void Start_DisablesAutomaticOffMeshLinkTraversal()
        {
            Movement movement = CreateMovement(out NavMeshAgent agent);

            InvokeStart(movement);

            Assert.IsFalse(agent.autoTraverseOffMeshLink);
        }

        [Test]
        public void ApplyKnockback_WhenLeaping_DoesNotStartKnockback()
        {
            Movement movement = CreateMovement(out _);
            SetIsLeaping(movement, true);

            movement.ApplyKnockback(Vector3.forward, 1f, 0.5f);

            Assert.IsTrue(movement.IsLeaping);
            Assert.IsFalse(GetKnockback(movement).IsActive);
        }

        [Test]
        public void ResetForSpawn_WhenLeaping_ClearsLeapState()
        {
            Movement movement = CreateMovement(out _);
            SetIsLeaping(movement, true);

            movement.ResetForSpawn();

            Assert.IsFalse(movement.IsLeaping);
        }

        [Test]
        public void CancelLeap_WhenTraversingOffMeshLink_ClearsTraversalWithoutNormalCompletion()
        {
            Movement movement = CreateMovement(out _);
            SetIsLeaping(movement, true);
            SetLeapTraversalState(movement, true);

            GetLeap(movement).Cancel();

            Assert.IsFalse(movement.IsLeaping);
            Assert.IsFalse(GetLeapTraversalState(movement));
        }

        [Test]
        public void BlockMovement_WhenLeaping_DoesNotCancelLeap()
        {
            Movement movement = CreateMovement(out _);
            SetTraversalLeaping(movement);

            movement.BlockMovement();

            Assert.IsTrue(movement.IsLeaping);
            Assert.That(GetLeap(movement).State, Is.EqualTo(LeapMovementState.Traversal));
        }

        [Test]
        public void BlockMovement_WhenCombatLeaping_TransitionsToFalling()
        {
            Movement movement = CreateMovement(out _);
            SetCombatLeaping(movement);

            movement.BlockMovement();

            Assert.IsTrue(movement.IsLeaping);
            Assert.That(GetLeap(movement).State, Is.EqualTo(LeapMovementState.CombatFalling));
        }

        [Test]
        public void ApplyKnockback_WhenCombatFalling_DoesNotStartKnockback()
        {
            Movement movement = CreateMovement(out _);
            SetCombatLeaping(movement);
            movement.BlockMovement();

            movement.ApplyKnockback(Vector3.forward, 1f, 0.5f);

            Assert.IsFalse(GetKnockback(movement).IsActive);
        }

        [Test]
        public void BlockMovement_WhenCharging_CancelsCharge()
        {
            Movement movement = CreateMovement(out _);
            SetIsCharging(movement, true);

            movement.BlockMovement();

            Assert.IsFalse(movement.IsCharging);
        }

        [Test]
        public void ApplyKnockback_WhenChargeHasFullResistance_CancelsChargeWithoutKnockback()
        {
            Movement movement = CreateMovement(out _);
            SetIsCharging(movement, true);
            SetChargeKnockbackResistance(movement, 1f);

            movement.ApplyKnockback(Vector3.forward, 5f, 0.5f);

            Assert.IsFalse(movement.IsCharging);
            Assert.IsFalse(GetKnockback(movement).IsActive);
        }

        [Test]
        public void ApplyKnockback_WhenChargeHasNoResistance_CancelsChargeAndUsesFullDistance()
        {
            Movement movement = CreateMovement(out _);
            SetIsCharging(movement, true);
            SetChargeKnockbackResistance(movement, 0f);

            movement.ApplyKnockback(Vector3.forward, 5f, 0.5f);

            Assert.IsFalse(movement.IsCharging);
            Assert.That(GetKnockbackDistance(movement), Is.EqualTo(5f));
        }

        [TestCase(0f, 5f)]
        [TestCase(0.75f, 1.25f)]
        public void ApplyKnockback_UsesBaseKnockbackResistance(float resistance, float expectedDistance)
        {
            Movement movement = CreateMovement(out _);
            movement.SetKnockbackResistance(resistance);

            movement.ApplyKnockback(Vector3.forward, 5f, 0.5f);

            Assert.IsTrue(GetKnockback(movement).IsActive);
            Assert.That(GetKnockbackDistance(movement), Is.EqualTo(expectedDistance));
        }

        [Test]
        public void ApplyKnockback_WithFullBaseResistance_DoesNotStartKnockback()
        {
            Movement movement = CreateMovement(out _);
            movement.SetKnockbackResistance(1f);

            movement.ApplyKnockback(Vector3.forward, 5f, 0.5f);

            Assert.IsFalse(GetKnockback(movement).IsActive);
        }

        [Test]
        public void ApplyKnockback_WithFullBaseResistance_CancelsCharge()
        {
            Movement movement = CreateMovement(out _);
            SetIsCharging(movement, true);
            SetChargeKnockbackResistance(movement, 0f);
            movement.SetKnockbackResistance(1f);

            movement.ApplyKnockback(Vector3.forward, 5f, 0.5f);

            Assert.IsFalse(movement.IsCharging);
            Assert.IsFalse(GetKnockback(movement).IsActive);
        }

        [Test]
        public void ApplyKnockback_WhenCharging_CombinesBaseResistanceAndChargeMultiplier()
        {
            Movement movement = CreateMovement(out _);
            SetIsCharging(movement, true);
            SetChargeKnockbackResistance(movement, 0.8f);
            movement.SetKnockbackResistance(0.75f);

            movement.ApplyKnockback(Vector3.forward, 10f, 0.5f);

            Assert.IsFalse(movement.IsCharging);
            Assert.That(GetKnockbackDistance(movement), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ChargeMovement_DoesNotStoreKnockbackResistance()
        {
            Assert.IsNull(typeof(ChargeMovement).GetField(
                "knockbackResistance",
                BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(typeof(ChargeMovement).GetProperty(
                "KnockbackResistance",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void ChargeModifier_IsRemovedWhenChargeFinishesNaturally()
        {
            Movement movement = CreateMovement(out _);
            SetIsCharging(movement, true);
            SetChargeKnockbackResistance(movement, 0.5f);
            SetChargeMaxDistance(movement, 0f);

            GetCharge(movement).Tick(0f);

            Assert.IsFalse(movement.IsCharging);
            Assert.That(GetKnockbackModifiers(movement).Multiplier, Is.EqualTo(1f));
        }

        [Test]
        public void ChargeModifier_IsRemovedWhenChargeIsCancelled()
        {
            Movement movement = CreateMovement(out _);
            SetIsCharging(movement, true);
            SetChargeKnockbackResistance(movement, 0.5f);

            GetCharge(movement).Cancel();

            Assert.That(GetKnockbackModifiers(movement).Multiplier, Is.EqualTo(1f));
        }

        [Test]
        public void MovementSpeedModifier_DoesNotChangeActiveChargeSpeed()
        {
            Movement movement = CreateMovement(out _);
            SetChargeSpeed(movement, 8f);

            movement.AddMovementSpeedModifier(0.5f);

            Assert.AreEqual(8f, GetChargeSpeed(movement));
        }

        [Test]
        public void ResetForDespawn_WhenCharging_ClearsChargeState()
        {
            Movement movement = CreateMovement(out _);
            SetIsCharging(movement, true);
            SetChargeKnockbackResistance(movement, 0.5f);

            movement.ResetForDespawn();

            Assert.IsFalse(movement.IsCharging);
            Assert.That(GetKnockbackModifiers(movement).Multiplier, Is.EqualTo(1f));
        }

        [Test]
        public void ResetForSpawn_WhenCharging_RemovesChargeModifier()
        {
            Movement movement = CreateMovement(out _);
            SetIsCharging(movement, true);
            SetChargeKnockbackResistance(movement, 0.5f);

            movement.ResetForSpawn();

            Assert.IsFalse(movement.IsCharging);
            Assert.That(GetKnockbackModifiers(movement).Multiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Movement_DoesNotReferenceDetectionPlayerOrCombat()
        {
            bool hasForbiddenField = typeof(Movement)
                .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .Any(field =>
                    field.FieldType == typeof(Detection)
                    || field.FieldType.Namespace == "RPGame.Player"
                    || field.FieldType.Namespace == "RPGame.Combat");

            Assert.IsFalse(hasForbiddenField);
        }

        private Movement CreateMovement(out NavMeshAgent agent)
        {
            GameObject gameObject = CreateObject("Movement");
            agent = gameObject.AddComponent<NavMeshAgent>();
            Movement movement = gameObject.AddComponent<Movement>();
            InvokeAwake(movement);
            return movement;
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetMoveSpeed(Movement movement, float moveSpeed)
        {
            SerializedObject serializedObject = new(movement);
            serializedObject.FindProperty("moveSpeed").floatValue = moveSpeed;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InvokeStart(Movement movement)
        {
            MethodInfo method = typeof(Movement).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(movement, null);
        }

        private static void InvokeAwake(Movement movement)
        {
            MethodInfo method = typeof(Movement).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(movement, null);
        }

        private static void SetIsLeaping(Movement movement, bool isLeaping)
        {
            FieldInfo field = typeof(LeapMovement).GetField("isActive", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(GetLeap(movement), isLeaping);
        }

        private static void SetCombatLeaping(Movement movement)
        {
            SetIsLeaping(movement, true);
            FieldInfo field = typeof(LeapMovement).GetField(
                "<State>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(GetLeap(movement), LeapMovementState.Combat);
        }

        private static void SetTraversalLeaping(Movement movement)
        {
            SetIsLeaping(movement, true);
            FieldInfo field = typeof(LeapMovement).GetField(
                "<State>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(GetLeap(movement), LeapMovementState.Traversal);
        }

        private static LeapMovement GetLeap(Movement movement)
        {
            FieldInfo field = typeof(Movement).GetField("leap", BindingFlags.Instance | BindingFlags.NonPublic);
            return (LeapMovement)field.GetValue(movement);
        }

        private static ChargeMovement GetCharge(Movement movement)
        {
            FieldInfo field = typeof(Movement).GetField("charge", BindingFlags.Instance | BindingFlags.NonPublic);
            return (ChargeMovement)field.GetValue(movement);
        }

        private static KnockbackMovement GetKnockback(Movement movement)
        {
            FieldInfo field = typeof(Movement).GetField("knockback", BindingFlags.Instance | BindingFlags.NonPublic);
            return (KnockbackMovement)field.GetValue(movement);
        }

        private static KnockbackModifiers GetKnockbackModifiers(Movement movement)
        {
            FieldInfo field = typeof(Movement).GetField(
                "knockbackModifiers",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (KnockbackModifiers)field.GetValue(movement);
        }

        private static void SetIsCharging(Movement movement, bool isCharging)
        {
            FieldInfo field = typeof(ChargeMovement).GetField("isActive", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(GetCharge(movement), isCharging);
        }

        private static void SetChargeKnockbackResistance(Movement movement, float resistance)
        {
            int modifierId = GetKnockbackModifiers(movement).AddResistance(resistance);
            FieldInfo field = typeof(Movement).GetField(
                "chargeKnockbackModifierId",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(movement, modifierId);
        }

        private static void SetChargeMaxDistance(Movement movement, float maxDistance)
        {
            FieldInfo field = typeof(ChargeMovement).GetField(
                "maxDistance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(GetCharge(movement), maxDistance);
        }

        private static void SetChargeSpeed(Movement movement, float chargeSpeed)
        {
            FieldInfo field = typeof(ChargeMovement).GetField("speed", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(GetCharge(movement), chargeSpeed);
        }

        private static float GetChargeSpeed(Movement movement)
        {
            FieldInfo field = typeof(ChargeMovement).GetField("speed", BindingFlags.Instance | BindingFlags.NonPublic);
            return (float)field.GetValue(GetCharge(movement));
        }

        private static float GetKnockbackDistance(Movement movement)
        {
            FieldInfo field = typeof(KnockbackMovement).GetField("distance", BindingFlags.Instance | BindingFlags.NonPublic);
            return (float)field.GetValue(GetKnockback(movement));
        }

        private static void SetLeapTraversalState(Movement movement, bool traversesOffMeshLink)
        {
            FieldInfo field = typeof(LeapMovement).GetField(
                "traversesOffMeshLink",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(GetLeap(movement), traversesOffMeshLink);
        }

        private static bool GetLeapTraversalState(Movement movement)
        {
            FieldInfo field = typeof(LeapMovement).GetField(
                "traversesOffMeshLink",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (bool)field.GetValue(GetLeap(movement));
        }

    }
}
