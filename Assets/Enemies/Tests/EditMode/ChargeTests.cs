using System;
using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Damage;
using RPGame.Core.Targeting;
using UnityEditor;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class ChargeTests
    {
        private readonly List<GameObject> createdObjects = new();
        private readonly List<ScriptableObject> createdAssets = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }

            for (int i = createdAssets.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(createdAssets[i]);
            }
        }

        [Test]
        public void Tick_RequiresTriggerDistanceChanceAndCheckInterval()
        {
            ChargingMeleeEnemyBehaviourConfig config = CreateConfig(triggerChance: 1f, triggerCheckInterval: 0.5f);
            FakeMovement movement = new();
            Charge charge = new(config, movement, CreateObject("Enemy"));
            FakeTargetable targetable = CreateTargetable(new Vector3(8f, 0f, 0f));

            charge.Tick(0.5f, new SelectedTarget(targetable, targetable.TargetPoint.position));

            Assert.AreEqual(ChargeState.Ready, charge.State);

            targetable.TargetPoint.position = new Vector3(4f, 0f, 0f);
            config = CreateConfig(triggerChance: 0f, triggerCheckInterval: 0.5f);
            charge = new Charge(config, movement, CreateObject("EnemyWithoutChance"));

            charge.Tick(0.5f, new SelectedTarget(targetable, targetable.TargetPoint.position));

            Assert.AreEqual(ChargeState.Ready, charge.State);
        }

        [Test]
        public void Tick_TransitionsFromWindupToChargingRecoveryCooldownAndReady()
        {
            ChargingMeleeEnemyBehaviourConfig config = CreateConfig(
                triggerChance: 1f,
                windupDuration: 0.25f,
                recoveryDuration: 0.25f,
                cooldown: 0.25f);
            FakeMovement movement = new();
            Charge charge = new(config, movement, CreateObject("Enemy"));
            FakeTargetable targetable = CreateTargetable(new Vector3(4f, 0f, 0f));
            SelectedTarget target = new(targetable, targetable.TargetPoint.position);

            charge.Tick(0.1f, target);
            Assert.AreEqual(ChargeState.Windup, charge.State);

            charge.Tick(0.25f, target);
            Assert.AreEqual(ChargeState.Charging, charge.State);

            movement.EndCharge();
            charge.Tick(0.1f, target);
            Assert.AreEqual(ChargeState.Recovery, charge.State);

            charge.Tick(0.25f, target);
            Assert.AreEqual(ChargeState.Cooldown, charge.State);

            charge.Tick(0.25f, target);
            Assert.AreEqual(ChargeState.Ready, charge.State);
        }

        [Test]
        public void Windup_LocksChargeDestinationAtItsEnd()
        {
            ChargingMeleeEnemyBehaviourConfig config = CreateConfig(triggerChance: 1f, windupDuration: 0.25f);
            FakeMovement movement = new();
            Charge charge = new(config, movement, CreateObject("Enemy"));
            FakeTargetable targetable = CreateTargetable(new Vector3(4f, 0f, 0f));

            charge.Tick(0.1f, new SelectedTarget(targetable, targetable.TargetPoint.position));
            targetable.TargetPoint.position = new Vector3(6f, 0f, 0f);
            charge.Tick(0.25f, new SelectedTarget(targetable, targetable.TargetPoint.position));
            targetable.TargetPoint.position = new Vector3(10f, 0f, 0f);
            charge.Tick(0.1f, new SelectedTarget(targetable, targetable.TargetPoint.position));

            Assert.AreEqual(new Vector3(6f, 0f, 0f), movement.ChargeDestination);
        }

        [Test]
        public void ChargeCollision_WithCurrentTarget_AppliesDamageOnlyOnce()
        {
            ChargingMeleeEnemyBehaviourConfig config = CreateConfig(triggerChance: 1f, windupDuration: 0f);
            FakeMovement movement = new();
            Charge charge = new(config, movement, CreateObject("Enemy"));
            FakeDamageable damageable = CreateDamageableTarget(new Vector3(4f, 0f, 0f), out FakeTargetable targetable, out Collider collider);
            SelectedTarget target = new(targetable, targetable.TargetPoint.position);

            charge.Tick(0f, target);
            charge.Tick(0f, target);
            movement.NotifyCollision(collider);
            movement.NotifyCollision(collider);

            Assert.AreEqual(1, damageable.DamageCount);
        }

        [Test]
        public void ChargeCollision_WithObstacle_DoesNotDamageTarget()
        {
            ChargingMeleeEnemyBehaviourConfig config = CreateConfig(triggerChance: 1f, windupDuration: 0f);
            FakeMovement movement = new();
            Charge charge = new(config, movement, CreateObject("Enemy"));
            FakeDamageable damageable = CreateDamageableTarget(new Vector3(4f, 0f, 0f), out FakeTargetable targetable, out _);
            GameObject obstacle = CreateObject("Obstacle");
            Collider obstacleCollider = obstacle.AddComponent<BoxCollider>();
            SelectedTarget target = new(targetable, targetable.TargetPoint.position);

            charge.Tick(0f, target);
            charge.Tick(0f, target);
            movement.NotifyCollision(obstacleCollider);
            movement.EndCharge();
            charge.Tick(0.1f, target);

            Assert.AreEqual(0, damageable.DamageCount);
            Assert.AreEqual(ChargeState.Recovery, charge.State);
        }

        [Test]
        public void ChargeEndingAtMaxDistance_DoesNotDamageTarget()
        {
            ChargingMeleeEnemyBehaviourConfig config = CreateConfig(triggerChance: 1f, windupDuration: 0f);
            FakeMovement movement = new();
            Charge charge = new(config, movement, CreateObject("Enemy"));
            FakeDamageable damageable = CreateDamageableTarget(new Vector3(4f, 0f, 0f), out FakeTargetable targetable, out _);
            SelectedTarget target = new(targetable, targetable.TargetPoint.position);

            charge.Tick(0f, target);
            charge.Tick(0f, target);
            movement.EndCharge();
            charge.Tick(0.1f, target);

            Assert.AreEqual(0, damageable.DamageCount);
            Assert.AreEqual(ChargeState.Recovery, charge.State);
        }

        [Test]
        public void Tick_WhenMovementIsBlocked_InterruptsWindupAndChargingIntoRecovery()
        {
            ChargingMeleeEnemyBehaviourConfig config = CreateConfig(triggerChance: 1f, windupDuration: 0.5f);
            FakeMovement movement = new();
            Charge charge = new(config, movement, CreateObject("Enemy"));
            FakeTargetable targetable = CreateTargetable(new Vector3(4f, 0f, 0f));
            SelectedTarget target = new(targetable, targetable.TargetPoint.position);

            charge.Tick(0f, target);
            movement.IsBlocked = true;
            charge.Tick(0.1f, target);

            Assert.AreEqual(ChargeState.Recovery, charge.State);

            movement.IsBlocked = false;
            charge = new Charge(CreateConfig(triggerChance: 1f, windupDuration: 0f), movement, CreateObject("EnemyCharging"));
            charge.Tick(0f, target);
            charge.Tick(0f, target);
            movement.EndCharge();
            charge.Tick(0.1f, target);

            Assert.AreEqual(ChargeState.Recovery, charge.State);
        }

        [Test]
        public void ChargingMeleeBehaviour_RunsMeleeInReadyAndCooldown()
        {
            ChargingMeleeEnemyBehaviourConfig readyConfig = CreateConfig(triggerChance: 0f);
            FakeAttack readyAttack = new();
            ChargingMeleeEnemyBehaviour readyBehaviour = new(
                new FakeDetection(),
                new FakeMovement(),
                readyAttack,
                readyConfig,
                CreateObject("ReadyEnemy"));

            readyBehaviour.Tick(0.1f);

            Assert.AreEqual(1, readyAttack.TickCount);

            ChargingMeleeEnemyBehaviourConfig cooldownConfig = CreateConfig(
                triggerChance: 1f,
                windupDuration: 0f,
                recoveryDuration: 0f,
                cooldown: 1f);
            FakeDetection detection = new();
            FakeTargetable targetable = CreateTargetable(new Vector3(4f, 0f, 0f));
            detection.SetTarget(new SelectedTarget(targetable, targetable.TargetPoint.position));
            FakeMovement movement = new();
            FakeAttack attack = new();
            ChargingMeleeEnemyBehaviour behaviour = new(
                detection,
                movement,
                attack,
                cooldownConfig,
                CreateObject("Enemy"));

            behaviour.Tick(0f);
            behaviour.Tick(0f);
            movement.EndCharge();
            behaviour.Tick(0f);
            behaviour.Tick(0f);

            Assert.AreEqual(1, attack.TickCount);
        }

        private ChargingMeleeEnemyBehaviourConfig CreateConfig(
            float triggerChance = 1f,
            float triggerCheckInterval = 0.1f,
            float windupDuration = 0.1f,
            float recoveryDuration = 0.1f,
            float cooldown = 0.1f)
        {
            ChargingMeleeEnemyBehaviourConfig config = ScriptableObject.CreateInstance<ChargingMeleeEnemyBehaviourConfig>();
            createdAssets.Add(config);
            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("minTriggerDistance").floatValue = 2f;
            serializedConfig.FindProperty("maxTriggerDistance").floatValue = 6f;
            serializedConfig.FindProperty("triggerChance").floatValue = triggerChance;
            serializedConfig.FindProperty("triggerCheckInterval").floatValue = triggerCheckInterval;
            serializedConfig.FindProperty("windupDuration").floatValue = windupDuration;
            serializedConfig.FindProperty("chargeSpeed").floatValue = 8f;
            serializedConfig.FindProperty("maxDistance").floatValue = 6f;
            serializedConfig.FindProperty("recoveryDuration").floatValue = recoveryDuration;
            serializedConfig.FindProperty("cooldown").floatValue = cooldown;
            serializedConfig.FindProperty("knockbackResistance").floatValue = 0.5f;
            SerializedProperty chargeDamage = serializedConfig.FindProperty("chargeDamage");
            chargeDamage.arraySize = 1;
            chargeDamage.GetArrayElementAtIndex(0).FindPropertyRelative("minDamage").floatValue = 5f;
            chargeDamage.GetArrayElementAtIndex(0).FindPropertyRelative("maxDamage").floatValue = 5f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private FakeTargetable CreateTargetable(Vector3 position)
        {
            GameObject targetObject = CreateObject("Target");
            targetObject.transform.position = position;
            return new FakeTargetable(targetObject.transform);
        }

        private FakeDamageable CreateDamageableTarget(
            Vector3 position,
            out FakeTargetable targetable,
            out Collider collider)
        {
            GameObject targetObject = CreateObject("DamageableTarget");
            targetObject.transform.position = position;
            collider = targetObject.AddComponent<BoxCollider>();
            targetable = new FakeTargetable(targetObject.transform);
            return targetObject.AddComponent<FakeDamageable>();
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class FakeMovement : IEnemyMovement
        {
            private Action<Collider, Vector3> chargeCollisionHandler;

            public Vector3 Position { get; set; }
            public bool IsLeaping => false;
            public bool IsCharging { get; private set; }
            public bool IsMovementBlocked => IsBlocked;
            public bool IsBlocked { get; set; }
            public Vector3 ChargeDestination { get; private set; }

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
                ChargeDestination = destination;
                chargeCollisionHandler = onCollision;
                IsCharging = true;
                return true;
            }

            public void EndCharge()
            {
                IsCharging = false;
            }

            public void NotifyCollision(Collider collider)
            {
                chargeCollisionHandler?.Invoke(collider, collider.transform.position);
            }
        }

        private sealed class FakeDetection : IEnemyDetection
        {
            private SelectedTarget target;
            private bool hasTarget;

            public bool TryGetTarget(out SelectedTarget target)
            {
                target = this.target;
                return hasTarget;
            }

            public void SetTarget(SelectedTarget target)
            {
                this.target = target;
                hasTarget = true;
            }
        }

        private sealed class FakeAttack : IEnemyAttack
        {
            public float Range => 1f;
            public int TickCount { get; private set; }

            public void Tick(float deltaTime)
            {
                TickCount++;
            }

            public bool IsInRange(SelectedTarget target)
            {
                return false;
            }

            public bool TryAttack(SelectedTarget target)
            {
                return false;
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

        private sealed class FakeDamageable : MonoBehaviour, IDamageable
        {
            public int DamageCount { get; private set; }
            public bool CanReceiveDamage => true;

            public DamageResult ApplyDamage(DamageData data)
            {
                DamageCount++;
                return DamageResult.Applied(data, data.Amount, 10f, 10f, false);
            }
        }
    }
}
