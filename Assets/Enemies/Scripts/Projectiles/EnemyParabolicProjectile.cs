using System;
using System.Collections.Generic;
using RPGame.Combat.Projectiles;
using RPGame.Core.Buildup;
using RPGame.Core.Damage;
using UnityEngine;

namespace RPGame.Enemies
{
    [RequireComponent(typeof(ParabolicProjectileMover))]
    public sealed class EnemyParabolicProjectile : EnemyProjectile, IEnemyProjectile
    {
        private const float TelegraphGroundOffset = 0.02f;
        private const int AoEHitBufferSize = 32;

        [SerializeField] private float projectileLifetime = 5f;
        [SerializeField] private float arcHeight = 3f;
        [SerializeField] private float ascentDuration = 0.75f;
        [SerializeField] private float apexPauseDuration;
        [SerializeField] private float descentDuration = 0.5f;
        [SerializeField] private GameObject telegraphPrefab;
        [SerializeField] private float aoeRadius = 2f;
        [SerializeField] private LayerMask aoeObstacleMask;
        [SerializeField] private QueryTriggerInteraction aoeTriggerInteraction = QueryTriggerInteraction.Collide;

        private readonly Collider[] aoeHitBuffer = new Collider[AoEHitBufferSize];
        private readonly HashSet<IDamageable> damagedTargets = new();
        private ParabolicProjectileMover mover;
        private bool trajectoryCompleted;
        private GameObject activeTelegraph;

        public event Action<EnemyParabolicProjectile> ApexReached;

        internal Vector3 ApexPoint => mover.ApexPoint;
        internal Vector3 ImpactPoint => mover.ImpactPoint;
        internal float CurrentSpeed => mover.CurrentSpeed;
        internal ParabolicProjectilePhase Phase => mover.Phase;
        internal bool HasReachedApex => mover.HasReachedApex;
        internal int ApexReachedCount { get; private set; }
        internal EnemyProjectileHit LastImpact { get; private set; }
        internal bool HasImpact { get; private set; }
        internal GameObject ActiveTelegraph => activeTelegraph;

        private void Start()
        {
            CacheMover();
        }

        public void Initialize(
            Vector3 targetPosition,
            IDamageable targetDamageable,
            IReadOnlyList<PartialDamage> damageParts,
            GameObject source,
            BuildupData buildup = null)
        {
            InitializeProjectile(damageParts, source, projectileLifetime);
            trajectoryCompleted = false;
            HasImpact = false;
            LastImpact = default;
            ApexReachedCount = 0;
            DestroyTelegraph();
            CacheMover();
            mover.ApexReached -= OnMoverApexReached;
            mover.ApexReached += OnMoverApexReached;
            mover.InitializeTrajectory(
                transform.position,
                targetPosition,
                arcHeight,
                ascentDuration,
                apexPauseDuration,
                descentDuration);
        }

        protected override void Move(float deltaTime)
        {
            mover.Tick(deltaTime);
            trajectoryCompleted = mover.IsComplete;
        }

        protected override void AfterMove()
        {
            if (trajectoryCompleted)
            {
                FinishAt(mover.ImpactPoint, Vector3.up);
            }
        }

        protected override void OnImpact(EnemyProjectileHit hit, IDamageable damageable)
        {
            HasImpact = true;
            LastImpact = hit;
            ApplyAoEDamage(hit.Point);
        }

        protected override void Cleanup()
        {
            DestroyTelegraph();
        }

        private void OnDestroy()
        {
            DestroyTelegraph();
        }

        private void CacheMover()
        {
            if (mover != null)
            {
                return;
            }

            mover = GetComponent<ParabolicProjectileMover>();
        }

        private void OnMoverApexReached()
        {
            ApexReachedCount++;
            SpawnTelegraph();
            ApexReached?.Invoke(this);
        }

        private void SpawnTelegraph()
        {
            if (activeTelegraph != null || telegraphPrefab == null)
            {
                return;
            }

            activeTelegraph = Instantiate(
                telegraphPrefab,
                mover.ImpactPoint + Vector3.up * TelegraphGroundOffset,
                Quaternion.identity);

            float diameter = aoeRadius * 2f;
            activeTelegraph.transform.localScale = new Vector3(diameter, activeTelegraph.transform.localScale.y, diameter);
        }

        private void ApplyAoEDamage(Vector3 impactPoint)
        {
            if (aoeRadius <= 0f || DamageParts == null || DamageParts.Count == 0)
            {
                return;
            }

            damagedTargets.Clear();
            int hitCount = Physics.OverlapSphereNonAlloc(
                impactPoint,
                aoeRadius,
                aoeHitBuffer,
                ~0,
                aoeTriggerInteraction);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = aoeHitBuffer[i];
                if (!TryGetAoETarget(hitCollider, out IDamageable target))
                {
                    continue;
                }

                if (damagedTargets.Contains(target)
                    || !target.CanReceiveDamage
                    || !HasImpactLineOfSight(impactPoint, hitCollider.bounds.center))
                {
                    continue;
                }

                damagedTargets.Add(target);
                target.ApplyDamage(new DamageData(DamageParts, Source));
            }
        }

        private bool TryGetAoETarget(Collider hitCollider, out IDamageable target)
        {
            target = null;
            if (hitCollider == null || hitCollider.gameObject == gameObject || hitCollider.transform.IsChildOf(transform))
            {
                return false;
            }

            if (Source != null && (hitCollider.gameObject == Source || hitCollider.transform.IsChildOf(Source.transform)))
            {
                return false;
            }

            target = hitCollider.GetComponentInParent<IDamageable>();
            return target != null;
        }

        private bool HasImpactLineOfSight(Vector3 impactPoint, Vector3 targetPoint)
        {
            Vector3 direction = targetPoint - impactPoint;
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return true;
            }

            return !Physics.Raycast(
                impactPoint,
                direction / distance,
                distance,
                aoeObstacleMask,
                QueryTriggerInteraction.Ignore);
        }

        private void DestroyTelegraph()
        {
            if (activeTelegraph == null)
            {
                return;
            }

            GameObject telegraph = activeTelegraph;
            activeTelegraph = null;
            if (Application.isPlaying)
            {
                Destroy(telegraph);
            }
            else
            {
                DestroyImmediate(telegraph);
            }
        }

        private void OnValidate()
        {
            projectileLifetime = Mathf.Max(0f, projectileLifetime);
            arcHeight = Mathf.Max(0f, arcHeight);
            ascentDuration = Mathf.Max(0.001f, ascentDuration);
            apexPauseDuration = Mathf.Max(0f, apexPauseDuration);
            descentDuration = Mathf.Max(0.001f, descentDuration);
            aoeRadius = Mathf.Max(0f, aoeRadius);
        }
    }
}
