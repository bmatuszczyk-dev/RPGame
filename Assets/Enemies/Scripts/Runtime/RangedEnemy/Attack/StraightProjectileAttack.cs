using System;
using RPGame.Core.Buildup;
using RPGame.Core.Damage;
using RPGame.Core.Pooling;
using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class StraightProjectileAttack : IEnemyAttack, IPooledEnemyResettable
    {
        private readonly StraightProjectileAttackConfig config;
        private readonly IEnemyLineOfSight lineOfSight;
        private readonly IProjectileLauncher projectileLauncher;
        private readonly Func<SelectedTarget, IDamageable> damageableProvider;
        private readonly Func<GameObject> sourceProvider;
        private float remainingCooldown;

        public StraightProjectileAttack(
            StraightProjectileAttackConfig config,
            IEnemyLineOfSight lineOfSight,
            IProjectileLauncher projectileLauncher,
            Func<SelectedTarget, IDamageable> damageableProvider,
            Func<GameObject> sourceProvider)
        {
            this.config = config;
            this.lineOfSight = lineOfSight;
            this.projectileLauncher = projectileLauncher;
            this.damageableProvider = damageableProvider;
            this.sourceProvider = sourceProvider;
        }

        public float Range => float.PositiveInfinity;

        public void Tick(float deltaTime)
        {
            remainingCooldown -= deltaTime;
        }

        public void ResetForSpawn()
        {
            remainingCooldown = 0f;
        }

        public void ResetForDespawn()
        {
            remainingCooldown = 0f;
        }

        public bool IsInRange(SelectedTarget target)
        {
            return target.IsValid;
        }

        public bool TryAttack(SelectedTarget target)
        {
            IDamageable targetDamageable = damageableProvider?.Invoke(target);
            if (!CanAttack(target, targetDamageable))
            {
                return false;
            }

            ProjectileLaunchData launchData = new(
                config.ProjectilePrefab,
                target.Position,
                targetDamageable,
                DamageRangeRoller.Roll(config.Damage),
                sourceProvider?.Invoke(),
                CreateBuildupData());

            if (!projectileLauncher.Launch(launchData))
            {
                return false;
            }

            remainingCooldown = config.AttackInterval;
            return true;
        }

        private bool CanAttack(SelectedTarget target, IDamageable targetDamageable)
        {
            return remainingCooldown <= 0f
                && target.IsValid
                && targetDamageable != null
                && config != null
                && config.ProjectilePrefab != null
                && lineOfSight != null
                && lineOfSight.HasLineOfSight(target.Position)
                && projectileLauncher != null;
        }

        private BuildupData CreateBuildupData()
        {
            return config.BuildupDefinition != null && config.BuildupAmount > 0f
                ? new BuildupData(config.BuildupDefinition, config.BuildupAmount)
                : null;
        }
    }
}
