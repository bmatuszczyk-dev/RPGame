using System;
using RPGame.Core.Damage;
using RPGame.Core.Pooling;
using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class MeleeAttack : IEnemyAttack, IPooledEnemyResettable
    {
        private readonly MeleeAttackConfig config;
        private readonly Func<Vector3> attackerPositionProvider;
        private readonly Func<SelectedTarget, IDamageable> damageableProvider;
        private readonly Func<GameObject> sourceProvider;
        private float remainingCooldown;
        private float remainingWindup;
        private Action pendingAttackCompletion;

        public MeleeAttack(
            MeleeAttackConfig config,
            Func<Vector3> attackerPositionProvider,
            Func<SelectedTarget, IDamageable> damageableProvider = null,
            Func<GameObject> sourceProvider = null)
        {
            this.config = config;
            this.attackerPositionProvider = attackerPositionProvider;
            this.damageableProvider = damageableProvider;
            this.sourceProvider = sourceProvider;
        }

        public float Range => config.AttackRange;

        public void Tick(float deltaTime)
        {
            remainingCooldown -= deltaTime;

            if (pendingAttackCompletion == null)
            {
                return;
            }

            remainingWindup -= deltaTime;
            if (remainingWindup <= 0f)
            {
                Action completeAttack = pendingAttackCompletion;
                ClearWindup();
                completeAttack.Invoke();
            }
        }

        public void ResetForSpawn()
        {
            ResetRuntimeState();
        }

        public void ResetForDespawn()
        {
            ResetRuntimeState();
        }

        public bool IsInRange(SelectedTarget target)
        {
            if (!target.IsValid)
            {
                return false;
            }

            Vector3 targetPosition = target.Targetable.TargetPoint != null
                ? target.Targetable.TargetPoint.position
                : target.Position;
            float attackRangeSqr = Range * Range;
            return (targetPosition - attackerPositionProvider()).sqrMagnitude <= attackRangeSqr;
        }

        public bool TryAttack(SelectedTarget target)
        {
            return TryAttack(target, damageableProvider?.Invoke(target), sourceProvider?.Invoke());
        }

        internal bool TryAttack(SelectedTarget target, IDamageable damageable, GameObject source)
        {
            if (!CanAttack(target, damageable))
            {
                return false;
            }

            remainingWindup = config.WindupDuration;

            if (remainingWindup <= 0f)
            {
                CompleteAttackAttempt(target, damageable, source);
                return true;
            }

            pendingAttackCompletion = () => CompleteAttackAttempt(target, damageable, source);
            return true;
        }

        private bool CanAttack(SelectedTarget target, IDamageable damageable)
        {
            return pendingAttackCompletion == null
                && remainingCooldown <= 0f
                && IsInRange(target)
                && damageable != null
                && damageable.CanReceiveDamage;
        }

        private void CompleteAttackAttempt(SelectedTarget target, IDamageable damageable, GameObject source)
        {
            if (IsInRange(target) && damageable != null && damageable.CanReceiveDamage)
            {
                damageable.ApplyDamage(new DamageData(DamageRangeRoller.Roll(config.Damage), source));
            }

            remainingCooldown = config.AttackInterval;
        }

        private void ResetRuntimeState()
        {
            remainingCooldown = 0f;
            ClearWindup();
        }

        private void ClearWindup()
        {
            remainingWindup = 0f;
            pendingAttackCompletion = null;
        }
    }
}
