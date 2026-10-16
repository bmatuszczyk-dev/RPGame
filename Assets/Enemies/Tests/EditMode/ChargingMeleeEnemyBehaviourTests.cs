using System;
using NUnit.Framework;
using RPGame.Core.Targeting;
using UnityEditor;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class ChargingMeleeEnemyBehaviourTests
    {
        private GameObject enemyObject;
        private GameObject targetObject;
        private ChargingMeleeEnemyBehaviourConfig config;

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(enemyObject);
            UnityEngine.Object.DestroyImmediate(targetObject);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [TestCase(0f)]
        [TestCase(1f)]
        [TestCase(0.25f)]
        public void WindupEnd_PassesKnockbackResistance(float knockbackResistance)
        {
            config = CreateConfig(knockbackResistance);
            enemyObject = new GameObject("Enemy");
            targetObject = new GameObject("Target");
            targetObject.transform.position = new Vector3(4f, 0f, 0f);
            FakeMovement movement = new();
            Charge charge = new(config, movement, enemyObject);
            SelectedTarget target = new(new FakeTargetable(targetObject.transform), targetObject.transform.position);

            charge.Tick(0f, target);
            charge.Tick(0f, target);

            Assert.That(movement.KnockbackResistance, Is.EqualTo(knockbackResistance));
        }

        private static ChargingMeleeEnemyBehaviourConfig CreateConfig(float knockbackResistance)
        {
            ChargingMeleeEnemyBehaviourConfig createdConfig = ScriptableObject.CreateInstance<ChargingMeleeEnemyBehaviourConfig>();
            SerializedObject serializedConfig = new(createdConfig);
            serializedConfig.FindProperty("minTriggerDistance").floatValue = 2f;
            serializedConfig.FindProperty("maxTriggerDistance").floatValue = 6f;
            serializedConfig.FindProperty("triggerChance").floatValue = 1f;
            serializedConfig.FindProperty("triggerCheckInterval").floatValue = 1f;
            serializedConfig.FindProperty("windupDuration").floatValue = 0f;
            serializedConfig.FindProperty("chargeSpeed").floatValue = 8f;
            serializedConfig.FindProperty("maxDistance").floatValue = 6f;
            serializedConfig.FindProperty("knockbackResistance").floatValue = knockbackResistance;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return createdConfig;
        }

        private sealed class FakeMovement : IEnemyMovement
        {
            public Vector3 Position => Vector3.zero;
            public bool IsLeaping => false;
            public bool IsCharging { get; private set; }
            public bool IsMovementBlocked => false;
            public float KnockbackResistance { get; private set; }

            public void FaceTowards(Vector3 position)
            {
            }

            public void MoveTo(Vector3 position)
            {
            }

            public void Stop()
            {
            }

            public bool TryResolvePosition(Vector3 desiredPosition, out Vector3 validPosition)
            {
                validPosition = desiredPosition;
                return true;
            }

            public bool TryLeapTo(Vector3 destination, float speed, float arcHeight)
            {
                return false;
            }

            public bool TryCombatLeapTo(Vector3 destination, float speed, float arcHeight, float fallSpeed, Action<bool> onCompleted) => false;

            public bool TryStartCharge(
                Vector3 destination,
                float speed,
                float maxDistance,
                float knockbackResistance,
                Action<Collider, Vector3> onCollision)
            {
                KnockbackResistance = knockbackResistance;
                IsCharging = true;
                return true;
            }
        }

        private sealed class FakeTargetable : ITargetable
        {
            public FakeTargetable(Transform targetPoint)
            {
                TargetPoint = targetPoint;
            }

            public Transform TargetPoint { get; }
        }
    }
}
