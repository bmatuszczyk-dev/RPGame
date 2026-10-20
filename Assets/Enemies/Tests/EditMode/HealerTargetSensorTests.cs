using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class HealerTargetSensorTests
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
        public void TriggerEnter_AddsEnemyTarget()
        {
            HealerTargetSensor sensor = CreateSensor(out _);
            Collider collider = CreateEnemy("Ally").AddComponent<BoxCollider>();

            InvokeTrigger(sensor, "OnTriggerEnter", collider);

            Assert.AreEqual(1, GetRegisteredTargetCount(sensor));
        }

        [Test]
        public void TriggerExit_RemovesEnemyTarget()
        {
            HealerTargetSensor sensor = CreateSensor(out _);
            Collider collider = CreateEnemy("Ally").AddComponent<BoxCollider>();
            InvokeTrigger(sensor, "OnTriggerEnter", collider);

            InvokeTrigger(sensor, "OnTriggerExit", collider);

            Assert.AreEqual(0, GetRegisteredTargetCount(sensor));
        }

        [Test]
        public void TriggerEnter_IgnoresSensorOwner()
        {
            HealerTargetSensor sensor = CreateSensor(out GameObject owner);
            Collider collider = owner.AddComponent<BoxCollider>();

            InvokeTrigger(sensor, "OnTriggerEnter", collider);

            Assert.AreEqual(0, GetRegisteredTargetCount(sensor));
        }

        [Test]
        public void MultipleColliders_KeepTargetUntilLastExit()
        {
            HealerTargetSensor sensor = CreateSensor(out _);
            GameObject ally = CreateEnemy("Ally");
            Collider firstCollider = ally.AddComponent<BoxCollider>();
            Collider secondCollider = ally.AddComponent<SphereCollider>();
            InvokeTrigger(sensor, "OnTriggerEnter", firstCollider);
            InvokeTrigger(sensor, "OnTriggerEnter", secondCollider);

            InvokeTrigger(sensor, "OnTriggerExit", firstCollider);

            Assert.AreEqual(1, GetRegisteredTargetCount(sensor));

            InvokeTrigger(sensor, "OnTriggerExit", secondCollider);

            Assert.AreEqual(0, GetRegisteredTargetCount(sensor));
        }

        [Test]
        public void ResetForDespawn_ClearsRegistry()
        {
            HealerTargetSensor sensor = CreateSensor(out _);
            Collider collider = CreateEnemy("Ally").AddComponent<BoxCollider>();
            InvokeTrigger(sensor, "OnTriggerEnter", collider);

            sensor.ResetForDespawn();

            Assert.AreEqual(0, GetRegisteredTargetCount(sensor));
        }

        private HealerTargetSensor CreateSensor(out GameObject owner)
        {
            owner = CreateEnemy("Healer");
            GameObject sensorObject = CreateObject("HealSensor");
            sensorObject.transform.SetParent(owner.transform);
            sensorObject.AddComponent<SphereCollider>().isTrigger = true;
            Rigidbody rigidbody = sensorObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            HealerTargetSensor sensor = sensorObject.AddComponent<HealerTargetSensor>();
            InvokeAwake(sensor);
            return sensor;
        }

        private GameObject CreateEnemy(string name)
        {
            GameObject gameObject = CreateObject(name);
            gameObject.AddComponent<StatisticsController>();
            gameObject.AddComponent<EnemyTargetable>();
            return gameObject;
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeTrigger(HealerTargetSensor sensor, string methodName, Collider collider)
        {
            MethodInfo method = typeof(HealerTargetSensor).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(sensor, new object[] { collider });
        }

        private static void InvokeAwake(HealerTargetSensor sensor)
        {
            MethodInfo method = typeof(HealerTargetSensor).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(sensor, null);
        }

        private static int GetRegisteredTargetCount(HealerTargetSensor sensor)
        {
            FieldInfo field = typeof(HealerTargetSensor).GetField(
                "registeredTargets",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return ((IDictionary)field.GetValue(sensor)).Count;
        }
    }
}
