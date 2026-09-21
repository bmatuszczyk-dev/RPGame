using System.Collections.Generic;
using RPGame.Core.Damage;
using RPGame.Core.Movement;
using RPGame.Core.Spells;
using UnityEngine;
using UnityEngine.Serialization;

namespace RPGame.Combat.Spells
{
    public sealed class WaveController : MonoBehaviour
    {
        [FormerlySerializedAs("particleSystem")]
        [SerializeField] private ParticleSystem waveParticleSystem;
        [SerializeField, Min(1)] private int particleCount = 40;
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        private readonly Collider[] overlapBuffer = new Collider[64];
        private readonly HashSet<GameObject> hitTargets = new();

        private Vector3 origin;
        private Vector3 forward;
        private float range;
        private float angle;
        private float propagationSpeed;
        private float knockbackDistance;
        private float knockbackDuration;
        private CasterData casterData;
        private float previousFrontDistance;
        private float frontDistance;
        private bool isInitialized;

        public void Initialize(
            Vector3 origin,
            Vector3 forward,
            float range,
            float angle,
            float propagationSpeed,
            float knockbackDistance,
            float knockbackDuration,
            CasterData casterData)
        {
            this.origin = origin;

            Vector3 planarForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            this.forward = planarForward.sqrMagnitude > Mathf.Epsilon
                ? planarForward.normalized
                : Vector3.forward;

            this.range = Mathf.Max(0f, range);
            this.angle = Mathf.Clamp(angle, 0f, 360f);
            this.propagationSpeed = Mathf.Max(0f, propagationSpeed);
            this.knockbackDistance = Mathf.Max(0f, knockbackDistance);
            this.knockbackDuration = Mathf.Max(0f, knockbackDuration);
            this.casterData = casterData;

            previousFrontDistance = 0f;
            frontDistance = 0f;

            transform.SetPositionAndRotation(
                origin,
                Quaternion.LookRotation(this.forward, Vector3.up));

            if (this.range <= 0f || this.propagationSpeed <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            ConfigureParticleSystem();
            EmitWaveParticles();

            isInitialized = true;
        }

        private void ConfigureParticleSystem()
        {
            if (waveParticleSystem == null)
            {
                return;
            }

            ParticleSystem.MainModule main = waveParticleSystem.main;

            // Kierunek i prędkość ustawiamy ręcznie per particle.
            main.startSpeed = 0f;
            main.startLifetime = propagationSpeed > 0f
                ? range / propagationSpeed
                : 0f;

            // Używamy world space, żeby velocity odpowiadało bezpośrednio
            // kierunkom wyliczonym na podstawie Wave.forward.
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Wyłącz automatyczną emisję i Shape.
            ParticleSystem.EmissionModule emission = waveParticleSystem.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = waveParticleSystem.shape;
            shape.enabled = false;

            waveParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void EmitWaveParticles()
        {
            if (waveParticleSystem == null
                || particleCount <= 0
                || propagationSpeed <= 0f
                || range <= 0f)
            {
                return;
            }

            float halfAngle = angle * 0.5f;
            float lifetime = range / propagationSpeed;

            for (int i = 0; i < particleCount; i++)
            {
                float t = particleCount == 1
                    ? 0.5f
                    : i / (float)(particleCount - 1);

                float yaw = Mathf.Lerp(-halfAngle, halfAngle, t);

                Vector3 direction =
                    Quaternion.AngleAxis(yaw, Vector3.up) * forward;

                ParticleSystem.EmitParams emitParams = new()
                {
                    position = origin,
                    velocity = direction * propagationSpeed,
                    startLifetime = lifetime
                };

                waveParticleSystem.Emit(emitParams, 1);
            }
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            previousFrontDistance = frontDistance;
            frontDistance = Mathf.Min(
                range,
                frontDistance + propagationSpeed * Time.deltaTime);

            CheckForTargets();

            if (frontDistance >= range)
            {
                Destroy(gameObject);
            }
        }

        private void CheckForTargets()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                frontDistance,
                overlapBuffer,
                hitLayers,
                triggerInteraction);

            float halfAngleCosine =
                Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);

            for (int i = 0; i < hitCount; i++)
            {
                Collider targetCollider = overlapBuffer[i];

                if (targetCollider == null || ShouldIgnore(targetCollider))
                {
                    continue;
                }

                IDamageable damageable =
                    targetCollider.GetComponentInParent<IDamageable>();

                if (damageable == null)
                {
                    continue;
                }

                Component targetComponent = (Component)damageable;
                GameObject targetObject = targetComponent.gameObject;

                if (hitTargets.Contains(targetObject))
                {
                    continue;
                }

                Vector3 targetPosition = targetComponent.transform.position;

                Vector3 offset = targetPosition - origin;
                offset.y = 0f;

                float distance = offset.magnitude;

                if (distance < previousFrontDistance
                    || distance > frontDistance
                    || distance > range)
                {
                    continue;
                }

                if (distance > Mathf.Epsilon
                    && Vector3.Dot(forward, offset / distance) < halfAngleCosine)
                {
                    continue;
                }

                hitTargets.Add(targetObject);

                SpellBehaviorPipelineExecutor.Resolve(
                    casterData,
                    targetObject,
                    new WaveResolveBehavior(
                        damageable,
                        targetCollider,
                        targetPosition,
                        targetObject,
                        this));
            }
        }

        private void ApplyKnockback(
            Collider targetCollider,
            Vector3 targetPosition,
            GameObject targetObject)
        {
            Vector3 direction = targetPosition - origin;
            direction.y = 0f;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = forward;
            }
            else
            {
                direction.Normalize();
            }

            IKnockbackable knockbackable =
                targetCollider.GetComponentInParent<IKnockbackable>();

            knockbackable?.ApplyKnockback(
                direction,
                knockbackDistance,
                knockbackDuration,
                (obstacle, point) => KnockbackCollisionDispatcher.Dispatch(
                    casterData.RuntimeBehaviors,
                    targetObject,
                    obstacle,
                    point));
        }

        private sealed class WaveResolveBehavior : ISpellResolveBehavior
        {
            private readonly IDamageable damageable;
            private readonly Collider targetCollider;
            private readonly Vector3 targetPosition;
            private readonly GameObject targetObject;
            private readonly WaveController wave;

            public WaveResolveBehavior(
                IDamageable damageable,
                Collider targetCollider,
                Vector3 targetPosition,
                GameObject targetObject,
                WaveController wave)
            {
                this.damageable = damageable;
                this.targetCollider = targetCollider;
                this.targetPosition = targetPosition;
                this.targetObject = targetObject;
                this.wave = wave;
            }

            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;

            public bool Resolve(SpellBehaviorContext context)
            {
                DamageResult result = damageable.ApplyDamage(
                    new DamageData(
                        DamageRangeRoller.Roll(wave.casterData.DamageRanges),
                        wave.casterData.CasterObject));

                wave.ApplyKnockback(targetCollider, targetPosition, targetObject);
                return result.WasApplied;
            }
        }

        private bool ShouldIgnore(Collider targetCollider)
        {
            GameObject targetObject = targetCollider.gameObject;
            GameObject casterObject = casterData.CasterObject;

            return targetObject == gameObject
                || targetObject.transform.IsChildOf(transform)
                || (casterObject != null
                    && (targetObject == casterObject
                        || targetObject.transform.IsChildOf(casterObject.transform)));
        }
    }
}
