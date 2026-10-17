using System.Collections.Generic;
using RPGame.Core.Damage;
using RPGame.Core.Pooling;
using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class Attack : MonoBehaviour, IEnemyAttack, IPooledEnemyResettable
    {
        [SerializeField] private AttackType attackType = AttackType.Melee;
        [SerializeField] private LineOfSight lineOfSight;
        [SerializeField] private GroundProjection groundProjection;
        [SerializeField] private ProjectileLauncher projectileLauncher;
        [SerializeField] private AssasinSlashView assasinSlashView;

        private Config config;
        private readonly Dictionary<AttackType, IEnemyAttack> runtimeAttacks = new();

        float IEnemyAttack.Range
        {
            get => TryGetRuntimeAttack(attackType, out IEnemyAttack attack) ? attack.Range : 0f;
        }

        internal void SetConfig(Config config)
        {
            if (this.config == config)
            {
                return;
            }

            this.config = config;
            runtimeAttacks.Clear();
        }

        public void ResetForSpawn()
        {
            foreach (IEnemyAttack attack in runtimeAttacks.Values)
            {
                if (attack is IPooledEnemyResettable resettable)
                {
                    resettable.ResetForSpawn();
                }
            }
        }

        public void ResetForDespawn()
        {
            foreach (IEnemyAttack attack in runtimeAttacks.Values)
            {
                if (attack is IPooledEnemyResettable resettable)
                {
                    resettable.ResetForDespawn();
                }
            }
        }

        void IEnemyAttack.Tick(float deltaTime)
        {
            if (TryGetRuntimeAttack(attackType, out IEnemyAttack attack))
            {
                attack.Tick(deltaTime);
            }
        }

        bool IEnemyAttack.IsInRange(SelectedTarget target)
        {
            return TryGetRuntimeAttack(attackType, out IEnemyAttack attack) && attack.IsInRange(target);
        }

        bool IEnemyAttack.TryAttack(SelectedTarget target)
        {
            return TryGetRuntimeAttack(attackType, out IEnemyAttack attack) && attack.TryAttack(target);
        }

        internal bool TryGetRuntimeAttack(AttackType type, out IEnemyAttack attack)
        {
            if (runtimeAttacks.TryGetValue(type, out attack))
            {
                return true;
            }

            if (config == null)
            {
                Debug.LogError("Missing field config.", this);
                return false;
            }

            try
            {
                attack = CreateRuntimeAttack(type);
                runtimeAttacks.Add(type, attack);
                return true;
            }
            catch (System.InvalidOperationException exception)
            {
                Debug.LogError(exception.Message, this);
                attack = null;
                return false;
            }
        }

        private IEnemyAttack CreateRuntimeAttack(AttackType type)
        {
            return type switch
            {
                AttackType.Melee => new MeleeAttack(
                    config.GetAttack<MeleeAttackConfig>(AttackType.Melee),
                    () => transform.position,
                    GetDamageable,
                    () => gameObject),

                AttackType.StraightProjectile => new StraightProjectileAttack(
                    config.GetAttack<StraightProjectileAttackConfig>(AttackType.StraightProjectile),
                    GetLineOfSight(),
                    GetProjectileLauncher(),
                    GetDamageable,
                    () => gameObject),

                AttackType.ParabolicProjectile => new ParabolicProjectileAttack(
                    config.GetAttack<ParabolicProjectileAttackConfig>(AttackType.ParabolicProjectile),
                    GetLineOfSight(),
                    GetGroundProjection(),
                    GetProjectileLauncher(),
                    () => gameObject),

                AttackType.AssasinSlash => new AssasinSlashAttack(
                    config.GetAttack<AssasinSlashAttackConfig>(AttackType.AssasinSlash),
                    transform,
                    GetDamageable,
                    () => gameObject,
                    GetAssasinSlashView()),

                _ => throw new System.InvalidOperationException($"Unsupported attack type '{type}'.")
            };
        }

        private static bool TryGetDamageable(SelectedTarget target, out IDamageable damageable)
        {
            damageable = null;
            if (!target.IsValid || target.Targetable.TargetPoint == null)
            {
                return false;
            }

            damageable = target.Targetable.TargetPoint.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                return false;
            }

            return true;
        }

        private static IDamageable GetDamageable(SelectedTarget target)
        {
            return TryGetDamageable(target, out IDamageable damageable) ? damageable : null;
        }

        private IEnemyLineOfSight GetLineOfSight()
        {
            if (lineOfSight == null)
            {
                lineOfSight = GetComponent<LineOfSight>();
            }

            if (lineOfSight == null)
            {
                throw new System.InvalidOperationException("Missing field lineOfSight.");
            }

            return lineOfSight;
        }

        private IEnemyGroundProjection GetGroundProjection()
        {
            if (groundProjection == null)
            {
                groundProjection = GetComponent<GroundProjection>();
            }

            if (groundProjection == null)
            {
                throw new System.InvalidOperationException("Missing field groundProjection.");
            }

            return groundProjection;
        }

        private IProjectileLauncher GetProjectileLauncher()
        {
            if (projectileLauncher == null)
            {
                projectileLauncher = GetComponent<ProjectileLauncher>();
            }

            if (projectileLauncher == null)
            {
                throw new System.InvalidOperationException("Missing field projectileLauncher.");
            }

            return projectileLauncher;
        }

        private AssasinSlashView GetAssasinSlashView()
        {
            if (assasinSlashView == null)
            {
                assasinSlashView = GetComponent<AssasinSlashView>();
            }

            if (assasinSlashView == null)
            {
                throw new System.InvalidOperationException("Missing field assasinSlashView.");
            }

            return assasinSlashView;
        }
    }
}
