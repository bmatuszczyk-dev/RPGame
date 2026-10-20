using System;
using RPGame.Core.Damage;
using UnityEngine;

namespace RPGame.Enemies
{
    internal enum ChargeState
    {
        Ready,
        Windup,
        Charging,
        Recovery,
        Cooldown
    }

    internal sealed class Charge
    {
        private readonly ChargingMeleeEnemyBehaviourConfig config;
        private readonly IEnemyMovement movement;
        private readonly GameObject source;

        private ChargeState state;
        private float stateTimer;
        private float triggerCheckTimer;
        private IDamageable targetDamageable;
        private bool hasAppliedDamage;

        internal ChargeState State => state;
        internal bool HasControl => state == ChargeState.Windup
            || state == ChargeState.Charging
            || state == ChargeState.Recovery;

        public Charge(
            ChargingMeleeEnemyBehaviourConfig config,
            IEnemyMovement movement,
            GameObject source)
        {
            this.config = config;
            this.movement = movement;
            this.source = source;
        }

        public void Tick(float deltaTime, SelectedTarget target)
        {
            switch (state)
            {
                case ChargeState.Ready:
                    TryStartWindup(deltaTime, target);
                    break;

                case ChargeState.Windup:
                    TickWindup(deltaTime, target);
                    break;

                case ChargeState.Charging:
                    if (!movement.IsCharging)
                    {
                        EnterRecovery();
                    }
                    break;

                case ChargeState.Recovery:
                    TickRecovery(deltaTime);
                    break;

                case ChargeState.Cooldown:
                    TickCooldown(deltaTime);
                    break;
            }
        }

        private void TryStartWindup(float deltaTime, SelectedTarget target)
        {
            if (!target.IsValid || !IsWithinTriggerDistance(target.Position))
            {
                triggerCheckTimer = 0f;
                return;
            }

            triggerCheckTimer -= deltaTime;
            if (triggerCheckTimer > 0f)
            {
                return;
            }

            triggerCheckTimer = config.TriggerCheckInterval;
            if (UnityEngine.Random.value > config.TriggerChance)
            {
                return;
            }

            state = ChargeState.Windup;
            stateTimer = config.WindupDuration;
            movement.Stop();
        }

        private void TickWindup(float deltaTime, SelectedTarget target)
        {
            if (movement.IsMovementBlocked || !target.IsValid)
            {
                EnterRecovery();
                return;
            }

            movement.Stop();
            movement.FaceTowards(target.Position);
            stateTimer -= deltaTime;
            if (stateTimer > 0f)
            {
                return;
            }

            targetDamageable = GetDamageable(target);
            hasAppliedDamage = false;
            if (!movement.TryStartCharge(
                    target.Position,
                    config.ChargeSpeed,
                    config.MaxDistance,
                    config.KnockbackResistance,
                    HandleChargeCollision))
            {
                EnterRecovery();
                return;
            }

            state = ChargeState.Charging;
        }

        private void TickRecovery(float deltaTime)
        {
            movement.Stop();
            stateTimer -= deltaTime;
            if (stateTimer <= 0f)
            {
                state = ChargeState.Cooldown;
                stateTimer = config.Cooldown;
            }
        }

        private void TickCooldown(float deltaTime)
        {
            stateTimer -= deltaTime;
            if (stateTimer <= 0f)
            {
                state = ChargeState.Ready;
                triggerCheckTimer = 0f;
            }
        }

        private bool IsWithinTriggerDistance(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - movement.Position;
            direction.y = 0f;
            float distance = direction.magnitude;
            return distance >= config.MinTriggerDistance && distance <= config.MaxTriggerDistance;
        }

        private void HandleChargeCollision(Collider collider, Vector3 _)
        {
            if (hasAppliedDamage || targetDamageable == null)
            {
                return;
            }

            IDamageable hitDamageable = collider.GetComponentInParent<IDamageable>();
            if (!ReferenceEquals(hitDamageable, targetDamageable))
            {
                return;
            }

            hasAppliedDamage = true;
            if (hitDamageable.CanReceiveDamage)
            {
                hitDamageable.ApplyDamage(new DamageData(DamageRangeRoller.Roll(config.ChargeDamage), source));
            }
        }

        private static IDamageable GetDamageable(SelectedTarget target)
        {
            return target.IsValid && target.Targetable.TargetPoint != null
                ? target.Targetable.TargetPoint.GetComponentInParent<IDamageable>()
                : null;
        }

        private void EnterRecovery()
        {
            state = ChargeState.Recovery;
            stateTimer = config.RecoveryDuration;
            targetDamageable = null;
            movement.Stop();
        }
    }
}
