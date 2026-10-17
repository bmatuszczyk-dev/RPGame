using System;
using RPGame.Core.Damage;
using RPGame.Core.Pooling;
using UnityEngine;

namespace RPGame.Enemies
{
    public enum AssasinSlashState
    {
        Idle,
        Windup,
        Slash,
        Recovery
    }

    public sealed class AssasinSlashAttack : IEnemyAttack, IPooledEnemyResettable
    {
        private const int HitBufferSize = 16;

        private readonly AssasinSlashAttackConfig config;
        private readonly Transform attackerTransform;
        private readonly Func<SelectedTarget, IDamageable> damageableProvider;
        private readonly Func<GameObject> sourceProvider;
        private readonly AssasinSlashView view;
        private readonly Collider[] hitBuffer = new Collider[HitBufferSize];

        private IDamageable targetDamageable;
        private Vector3 lockedForward;
        private float stateElapsed;
        private bool hitResolved;

        public AssasinSlashState State { get; private set; }
        public bool IsActive => State != AssasinSlashState.Idle;
        public float Range => 0f;

        public AssasinSlashAttack(
            AssasinSlashAttackConfig config,
            Transform attackerTransform,
            Func<SelectedTarget, IDamageable> damageableProvider,
            Func<GameObject> sourceProvider,
            AssasinSlashView view)
        {
            this.config = config;
            this.attackerTransform = attackerTransform;
            this.damageableProvider = damageableProvider;
            this.sourceProvider = sourceProvider;
            this.view = view;
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            view.Tick(deltaTime);
            stateElapsed += deltaTime;
            switch (State)
            {
                case AssasinSlashState.Windup:
                    if (stateElapsed >= config.WindupDuration)
                    {
                        EnterSlash();
                    }
                    break;

                case AssasinSlashState.Slash:
                    float slashProgress = GetNormalizedTime(config.SlashDuration);
                    if (!hitResolved && slashProgress >= config.HitTimeNormalized)
                    {
                        ResolveHit();
                    }

                    if (stateElapsed >= config.SlashDuration)
                    {
                        EnterRecovery();
                    }
                    break;

                case AssasinSlashState.Recovery:
                    if (stateElapsed >= config.RecoveryDuration && !view.IsPlaying)
                    {
                        Reset();
                    }
                    break;
            }
        }

        public bool IsInRange(SelectedTarget selectedTarget)
        {
            return selectedTarget.IsValid;
        }

        public bool TryAttack(SelectedTarget selectedTarget)
        {
            IDamageable damageable = damageableProvider?.Invoke(selectedTarget);
            if (IsActive
                || !selectedTarget.IsValid
                || damageable == null
                || !damageable.CanReceiveDamage)
            {
                return false;
            }

            targetDamageable = damageable;
            Vector3 direction = selectedTarget.Position - attackerTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                attackerTransform.rotation = Quaternion.LookRotation(direction);
            }

            lockedForward = attackerTransform.forward;
            stateElapsed = 0f;
            hitResolved = false;
            State = AssasinSlashState.Windup;
            view.Play();
            return true;
        }

        public void ResetForSpawn()
        {
            Reset();
        }

        public void ResetForDespawn()
        {
            Reset();
        }

        private void EnterSlash()
        {
            State = AssasinSlashState.Slash;
            stateElapsed = 0f;
            if (config.HitTimeNormalized <= 0f)
            {
                ResolveHit();
            }
        }

        private void EnterRecovery()
        {
            State = AssasinSlashState.Recovery;
            stateElapsed = 0f;
        }

        private void ResolveHit()
        {
            hitResolved = true;
            if (!view.TryGetHitbox(out Vector3 localCenter, out Vector3 hitboxSize))
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(lockedForward);
            Vector3 center = attackerTransform.position + rotation * localCenter;
            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                hitboxSize * 0.5f,
                hitBuffer,
                rotation,
                config.HitLayers,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = hitBuffer[i];
                if (hitCollider == null
                    || hitCollider.transform == attackerTransform
                    || hitCollider.transform.IsChildOf(attackerTransform))
                {
                    continue;
                }

                IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
                if (damageable != targetDamageable)
                {
                    continue;
                }

                targetDamageable.ApplyDamage(new DamageData(DamageRangeRoller.Roll(config.Damage), sourceProvider?.Invoke()));
                break;
            }
        }

        private float GetNormalizedTime(float duration)
        {
            return duration > Mathf.Epsilon ? Mathf.Clamp01(stateElapsed / duration) : 1f;
        }

        private void Reset()
        {
            State = AssasinSlashState.Idle;
            targetDamageable = null;
            stateElapsed = 0f;
            hitResolved = false;
            view.Stop();
        }
    }
}
