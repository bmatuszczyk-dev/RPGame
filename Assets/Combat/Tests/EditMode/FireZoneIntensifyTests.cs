using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using RPGame.Core.Statuses;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RPGame.Combat.Tests
{
    public sealed class FireZoneIntensifyTests
    {
        private readonly List<Object> createdObjects = new();
        private GameObject caster;
        private StatusAggregator targetStatuses;
        private BurningStatusDefinition burn;

        [SetUp]
        public void SetUp()
        {
            caster = CreateGameObject("Caster");
            GameObject target = CreateGameObject("Target");
            target.AddComponent<TestDamageable>();
            targetStatuses = target.AddComponent<StatusAggregator>();
            InvokeAwake(targetStatuses);
            burn = ScriptableObject.CreateInstance<BurningStatusDefinition>();
            createdObjects.Add(burn);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        [Test]
        public void FireZone_WithIntensify_ReapplyIncreasesSingleBurnIntensity()
        {
            FireZoneBehaviour zone = CreateZone(new IRuntimeSpellBehavior[] { new IntensifyBehavior() });

            ApplyBurn(zone);
            ApplyBurn(zone);

            Assert.AreEqual(1, targetStatuses.Statuses.Count);
            Assert.AreEqual(2, targetStatuses.Statuses[0].CurrentStacks);
        }

        [Test]
        public void FireZone_WithoutIntensify_ReapplyOnlyRefreshesBurn()
        {
            FireZoneBehaviour zone = CreateZone(System.Array.Empty<IRuntimeSpellBehavior>());

            ApplyBurn(zone);
            ApplyBurn(zone);

            Assert.AreEqual(1, targetStatuses.Statuses.Count);
            Assert.AreEqual(1, targetStatuses.Statuses[0].CurrentStacks);
        }

        [Test]
        public void FireZones_WithIntensify_ShareOneBurnInstance()
        {
            IRuntimeSpellBehavior[] behaviors = { new IntensifyBehavior() };
            FireZoneBehaviour firstZone = CreateZone(behaviors);
            FireZoneBehaviour secondZone = CreateZone(behaviors);

            ApplyBurn(firstZone);
            ApplyBurn(secondZone);

            Assert.AreEqual(1, targetStatuses.Statuses.Count);
            Assert.AreEqual(2, targetStatuses.Statuses[0].CurrentStacks);
        }

        [Test]
        public void FireZone_ReapplyDoesNotExecuteOtherRuntimeBehaviors()
        {
            RecordingBehavior recording = new();
            FireZoneBehaviour zone = CreateZone(new IRuntimeSpellBehavior[] { recording, new IntensifyBehavior() });

            ApplyBurn(zone);
            ApplyBurn(zone);

            Assert.AreEqual(0, recording.ExecutionCount);
            Assert.AreEqual(2, targetStatuses.Statuses[0].CurrentStacks);
        }

        [Test]
        public void Intensify_SupportsOnlyFireZoneSpell()
        {
            IntensifyBehavior intensify = new();
            FireZoneSpell fireZone = ScriptableObject.CreateInstance<FireZoneSpell>();
            EarthZoneSpell earthZone = ScriptableObject.CreateInstance<EarthZoneSpell>();
            createdObjects.Add(fireZone);
            createdObjects.Add(earthZone);

            Assert.IsTrue(intensify.Supports(fireZone));
            Assert.IsFalse(intensify.Supports(earthZone));
        }

        private FireZoneBehaviour CreateZone(IReadOnlyList<IRuntimeSpellBehavior> runtimeBehaviors)
        {
            FireZoneBehaviour zone = CreateGameObject("Fire Zone").AddComponent<FireZoneBehaviour>();
            SerializedObject serializedZone = new(zone);
            serializedZone.FindProperty("burningStatus").objectReferenceValue = burn;
            serializedZone.FindProperty("burningDuration").floatValue = 3f;
            serializedZone.ApplyModifiedPropertiesWithoutUndo();
            zone.Initialize(
                new CasterDataBuilder(caster, caster.transform, targetStatuses.transform)
                    .WithRuntimeBehaviors(runtimeBehaviors)
                    .Build(),
                1f);
            return zone;
        }

        private void ApplyBurn(FireZoneBehaviour zone)
        {
            typeof(FireZoneBehaviour).GetMethod(
                "ApplyStatusTo",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(zone, new object[] { targetStatuses });
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeAwake(StatusAggregator target)
        {
            typeof(StatusAggregator).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }

        private sealed class RecordingBehavior : ISpellBehavior
        {
            public int ExecutionCount { get; private set; }
            public SpellBehaviorPhase Phase => SpellBehaviorPhase.PostResolve;

            public void Execute(SpellBehaviorContext context)
            {
                ExecutionCount++;
            }
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public bool CanReceiveDamage => true;

            public DamageResult ApplyDamage(DamageData data)
            {
                return DamageResult.Applied(data, data.Amount, 100f, 100f - data.Amount, false);
            }
        }
    }
}
