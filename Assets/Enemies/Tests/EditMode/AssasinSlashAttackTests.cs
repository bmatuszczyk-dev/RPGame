using NUnit.Framework;
using RPGame.Core.Damage;
using RPGame.Core.Targeting;
using UnityEditor;
using UnityEngine;

namespace RPGame.Enemies.Tests
{
    public sealed class AssasinSlashAttackTests
    {
        private GameObject attacker;
        private GameObject target;
        private AssasinSlashAttackConfig config;
        private FakeDamageable targetDamageable;

        [SetUp]
        public void SetUp()
        {
            attacker = new GameObject("Attacker");
            AssasinSlashView slashView = attacker.AddComponent<AssasinSlashView>();
            Transform leftBladePivot = CreateBladePivot("LeftBladePivot", new Vector3(-0.25f, 0.5f, 0f));
            Transform rightBladePivot = CreateBladePivot("RightBladePivot", new Vector3(0.25f, 0.5f, 0f));
            SerializedObject serializedView = new(slashView);
            serializedView.FindProperty("leftBladePivot").objectReferenceValue = leftBladePivot;
            serializedView.FindProperty("rightBladePivot").objectReferenceValue = rightBladePivot;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            target = new GameObject("Target") { layer = 3 };
            target.transform.position = Vector3.forward;
            target.AddComponent<BoxCollider>();
            targetDamageable = target.AddComponent<FakeDamageable>();
            config = ScriptableObject.CreateInstance<AssasinSlashAttackConfig>();

            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty("hitLayers").intValue = 1 << 3;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Tick_DoesNotResolveDamageDuringWindup()
        {
            AssasinSlashAttack attack = CreateAttack();
            attack.TryAttack(CreateTarget());

            attack.Tick(0.14f);

            Assert.That(targetDamageable.ApplyCount, Is.Zero);
        }

        [Test]
        public void Tick_ResolvesDamageExactlyOnceAtHitTiming()
        {
            AssasinSlashAttack attack = CreateAttack();
            attack.TryAttack(CreateTarget());

            TickToHit(attack);
            attack.Tick(1f);

            Assert.That(targetDamageable.ApplyCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_MissesTargetOutsideSpatialHitbox()
        {
            target.transform.position = Vector3.forward * 5f;
            AssasinSlashAttack attack = CreateAttack();
            attack.TryAttack(CreateTarget());

            TickToHit(attack);

            Assert.That(targetDamageable.ApplyCount, Is.Zero);
        }

        [Test]
        public void Tick_MultipleTargetCollidersResolveDamageOnce()
        {
            target.AddComponent<SphereCollider>();
            AssasinSlashAttack attack = CreateAttack();
            attack.TryAttack(CreateTarget());

            TickToHit(attack);

            Assert.That(targetDamageable.ApplyCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_KeepsDirectionLockedAfterAttackStarts()
        {
            AssasinSlashAttack attack = CreateAttack();
            target.transform.position = Vector3.forward;
            attack.TryAttack(CreateTarget());
            Vector3 lockedForward = attacker.transform.forward;
            target.transform.position = Vector3.right;

            attack.Tick(0.1f);

            Assert.That(attacker.transform.forward, Is.EqualTo(lockedForward));
        }

        [Test]
        public void ResetForDespawn_ClearsActiveSlash()
        {
            AssasinSlashAttack attack = CreateAttack();
            attack.TryAttack(CreateTarget());

            attack.ResetForDespawn();

            Assert.That(attack.State, Is.EqualTo(AssasinSlashState.Idle));
        }

        private AssasinSlashAttack CreateAttack()
        {
            return new AssasinSlashAttack(
                config,
                attacker.transform,
                _ => targetDamageable,
                () => attacker,
                attacker.GetComponent<AssasinSlashView>());
        }

        private Transform CreateBladePivot(string pivotName, Vector3 localPosition)
        {
            GameObject pivot = new(pivotName);
            pivot.transform.SetParent(attacker.transform, false);
            pivot.transform.localPosition = localPosition;

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(pivot.transform, false);
            blade.transform.localPosition = Vector3.forward * 0.5f;
            blade.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
            return pivot.transform;
        }

        private SelectedTarget CreateTarget()
        {
            return new SelectedTarget(new FakeTargetable(target.transform), target.transform.position);
        }

        private static void TickToHit(AssasinSlashAttack attack)
        {
            Physics.SyncTransforms();
            attack.Tick(0.15f);
            attack.Tick(0.06f);
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
            public int ApplyCount { get; private set; }
            public bool CanReceiveDamage => true;

            public DamageResult ApplyDamage(DamageData data)
            {
                ApplyCount++;
                return DamageResult.Applied(data, 1f, 1f, 0f, false);
            }
        }
    }
}
