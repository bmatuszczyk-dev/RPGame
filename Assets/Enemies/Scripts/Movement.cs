using System;
using RPGame.Core.Movement;
using RPGame.Core.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace RPGame.Enemies
{
    internal readonly struct MovementAgentState
    {
        public MovementAgentState(bool wasStopped, bool updatedPosition)
        {
            WasStopped = wasStopped;
            UpdatedPosition = updatedPosition;
        }

        public bool WasStopped { get; }
        public bool UpdatedPosition { get; }
    }

    internal readonly struct MovementCapsule
    {
        public MovementCapsule(Vector3 bottom, Vector3 top, float radius)
        {
            Bottom = bottom;
            Top = top;
            Radius = radius;
        }

        public Vector3 Bottom { get; }
        public Vector3 Top { get; }
        public float Radius { get; }
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Movement : MonoBehaviour, IEnemyMovement, IKnockbackable, IMovement, IPooledEnemyResettable
    {
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float destinationChangeThreshold = 0.05f;
        [SerializeField] private float traversalLeapSpeed = 6f;
        [SerializeField] private float traversalLeapArcHeight = 1f;

        private readonly MovementSpeedModifiers movementSpeedModifiers = new();
        private readonly KnockbackModifiers knockbackModifiers = new();

        private NavMeshAgent agent;
        private LeapMovement leap;
        private ChargeMovement charge;
        private KnockbackMovement knockback;
        private Vector3 lastDestination;
        private bool hasDestination;
        private int movementBlockCount;
        private float knockbackResistance;
        private int knockbackResistanceModifierId = -1;
        private int chargeKnockbackModifierId = -1;

        public Vector3 Position => transform.position;
        public bool IsLeaping => leap.IsActive;
        public bool IsCharging => charge.IsActive;
        public bool IsMovementBlocked => movementBlockCount > 0;

        private void Awake()
        {
            EnsureRuntimeMovements();
        }

        private void Start()
        {
            EnsureRuntimeMovements();
            CacheRequiredComponents();
            if (!HasRequiredComponents())
            {
                enabled = false;
                return;
            }

            ConfigureAgent();
        }

        private void Update()
        {
            leap.Tick(Time.deltaTime);
            charge.Tick(Time.deltaTime);
            knockback.Tick(Time.deltaTime);
            TryTraverseOffMeshLink();
        }

        public void MoveTo(Vector3 position)
        {
            if (HasActiveMovementOverride() || IsMovementBlocked || !CanUseAgent())
            {
                return;
            }

            ConfigureAgent();
            if (hasDestination && IsSameDestination(position))
            {
                agent.isStopped = false;
                return;
            }

            agent.isStopped = false;
            if (agent.SetDestination(position))
            {
                lastDestination = position;
                hasDestination = true;
            }
        }

        public void FaceTowards(Vector3 position)
        {
            if (charge.IsActive)
            {
                return;
            }

            Vector3 direction = position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction);
        }

        public void Stop()
        {
            if (HasActiveMovementOverride() || !CanUseAgent())
            {
                return;
            }

            agent.isStopped = true;
        }

        public bool TryResolvePosition(Vector3 desiredPosition, out Vector3 validPosition)
        {
            validPosition = default;
            if (agent == null
                || !NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, agent.height, agent.areaMask))
            {
                return false;
            }

            validPosition = hit.position;
            return true;
        }

        public bool TryLeapTo(Vector3 destination, float speed, float arcHeight)
        {
            if (!CanLeap()
                || !NavMesh.SamplePosition(destination, out NavMeshHit hit, agent.height, agent.areaMask))
            {
                return false;
            }

            leap.Start(hit.position, speed, arcHeight, agent.isOnOffMeshLink);
            return true;
        }

        public bool TryCombatLeapTo(
            Vector3 destination,
            float speed,
            float arcHeight,
            float fallSpeed,
            Action<bool> onCompleted)
        {
            if (!CanLeap()
                || !NavMesh.SamplePosition(destination, out NavMeshHit hit, agent.height, agent.areaMask))
            {
                return false;
            }

            leap.StartCombat(hit.position, speed, arcHeight, fallSpeed, onCompleted);
            return true;
        }

        public bool TryStartCharge(
            Vector3 destination,
            float speed,
            float maxDistance,
            float knockbackResistance,
            Action<Collider, Vector3> onCollision)
        {
            if (!CanStartCharge())
            {
                return false;
            }

            chargeKnockbackModifierId = knockbackModifiers.AddResistance(knockbackResistance);
            charge.Start(destination, speed, maxDistance, onCollision);
            return true;
        }

        internal void SetKnockbackResistance(float resistance)
        {
            knockbackResistance = Mathf.Clamp01(resistance);
            if (knockbackResistanceModifierId >= 0)
            {
                knockbackModifiers.Remove(knockbackResistanceModifierId);
            }

            knockbackResistanceModifierId = knockbackModifiers.AddResistance(knockbackResistance);
        }

        public void ApplyKnockback(
            Vector3 direction,
            float distance,
            float duration,
            Action<KnockbackEndContext> onEnded = null)
        {
            if (leap.IsActive)
            {
                return;
            }

            float multiplier = knockbackModifiers.Multiplier;
            if (charge.IsActive)
            {
                charge.Cancel();
            }

            float effectiveDistance = distance * multiplier;
            if (effectiveDistance <= Mathf.Epsilon)
            {
                return;
            }

            knockback.Start(
                direction,
                effectiveDistance,
                duration,
                (obstacle, collisionPoint) => onEnded?.Invoke(new KnockbackEndContext(
                    KnockbackEndReason.Collision,
                    obstacle,
                    collisionPoint)));
        }

        public void ResetForSpawn()
        {
            ResetRuntimeState(stopAgent: false);
        }

        public void ResetForDespawn()
        {
            ResetRuntimeState(stopAgent: true);
        }

        public void BlockMovement()
        {
            movementBlockCount++;
            charge.Cancel();
            leap.InterruptCombat();
            Stop();
        }

        public void UnblockMovement()
        {
            movementBlockCount = Mathf.Max(0, movementBlockCount - 1);
        }

        public int AddMovementSpeedModifier(float multiplier)
        {
            int modifierId = movementSpeedModifiers.Add(multiplier);
            ConfigureAgent();
            return modifierId;
        }

        public void RemoveMovementSpeedModifier(int modifierId)
        {
            if (movementSpeedModifiers.Remove(modifierId))
            {
                ConfigureAgent();
            }
        }

        private void TryTraverseOffMeshLink()
        {
            if (!CanLeap() || !agent.isOnOffMeshLink)
            {
                return;
            }

            TryLeapTo(
                agent.currentOffMeshLinkData.endPos,
                traversalLeapSpeed,
                traversalLeapArcHeight);
        }

        private bool CanLeap()
        {
            return !leap.IsActive
                && !knockback.IsActive
                && !IsMovementBlocked
                && CanUseAgent();
        }

        private bool HasActiveMovementOverride()
        {
            return leap.IsActive || charge.IsActive || knockback.IsActive;
        }

        private bool CanStartCharge()
        {
            return !leap.IsActive
                && !charge.IsActive
                && !knockback.IsActive
                && !IsMovementBlocked
                && CanUseAgent();
        }

        private MovementAgentState BeginManualMovement()
        {
            MovementAgentState state = new(agent.isStopped, agent.updatePosition);
            agent.isStopped = true;
            agent.updatePosition = false;
            return state;
        }

        private void FinishManualMovement(
            MovementAgentState state,
            Vector3 position,
            bool completeTraversal)
        {
            if (agent == null || !agent.enabled)
            {
                return;
            }

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, agent.height, agent.areaMask))
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
            }

            if (completeTraversal && agent.isOnOffMeshLink)
            {
                agent.CompleteOffMeshLink();
            }

            agent.updatePosition = state.UpdatedPosition;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = IsMovementBlocked || state.WasStopped;
            }
        }

        private bool TryResolveCombatLanding(Vector3 position, out Vector3 landingPosition)
        {
            landingPosition = default;
            if (agent == null
                || !NavMesh.SamplePosition(position, out NavMeshHit hit, agent.height, agent.areaMask)
                || hit.position.y > position.y)
            {
                return false;
            }

            landingPosition = hit.position;
            return true;
        }

        private void BeginKnockback()
        {
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }
        }

        private void EndKnockback()
        {
            hasDestination = false;
            if (agent == null)
            {
                return;
            }

            bool hasValidPosition = NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                agent.height,
                agent.areaMask);
            if (hasValidPosition)
            {
                transform.position = hit.position;
            }

            if (!agent.enabled && hasValidPosition)
            {
                agent.enabled = true;
                agent.Warp(hit.position);
            }

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = !IsMovementBlocked;
            }
        }

        private MovementCapsule GetMovementCapsule()
        {
            Vector3 center = transform.position + Vector3.up * (agent != null ? agent.height * 0.5f : 0.5f);
            float radius = agent != null ? agent.radius : 0.25f;
            float halfHeight = Mathf.Max(radius, (agent != null ? agent.height : 1f) * 0.5f);
            Vector3 bottom = center + Vector3.down * (halfHeight - radius);
            Vector3 top = center + Vector3.up * (halfHeight - radius);
            return new MovementCapsule(bottom, top, radius);
        }

        private bool ShouldIgnoreMovementCollider(Collider collider)
        {
            return collider.transform == transform
                || collider.transform.IsChildOf(transform)
                || collider.GetComponentInParent<Movement>() != null;
        }

        private void RemoveChargeKnockbackModifier()
        {
            if (chargeKnockbackModifierId < 0)
            {
                return;
            }

            knockbackModifiers.Remove(chargeKnockbackModifierId);
            chargeKnockbackModifierId = -1;
        }

        private void ResetRuntimeState(bool stopAgent)
        {
            CacheRequiredComponents();
            charge.Cancel();
            leap.Cancel();
            knockback.Cancel();
            RemoveChargeKnockbackModifier();
            hasDestination = false;
            lastDestination = default;
            movementBlockCount = 0;
            movementSpeedModifiers.Clear();
            knockbackModifiers.Clear();
            knockbackResistanceModifierId = knockbackModifiers.AddResistance(knockbackResistance);
            ConfigureAgent();
            ResetAgent(stopAgent);
        }

        private void EnsureRuntimeMovements()
        {
            if (leap == null)
            {
                leap = new LeapMovement(transform, BeginManualMovement, FinishManualMovement, TryResolveCombatLanding);
            }

            if (charge == null)
            {
                charge = new ChargeMovement(
                    transform,
                    BeginManualMovement,
                    FinishManualMovement,
                    GetMovementCapsule,
                    ShouldIgnoreMovementCollider,
                    RemoveChargeKnockbackModifier);
            }

            if (knockback == null)
            {
                knockback = new KnockbackMovement(
                    transform,
                    BeginKnockback,
                    EndKnockback,
                    GetMovementCapsule,
                    ShouldIgnoreMovementCollider);
            }
        }

        private void ResetAgent(bool stopAgent)
        {
            if (agent == null)
            {
                return;
            }

            if (!agent.enabled)
            {
                agent.enabled = true;
            }

            if (!CanUseAgent())
            {
                return;
            }

            agent.ResetPath();
            agent.Warp(transform.position);
            agent.isStopped = stopAgent;
        }

        private void ConfigureAgent()
        {
            if (agent == null)
            {
                return;
            }

            agent.speed = GetModifiedSpeed(moveSpeed);
            agent.autoTraverseOffMeshLink = false;
        }

        private void CacheRequiredComponents()
        {
            if (agent != null)
            {
                return;
            }
            agent = GetComponent<NavMeshAgent>();
        }

        private bool HasRequiredComponents()
        {
            if (agent != null)
            {
                return true;
            }

            Debug.LogError("Missing field agent.", this);
            return false;
        }

        private bool CanUseAgent()
        {
            return agent != null
                && agent.enabled
                && agent.gameObject.activeInHierarchy
                && agent.isOnNavMesh;
        }

        private float GetModifiedSpeed(float baseSpeed)
        {
            return baseSpeed * movementSpeedModifiers.Multiplier;
        }

        private bool IsSameDestination(Vector3 position)
        {
            float thresholdSqr = destinationChangeThreshold * destinationChangeThreshold;
            return (position - lastDestination).sqrMagnitude <= thresholdSqr;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            destinationChangeThreshold = Mathf.Max(0f, destinationChangeThreshold);
            traversalLeapSpeed = Mathf.Max(0.01f, traversalLeapSpeed);
            traversalLeapArcHeight = Mathf.Max(0f, traversalLeapArcHeight);
            ConfigureAgent();
        }
    }
}
