using System.Collections.Generic;
using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class HealerEnemyBehaviour : IEnemyBehaviour
    {
        private const float MinDirectionSqrMagnitude = 0.0001f;

        private readonly IEnemyDetection detection;
        private readonly IEnemyMovement movement;
        private readonly IHealerTargetProvider targetProvider;
        private readonly IEnemyLineOfSight lineOfSight;
        private readonly IStatisticsController statistics;
        private readonly HealerEnemyBehaviourConfig config;
        private readonly EnemyTargetable owner;
        private readonly ContinuousHeal continuousHeal;

        public bool IsHealing { get; private set; }
        public HealerTarget CurrentHealTarget { get; private set; }

        public HealerEnemyBehaviour(
            IEnemyDetection detection,
            IEnemyMovement movement,
            IHealerTargetProvider targetProvider,
            IEnemyLineOfSight lineOfSight,
            IStatisticsController statistics,
            HealerEnemyBehaviourConfig config,
            EnemyTargetable owner)
        {
            this.detection = detection;
            this.movement = movement;
            this.targetProvider = targetProvider;
            this.lineOfSight = lineOfSight;
            this.statistics = statistics;
            this.config = config;
            this.owner = owner;
            continuousHeal = new ContinuousHeal(config.HealPerSecond, config.ManaCostPerSecond);
        }

        public void Tick(float deltaTime)
        {
            IsHealing = false;
            if (!IsCurrentTargetAvailable())
            {
                CurrentHealTarget = SelectTarget();
            }

            if (CurrentHealTarget == null)
            {
                RetreatOrIdle();
                return;
            }

            movement.FaceTowards(CurrentHealTarget.Position);
            if (Vector3.Distance(movement.Position, CurrentHealTarget.Position) > config.HealRange)
            {
                MaintainHealDistance(CurrentHealTarget.Position);
                return;
            }

            if (lineOfSight == null || !lineOfSight.HasLineOfSight(CurrentHealTarget.Position))
            {
                movement.MoveTo(CurrentHealTarget.Position);
                return;
            }

            MaintainHealDistance(CurrentHealTarget.Position);
            IsHealing = continuousHeal.TryHeal(statistics, CurrentHealTarget, deltaTime);
        }

        private void MaintainHealDistance(Vector3 targetPosition)
        {
            Vector3 direction = movement.Position - targetPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= MinDirectionSqrMagnitude)
            {
                movement.Stop();
                return;
            }

            Vector3 desiredPosition = targetPosition + direction.normalized * config.HealRange;
            if (!movement.TryResolvePosition(desiredPosition, out Vector3 validPosition))
            {
                movement.Stop();
                return;
            }

            movement.MoveTo(validPosition);
        }

        private bool IsCurrentTargetAvailable()
        {
            if (!IsCandidate(CurrentHealTarget))
            {
                return false;
            }

            IReadOnlyList<HealerTarget> targets = targetProvider.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (ReferenceEquals(targets[i], CurrentHealTarget))
                {
                    return true;
                }
            }

            return false;
        }

        private HealerTarget SelectTarget()
        {
            HealerTarget selectedTarget = null;
            float lowestHealthNormalized = float.MaxValue;
            IReadOnlyList<HealerTarget> targets = targetProvider.Targets;

            for (int i = 0; i < targets.Count; i++)
            {
                HealerTarget candidate = targets[i];
                if (!IsCandidate(candidate) || candidate.Statistics.HealthNormalized >= lowestHealthNormalized)
                {
                    continue;
                }

                lowestHealthNormalized = candidate.Statistics.HealthNormalized;
                selectedTarget = candidate;
            }

            return selectedTarget;
        }

        private bool IsCandidate(HealerTarget target)
        {
            return target != null
                && target.IsValid
                && target.Targetable != owner
                && target.Statistics.CurrentHealth < target.Statistics.MaxHealth;
        }

        private void RetreatOrIdle()
        {
            if (!detection.TryGetTarget(out SelectedTarget playerTarget)
                || !playerTarget.IsValid
                || Vector3.Distance(movement.Position, playerTarget.Position) >= config.PlayerAvoidanceRange)
            {
                movement.Stop();
                return;
            }

            Vector3 direction = movement.Position - playerTarget.Position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= MinDirectionSqrMagnitude)
            {
                movement.Stop();
                return;
            }

            Vector3 desiredPosition = movement.Position + direction.normalized * config.PlayerAvoidanceRange;
            if (!movement.TryResolvePosition(desiredPosition, out Vector3 validPosition))
            {
                movement.Stop();
                return;
            }

            movement.MoveTo(validPosition);
        }
    }
}
