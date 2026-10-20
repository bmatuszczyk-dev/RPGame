using System.Collections.Generic;
using RPGame.Combat.Projectiles;
using RPGame.Core.Damage;
using UnityEngine;

namespace RPGame.Enemies
{
    public abstract class EnemyProjectile : MonoBehaviour, IProjectileDestructible
    {
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField] private float hitRadius = 0.1f;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[8];
        private float remainingLifetime;

        protected IReadOnlyList<PartialDamage> DamageParts { get; private set; }
        protected GameObject Source { get; private set; }

        internal bool IsInitialized { get; private set; }
        internal bool IsFinished { get; private set; }
        internal int FinishCount { get; private set; }
        public ProjectileTeam Team => ProjectileTeam.Enemy;

        public void DestroyProjectile()
        {
            Finish();
        }

        protected void InitializeProjectile(
            IReadOnlyList<PartialDamage> damageParts,
            GameObject source,
            float projectileLifetime)
        {
            DamageParts = damageParts;
            Source = source;
            remainingLifetime = projectileLifetime;
            IsFinished = false;
            FinishCount = 0;
            IsInitialized = true;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        internal void Tick(float deltaTime)
        {
            if (!IsInitialized || IsFinished)
            {
                return;
            }

            remainingLifetime -= deltaTime;
            if (remainingLifetime <= 0f)
            {
                Finish();
                return;
            }

            Vector3 previousPosition = transform.position;
            Move(deltaTime);
            CheckForHits(previousPosition, transform.position);
            if (!IsFinished)
            {
                AfterMove();
            }
        }

        protected abstract void Move(float deltaTime);

        protected virtual void AfterMove()
        {
        }

        protected virtual void Cleanup()
        {
        }

        private void CheckForHits(Vector3 previousPosition, Vector3 currentPosition)
        {
            Vector3 displacement = currentPosition - previousPosition;
            float distance = displacement.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return;
            }

            int hitCount = Physics.SphereCastNonAlloc(
                previousPosition,
                hitRadius,
                displacement / distance,
                hitBuffer,
                distance,
                hitLayers,
                triggerInteraction);

            RaycastHit closestHit = default;
            bool foundHit = false;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitBuffer[i];
                if (hit.collider == null || ShouldIgnore(hit.collider) || hit.distance >= closestDistance)
                {
                    continue;
                }

                closestHit = hit;
                closestDistance = hit.distance;
                foundHit = true;
            }

            if (foundHit)
            {
                HandleHit(new EnemyProjectileHit(closestHit.collider, closestHit.point, closestHit.normal));
            }
        }

        internal void HandleHit(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return;
            }

            HandleHit(new EnemyProjectileHit(
                hitCollider,
                hitCollider.ClosestPoint(transform.position),
                Vector3.zero));
        }

        private void HandleHit(EnemyProjectileHit hit)
        {
            if (IsFinished || hit.Collider == null || ShouldIgnore(hit.Collider))
            {
                return;
            }

            if (TryGetProjectileDestructible(hit.Collider, out IProjectileDestructible projectile)
                && !ReferenceEquals(projectile, this))
            {
                if (projectile.Team != Team)
                {
                    projectile.DestroyProjectile();
                    Finish();
                }

                return;
            }

            TryGetDamageable(hit.Collider, out IDamageable damageable);
            OnImpact(hit, damageable);
            Finish();
        }

        protected abstract void OnImpact(EnemyProjectileHit hit, IDamageable damageable);

        protected void FinishAt(Vector3 point, Vector3 normal)
        {
            if (IsFinished)
            {
                return;
            }

            OnImpact(new EnemyProjectileHit(null, point, normal), null);
            Finish();
        }

        private bool TryGetDamageable(Collider hitCollider, out IDamageable damageable)
        {
            damageable = hitCollider.GetComponentInParent<IDamageable>();
            return damageable != null;
        }

        private static bool TryGetProjectileDestructible(
            Collider hitCollider,
            out IProjectileDestructible projectile)
        {
            projectile = hitCollider.GetComponentInParent(typeof(IProjectileDestructible)) as IProjectileDestructible;
            return projectile != null;
        }

        private bool ShouldIgnore(Collider hitCollider)
        {
            GameObject hitObject = hitCollider.gameObject;
            if (hitObject == gameObject || hitObject.transform.IsChildOf(transform))
            {
                return true;
            }

            if (TryGetProjectileDestructible(hitCollider, out IProjectileDestructible projectile)
                && !ReferenceEquals(projectile, this)
                && projectile.Team == Team)
            {
                return true;
            }

            return Source != null && (hitObject == Source || hitObject.transform.IsChildOf(Source.transform));
        }

        private void Finish()
        {
            if (IsFinished)
            {
                return;
            }

            IsFinished = true;
            FinishCount++;
            Cleanup();
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        private void OnValidate()
        {
            hitRadius = Mathf.Max(0.001f, hitRadius);
        }
    }
}
