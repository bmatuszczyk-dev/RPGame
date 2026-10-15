using RPGame.Combat.Projectiles;
using RPGame.Core.Buildup;
using RPGame.Core.Damage;
using RPGame.Core.Statuses;
using UnityEngine;

namespace RPGame.Enemies
{
    [RequireComponent(typeof(StraightProjectileMover))]
    public sealed class EnemyStraightProjectile : EnemyProjectile, IEnemyProjectile, IProjectileMovementSource
    {
        private static readonly StatusSourceId BuildupSourceId = new("EnemyProjectileBuildup");

        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float projectileLifetime = 5f;

        private StraightProjectileMover mover;
        private IDamageable targetDamageable;
        private BuildupData buildup;

        public float CurrentSpeed => projectileSpeed;

        private void Start()
        {
            CacheMover();
        }

        public void Initialize(
            Vector3 targetPosition,
            IDamageable targetDamageable,
            System.Collections.Generic.IReadOnlyList<PartialDamage> damageParts,
            GameObject source,
            BuildupData buildup = null)
        {
            this.targetDamageable = targetDamageable;
            this.buildup = buildup;
            InitializeProjectile(damageParts, source, projectileLifetime);

            CacheMover();
            mover.Initialize(this);
        }

        protected override void Move(float deltaTime)
        {
            mover.Tick(deltaTime);
        }

        protected override void OnImpact(EnemyProjectileHit hit, IDamageable damageable)
        {
            if (ReferenceEquals(damageable, targetDamageable))
            {
                damageable.ApplyDamage(new DamageData(DamageParts, Source));
                IBuildupReceiver buildupReceiver = hit.Collider.GetComponentInParent<IBuildupReceiver>();
                buildupReceiver?.AddBuildup(
                    buildup?.Definition,
                    buildup?.Amount ?? 0f,
                    new StatusContext(BuildupSourceId, Source));
            }
        }

        private void CacheMover()
        {
            if (mover != null)
            {
                return;
            }

            mover = GetComponent<StraightProjectileMover>();
        }

        private void OnValidate()
        {
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            projectileLifetime = Mathf.Max(0f, projectileLifetime);
        }
    }
}
