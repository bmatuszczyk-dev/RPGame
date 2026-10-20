using System.Collections.Generic;
using NUnit.Framework;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class HealerEnemyBehaviourTests
    {
        private readonly List<GameObject> createdObjects = new();
        private HealerEnemyBehaviourConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<HealerEnemyBehaviourConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }

            Object.DestroyImmediate(config);
        }

        [Test]
        public void Tick_SelectsAllyWithLowestHealthNormalized()
        {
            TargetProvider provider = new();
            HealerTarget healthier = CreateTarget("Healthier", 80f);
            HealerTarget wounded = CreateTarget("Wounded", 30f);
            provider.TargetList.Add(healthier);
            provider.TargetList.Add(wounded);
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out _, out _, out _);

            behaviour.Tick(0.1f);

            Assert.AreSame(wounded, behaviour.CurrentHealTarget);
        }

        [Test]
        public void Tick_IgnoresFullAndDeadTargets()
        {
            TargetProvider provider = new();
            provider.TargetList.Add(CreateTarget("Full", 100f));
            provider.TargetList.Add(CreateTarget("Dead", 0f));
            HealerTarget wounded = CreateTarget("Wounded", 60f);
            provider.TargetList.Add(wounded);
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out _, out _, out _);

            behaviour.Tick(0.1f);

            Assert.AreSame(wounded, behaviour.CurrentHealTarget);
        }

        [Test]
        public void Tick_KeepsCurrentTargetWhenMoreWoundedAllyAppears()
        {
            TargetProvider provider = new();
            HealerTarget initialTarget = CreateTarget("Initial", 70f);
            provider.TargetList.Add(initialTarget);
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out _, out _, out _);
            behaviour.Tick(0.1f);

            provider.TargetList.Add(CreateTarget("MoreWounded", 10f));
            behaviour.Tick(0.1f);

            Assert.AreSame(initialTarget, behaviour.CurrentHealTarget);
        }

        [Test]
        public void Tick_WhenTargetBecomesFull_SelectsAnotherTarget()
        {
            TargetProvider provider = new();
            HealerTarget initialTarget = CreateTarget("Initial", 40f);
            HealerTarget nextTarget = CreateTarget("Next", 70f);
            provider.TargetList.Add(initialTarget);
            provider.TargetList.Add(nextTarget);
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out _, out _, out _);
            behaviour.Tick(0.1f);
            ((ContinuousHealTests.FakeStatistics)initialTarget.Statistics).Heal(100f);

            behaviour.Tick(0.1f);

            Assert.AreSame(nextTarget, behaviour.CurrentHealTarget);
        }

        [Test]
        public void Tick_WhenTargetLeavesProvider_SelectsAnotherTarget()
        {
            TargetProvider provider = new();
            HealerTarget initialTarget = CreateTarget("Initial", 40f);
            HealerTarget nextTarget = CreateTarget("Next", 70f);
            provider.TargetList.Add(initialTarget);
            provider.TargetList.Add(nextTarget);
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out _, out _, out _);
            behaviour.Tick(0.1f);

            provider.TargetList.Remove(initialTarget);
            behaviour.Tick(0.1f);

            Assert.AreSame(nextTarget, behaviour.CurrentHealTarget);
        }

        [Test]
        public void Tick_WhenTargetDies_SelectsAnotherTarget()
        {
            TargetProvider provider = new();
            HealerTarget initialTarget = CreateTarget("Initial", 40f);
            HealerTarget nextTarget = CreateTarget("Next", 70f);
            provider.TargetList.Add(initialTarget);
            provider.TargetList.Add(nextTarget);
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out _, out _, out _);
            behaviour.Tick(0.1f);
            ((ContinuousHealTests.FakeStatistics)initialTarget.Statistics).TakeDamage(100f);

            behaviour.Tick(0.1f);

            Assert.AreSame(nextTarget, behaviour.CurrentHealTarget);
        }

        [Test]
        public void Tick_OutsideHealRange_MovesToMaintainHealRangeWithoutHealing()
        {
            TargetProvider provider = new();
            provider.TargetList.Add(CreateTarget("Ally", 50f, new Vector3(10f, 0f, 0f)));
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out FakeMovement movement, out _, out _);

            behaviour.Tick(1f);

            Assert.AreEqual(1, movement.MoveToCount);
            Assert.That(movement.LastMoveTo, Is.EqualTo(new Vector3(4f, 0f, 0f)));
            Assert.IsFalse(behaviour.IsHealing);
        }

        [Test]
        public void Tick_InRangeWithLineOfSight_HealsWhileMaintainingHealRange()
        {
            TargetProvider provider = new();
            HealerTarget target = CreateTarget("Ally", 50f, new Vector3(2f, 0f, 0f));
            provider.TargetList.Add(target);
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out FakeMovement movement, out _, out _);

            behaviour.Tick(1f);

            Assert.AreEqual(1, movement.MoveToCount);
            Assert.That(movement.LastMoveTo, Is.EqualTo(new Vector3(-4f, 0f, 0f)));
            Assert.IsTrue(behaviour.IsHealing);
            Assert.That(target.Statistics.CurrentHealth, Is.EqualTo(60f));
        }

        [Test]
        public void Tick_InRangeWithoutLineOfSight_MovesWithoutHealing()
        {
            TargetProvider provider = new();
            Vector3 targetPosition = new(2f, 0f, 0f);
            provider.TargetList.Add(CreateTarget("Ally", 50f, targetPosition));
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out FakeMovement movement, out _, out FakeLineOfSight lineOfSight);
            lineOfSight.Result = false;

            behaviour.Tick(1f);

            Assert.AreEqual(1, movement.MoveToCount);
            Assert.That(movement.LastMoveTo, Is.EqualTo(targetPosition));
            Assert.IsFalse(behaviour.IsHealing);
        }

        [Test]
        public void Tick_WithoutMana_DoesNotHeal()
        {
            TargetProvider provider = new();
            provider.TargetList.Add(CreateTarget("Ally", 50f, new Vector3(2f, 0f, 0f)));
            HealerEnemyBehaviour behaviour = CreateBehaviour(
                provider,
                out _,
                out _,
                out _,
                mana: 0f);

            behaviour.Tick(1f);

            Assert.IsFalse(behaviour.IsHealing);
        }

        [Test]
        public void Tick_WithoutTargetAndNearbyPlayer_Retreats()
        {
            TargetProvider provider = new();
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out FakeMovement movement, out FakeDetection detection, out _);
            detection.Target = new SelectedTarget(CreateObject("Player").AddComponent<PlayerTargetable>(), new Vector3(2f, 0f, 0f));

            behaviour.Tick(0.1f);

            Assert.AreEqual(1, movement.MoveToCount);
        }

        [Test]
        public void Tick_WithoutTargetAndDistantPlayer_Idles()
        {
            TargetProvider provider = new();
            HealerEnemyBehaviour behaviour = CreateBehaviour(provider, out FakeMovement movement, out FakeDetection detection, out _);
            detection.Target = new SelectedTarget(CreateObject("Player").AddComponent<PlayerTargetable>(), new Vector3(8f, 0f, 0f));

            behaviour.Tick(0.1f);

            Assert.AreEqual(1, movement.StopCount);
        }

        private HealerEnemyBehaviour CreateBehaviour(
            TargetProvider provider,
            out FakeMovement movement,
            out FakeDetection detection,
            out FakeLineOfSight lineOfSight,
            float mana = 100f)
        {
            GameObject ownerObject = CreateObject("Healer");
            EnemyTargetable owner = ownerObject.AddComponent<EnemyTargetable>();
            movement = new FakeMovement();
            detection = new FakeDetection();
            lineOfSight = new FakeLineOfSight();
            return new HealerEnemyBehaviour(
                detection,
                movement,
                provider,
                lineOfSight,
                new ContinuousHealTests.FakeStatistics(currentMana: mana),
                config,
                owner);
        }

        private HealerTarget CreateTarget(string name, float health, Vector3? position = null)
        {
            GameObject gameObject = CreateObject(name);
            gameObject.transform.position = position ?? Vector3.zero;
            EnemyTargetable targetable = gameObject.AddComponent<EnemyTargetable>();
            return new HealerTarget(targetable, new ContinuousHealTests.FakeStatistics(health, 100f));
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class TargetProvider : IHealerTargetProvider
        {
            public List<HealerTarget> TargetList { get; } = new();
            public IReadOnlyList<HealerTarget> Targets => TargetList;
        }

        private sealed class FakeDetection : IEnemyDetection
        {
            public SelectedTarget Target { get; set; }
            public bool TryGetTarget(out SelectedTarget target)
            {
                target = Target;
                return target.IsValid;
            }
        }

        private sealed class FakeLineOfSight : IEnemyLineOfSight
        {
            public bool Result { get; set; } = true;
            public bool HasLineOfSight(Vector3 targetPosition) => Result;
            public bool HasLineOfSightFrom(Vector3 origin, Vector3 targetPosition) => Result;
        }

        private sealed class FakeMovement : IEnemyMovement
        {
            public Vector3 Position { get; set; }
            public bool IsLeaping => false;
            public bool IsCharging => false;
            public bool IsMovementBlocked => false;
            public int MoveToCount { get; private set; }
            public int StopCount { get; private set; }
            public Vector3 LastMoveTo { get; private set; }

            public void FaceTowards(Vector3 position) { }
            public void MoveTo(Vector3 position)
            {
                MoveToCount++;
                LastMoveTo = position;
            }
            public void Stop() => StopCount++;
            public bool TryResolvePosition(Vector3 desiredPosition, out Vector3 validPosition)
            {
                validPosition = desiredPosition;
                return true;
            }

            public bool TryLeapTo(Vector3 destination, float speed, float arcHeight) => false;
            public bool TryCombatLeapTo(Vector3 destination, float speed, float arcHeight, float fallSpeed, System.Action<bool> onCompleted) => false;
            public bool TryStartCharge(Vector3 destination, float speed, float maxDistance, float knockbackResistance, System.Action<Collider, Vector3> onCollision) => false;
        }
    }
}
