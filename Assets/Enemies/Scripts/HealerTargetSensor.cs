using System.Collections.Generic;
using RPGame.Core.Pooling;
using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Enemies
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class HealerTargetSensor : MonoBehaviour, IHealerTargetProvider, IPooledEnemyResettable
    {
        private sealed class RegisteredTarget
        {
            public RegisteredTarget(HealerTarget target)
            {
                Target = target;
            }

            public HealerTarget Target { get; }
            public int OverlapCount { get; set; }
        }

        [SerializeField] private SphereCollider sensorCollider;

        private readonly Dictionary<EnemyTargetable, RegisteredTarget> registeredTargets = new();
        private readonly List<HealerTarget> targets = new();
        private readonly List<EnemyTargetable> staleTargets = new();
        private EnemyTargetable owner;

        public IReadOnlyList<HealerTarget> Targets
        {
            get
            {
                RebuildTargets();
                return targets;
            }
        }

        private void Awake()
        {
            CacheDependencies();
        }

        private void OnDisable()
        {
            ClearTargets();
        }

        public void SetSearchRadius(float radius)
        {
            CacheDependencies();
            sensorCollider.radius = Mathf.Max(0f, radius);
        }

        public void ResetForSpawn()
        {
            ClearTargets();
        }

        public void ResetForDespawn()
        {
            ClearTargets();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!TryResolveTarget(other, out EnemyTargetable targetable, out StatisticsController statistics))
            {
                return;
            }

            if (registeredTargets.TryGetValue(targetable, out RegisteredTarget registeredTarget))
            {
                registeredTarget.OverlapCount++;
                return;
            }

            registeredTargets.Add(targetable, new RegisteredTarget(new HealerTarget(targetable, statistics))
            {
                OverlapCount = 1
            });
        }

        private void OnTriggerExit(Collider other)
        {
            EnemyTargetable targetable = other.GetComponentInParent<EnemyTargetable>();
            if (targetable == null || !registeredTargets.TryGetValue(targetable, out RegisteredTarget registeredTarget))
            {
                return;
            }

            registeredTarget.OverlapCount--;
            if (registeredTarget.OverlapCount <= 0)
            {
                registeredTargets.Remove(targetable);
            }
        }

        private bool TryResolveTarget(
            Collider other,
            out EnemyTargetable targetable,
            out StatisticsController statistics)
        {
            targetable = other.GetComponentInParent<EnemyTargetable>();
            statistics = null;
            if (targetable == null || targetable == owner)
            {
                return false;
            }

            statistics = targetable.GetComponentInParent<StatisticsController>();
            return statistics != null;
        }

        private void RebuildTargets()
        {
            targets.Clear();
            staleTargets.Clear();
            foreach (KeyValuePair<EnemyTargetable, RegisteredTarget> entry in registeredTargets)
            {
                if (entry.Value.Target.IsValid)
                {
                    targets.Add(entry.Value.Target);
                }
                else
                {
                    staleTargets.Add(entry.Key);
                }
            }

            for (int i = 0; i < staleTargets.Count; i++)
            {
                registeredTargets.Remove(staleTargets[i]);
            }
        }

        private void ClearTargets()
        {
            registeredTargets.Clear();
            targets.Clear();
            staleTargets.Clear();
        }

        private void CacheDependencies()
        {
            if (sensorCollider == null)
            {
                sensorCollider = GetComponent<SphereCollider>();
            }

            if (owner == null)
            {
                owner = GetComponentInParent<EnemyTargetable>();
            }
        }
    }
}
