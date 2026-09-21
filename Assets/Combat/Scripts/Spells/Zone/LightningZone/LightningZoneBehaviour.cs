using System.Collections;
using System.Collections.Generic;
using RPGame.Core.Statuses;
using RPGame.Core.Spells;
using UnityEngine;
using UnityEngine.Serialization;

namespace RPGame.Combat.Spells
{
    public sealed class LightningZoneBehaviour : MonoBehaviour, IZoneBehaviour
    {
        [FormerlySerializedAs("stunEffect")]
        [SerializeField] private StunStatusDefinition stunStatus;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private GameObject lightningVfxPrefab;
        [SerializeField] private Vector3 lightningVfxOffset;
        [SerializeField] private Vector2 strikeDelayRange = new(0f, 0.2f);

        private readonly HashSet<IStatusReceiver> hitReceivers = new();
        private CasterData casterData;
        private float radius;

        public void Initialize(CasterData casterData, float radius)
        {
            this.casterData = casterData;
            this.radius = Mathf.Max(0f, radius);
            hitReceivers.Clear();
        }

        public void Activate()
        {
            hitReceivers.Clear();
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider zoneCollider = colliders[i];
                if (IsCasterCollider(zoneCollider))
                {
                    continue;
                }

                IStatusReceiver statusReceiver =
                    zoneCollider.GetComponentInParent<IStatusReceiver>();
                if (statusReceiver == null || !hitReceivers.Add(statusReceiver))
                {
                    continue;
                }

                float delay = Random.Range(strikeDelayRange.x, strikeDelayRange.y);
                GameObject targetObject = statusReceiver is Component receiverComponent
                    ? receiverComponent.gameObject
                    : zoneCollider.gameObject;
                DelayedLightningStrike.Run(
                    stunStatus,
                    stunDuration,
                    casterData,
                    targetObject,
                    lightningVfxPrefab,
                    zoneCollider.bounds.center + lightningVfxOffset,
                    delay);
            }
        }

        public void Deactivate()
        {
            hitReceivers.Clear();
        }

        private bool IsCasterCollider(Collider zoneCollider)
        {
            GameObject casterObject = casterData.CasterObject;
            return casterObject != null
                && zoneCollider != null
                && (zoneCollider.gameObject == casterObject
                    || zoneCollider.transform.IsChildOf(casterObject.transform));
        }

        private void OnValidate()
        {
            stunDuration = Mathf.Max(0f, stunDuration);
            strikeDelayRange.x = Mathf.Max(0f, strikeDelayRange.x);
            strikeDelayRange.y = Mathf.Max(strikeDelayRange.x, strikeDelayRange.y);
        }
    }

    internal sealed class DelayedLightningStrike : MonoBehaviour
    {
        private StunStatusDefinition stunStatus;
        private float stunDuration;
        private CasterData casterData;
        private GameObject targetObject;
        private GameObject lightningVfxPrefab;
        private Vector3 strikePosition;
        private float delay;

        public static void Run(
            StunStatusDefinition stunStatus,
            float stunDuration,
            CasterData casterData,
            GameObject targetObject,
            GameObject lightningVfxPrefab,
            Vector3 strikePosition,
            float delay)
        {
            GameObject runnerObject = new($"{nameof(DelayedLightningStrike)}");
            DelayedLightningStrike runner = runnerObject.AddComponent<DelayedLightningStrike>();
            runner.stunStatus = stunStatus;
            runner.stunDuration = stunDuration;
            runner.casterData = casterData;
            runner.targetObject = targetObject;
            runner.lightningVfxPrefab = lightningVfxPrefab;
            runner.strikePosition = strikePosition;
            runner.delay = Mathf.Max(0f, delay);
        }

        private IEnumerator Start()
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            SpellBehaviorPipelineExecutor.Resolve(
                casterData,
                targetObject,
                new LightningZoneResolveBehavior(stunStatus, stunDuration, casterData.CasterObject));

            if (lightningVfxPrefab != null)
            {
                Instantiate(lightningVfxPrefab, strikePosition, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        private sealed class LightningZoneResolveBehavior : ISpellResolveBehavior
        {
            private readonly StunStatusDefinition stunStatus;
            private readonly float stunDuration;
            private readonly GameObject statusSource;

            public LightningZoneResolveBehavior(
                StunStatusDefinition stunStatus,
                float stunDuration,
                GameObject statusSource)
            {
                this.stunStatus = stunStatus;
                this.stunDuration = stunDuration;
                this.statusSource = statusSource;
            }

            public SpellBehaviorPhase Phase => SpellBehaviorPhase.Resolve;

            public bool Resolve(SpellBehaviorContext context)
            {
                if (stunStatus == null || context.StatusReceiver == null)
                {
                    return false;
                }

                return context.StatusReceiver.ApplyStatus(
                    stunStatus,
                    stunDuration,
                    new StatusContext(
                        new StatusSourceId(nameof(LightningZoneBehaviour)),
                        statusSource));
            }
        }
    }
}
