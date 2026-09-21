using System.Collections.Generic;
using RPGame.Core.Statuses;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class TriggerStatusZoneBehaviour : MonoBehaviour, IZoneBehaviour
    {
        [SerializeField] private List<ZoneParticleEffect> particleEffects = new();

        private readonly Dictionary<IStatusReceiver, TargetZoneState> targetsInside = new();
        private SphereCollider zoneCollider;
        private Rigidbody zoneRigidbody;
        private CasterData casterData;
        private bool isActive;

        protected abstract float ReapplyInterval { get; }

        protected CasterData CasterData => casterData;
        protected GameObject StatusSource => casterData.CasterObject;

        protected abstract void ApplyInitialTo(IStatusReceiver target, GameObject targetObject);
        protected abstract void ReapplyTo(IStatusReceiver target);

        private void Awake()
        {
            ResolveComponents();
        }

        public void Initialize(CasterData casterData, float radius)
        {
            this.casterData = casterData;
            ResolveComponents();

            zoneCollider.isTrigger = true;
            zoneCollider.radius = Mathf.Max(0f, radius);
            zoneCollider.enabled = false;

            zoneRigidbody.useGravity = false;
            zoneRigidbody.isKinematic = true;

            ConfigureParticleEffects(radius);
            isActive = false;
            targetsInside.Clear();
        }

        public void Activate()
        {
            zoneCollider.enabled = true;
            isActive = true;
            PlayParticleEffects();
        }

        public void Deactivate()
        {
            isActive = false;
            if (zoneCollider != null)
            {
                zoneCollider.enabled = false;
            }

            targetsInside.Clear();
            StopParticleEffects();
        }

        private void Update()
        {
            if (!isActive || ReapplyInterval <= 0f)
            {
                return;
            }

            foreach (TargetZoneState targetState in targetsInside.Values)
            {
                targetState.ReapplyTimer += Time.deltaTime;
                if (targetState.ReapplyTimer < ReapplyInterval)
                {
                    continue;
                }

                targetState.ReapplyTimer = 0f;
                ReapplyTo(targetState.StatusReceiver);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive || IsCasterCollider(other))
            {
                return;
            }

            IStatusReceiver statusReceiver = other.GetComponentInParent<IStatusReceiver>();
            if (statusReceiver == null)
            {
                return;
            }

            if (targetsInside.TryGetValue(statusReceiver, out TargetZoneState targetState))
            {
                targetState.Colliders.Add(other);
                return;
            }

            GameObject targetObject = ((Component)statusReceiver).gameObject;
            targetState = new TargetZoneState(statusReceiver);
            targetState.Colliders.Add(other);
            targetsInside.Add(statusReceiver, targetState);
            ApplyInitialTo(statusReceiver, targetObject);
        }

        private void OnTriggerExit(Collider other)
        {
            IStatusReceiver statusReceiver = other.GetComponentInParent<IStatusReceiver>();
            if (statusReceiver == null)
            {
                return;
            }

            if (!targetsInside.TryGetValue(statusReceiver, out TargetZoneState targetState))
            {
                return;
            }

            targetState.Colliders.Remove(other);
            if (targetState.Colliders.Count == 0)
            {
                targetsInside.Remove(statusReceiver);
            }
        }

        private bool IsCasterCollider(Collider other)
        {
            GameObject casterObject = casterData.CasterObject;
            return casterObject != null
                && other != null
                && (other.gameObject == casterObject || other.transform.IsChildOf(casterObject.transform));
        }

        private void ResolveComponents()
        {
            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<SphereCollider>();
            }

            if (zoneRigidbody == null)
            {
                zoneRigidbody = GetComponent<Rigidbody>();
            }
        }

        private void ConfigureParticleEffects(float radius)
        {
            float zoneRadius = Mathf.Max(0f, radius);
            for (int i = 0; i < particleEffects.Count; i++)
            {
                ZoneParticleEffect effect = particleEffects[i];
                ParticleSystem particleSystem = effect?.ParticleSystem;
                if (particleSystem == null)
                {
                    continue;
                }

                if (effect.MatchEmissionRadius)
                {
                    ParticleSystem.ShapeModule shape = particleSystem.shape;
                    if (shape.enabled)
                    {
                        shape.radius = zoneRadius * effect.EmissionRadiusMultiplier;
                    }
                }

                if (effect.MatchParticleSize)
                {
                    ParticleSystem.MainModule main = particleSystem.main;
                    main.startSize = zoneRadius * 2f * effect.ParticleSizeMultiplier;
                }
            }
        }

        private void PlayParticleEffects()
        {
            for (int i = 0; i < particleEffects.Count; i++)
            {
                ParticleSystem particleSystem = particleEffects[i]?.ParticleSystem;
                if (particleSystem != null && !particleSystem.isPlaying)
                {
                    particleSystem.Play();
                }
            }
        }

        private void StopParticleEffects()
        {
            for (int i = 0; i < particleEffects.Count; i++)
            {
                ParticleSystem particleSystem = particleEffects[i]?.ParticleSystem;
                if (particleSystem != null)
                {
                    particleSystem.Stop();
                }
            }
        }

        private sealed class TargetZoneState
        {
            public TargetZoneState(IStatusReceiver statusReceiver)
            {
                StatusReceiver = statusReceiver;
            }

            public IStatusReceiver StatusReceiver { get; }
            public HashSet<Collider> Colliders { get; } = new();
            public float ReapplyTimer { get; set; }
        }
    }
}
