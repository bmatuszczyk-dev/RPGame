using RPGame.Core.Damage;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Projectiles
{
    public sealed class ProjectileController : MonoBehaviour, IProjectileMovementSource, IProjectileDestructible
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float acceleration;
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField] private float hitRadius = 0.1f;
        [SerializeField] private bool destroyOnHit = true;

        private ProjectileMover mover;
        private float currentSpeed;
        private float remainingLifetime;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[8];
        private bool hasHit;
        private bool isDestroyed;

        public CasterData CasterData { get; private set; }
        public float CurrentSpeed => currentSpeed;
        public bool IsInitialized { get; private set; }
        public ProjectileTeam Team => ProjectileTeam.Player;

        public void DestroyProjectile()
        {
            if (isDestroyed)
            {
                return;
            }

            isDestroyed = true;
            hasHit = true;
            Destroy(gameObject);
        }

        public void Initialize(CasterData casterData)
        {
            CasterData = casterData;
            currentSpeed = speed;
            remainingLifetime = maxLifetime;
            hasHit = false;
            isDestroyed = false;
            mover = GetComponent<ProjectileMover>();

            if (mover == null)
            {
                Debug.LogWarning($"{name} cannot move because no {nameof(ProjectileMover)} is attached.", this);
                DestroyProjectile();
                return;
            }

            mover.Initialize(this, casterData);
            IsInitialized = true;
        }

        private void Update()
        {
            if (!IsInitialized || isDestroyed)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            remainingLifetime -= deltaTime;
            if (remainingLifetime <= 0f)
            {
                DestroyProjectile();
                return;
            }

            currentSpeed = Mathf.Max(0f, currentSpeed + acceleration * deltaTime);
            Vector3 previousPosition = transform.position;
            mover.Tick(deltaTime);
            CheckForHits(previousPosition, transform.position);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.collider == null)
            {
                return;
            }

            HandleHit(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null)
            {
                return;
            }

            HandleHit(other);
        }

        private void CheckForHits(Vector3 previousPosition, Vector3 currentPosition)
        {
            if (hasHit)
            {
                return;
            }

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
                HandleHit(closestHit.collider);
            }
        }

        private void HandleHit(Collider hitCollider)
        {
            if (hitCollider == null || ShouldIgnore(hitCollider))
            {
                return;
            }

            if (TryGetProjectileDestructible(hitCollider, out IProjectileDestructible projectile)
                && !ReferenceEquals(projectile, this))
            {
                if (projectile.Team != Team)
                {
                    projectile.DestroyProjectile();
                    DestroyProjectile();
                }

                return;
            }

            if (hasHit)
            {
                return;
            }

            hasHit = true;

            if (TryGetDamageable(hitCollider, out IDamageable damageable))
            {
                damageable.ApplyDamage(new DamageData(
                    DamageRangeRoller.Roll(CasterData.DamageRanges),
                    CasterData.CasterObject));
            }

            if (destroyOnHit)
            {
                DestroyProjectile();
            }
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

            GameObject casterObject = CasterData.CasterObject;
            return casterObject != null && (hitObject == casterObject || hitObject.transform.IsChildOf(casterObject.transform));
        }

        private void OnValidate()
        {
            speed = Mathf.Max(0f, speed);
            maxLifetime = Mathf.Max(0.01f, maxLifetime);
            hitRadius = Mathf.Max(0.001f, hitRadius);
        }
    }
}
