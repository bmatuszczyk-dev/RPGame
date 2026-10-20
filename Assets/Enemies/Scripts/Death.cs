using System;
using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using RPGame.Core.Pooling;
using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class Death : MonoBehaviour, IPooledEnemyResettable
    {
        [SerializeField] private StatisticsController deathSource;
        [SerializeField] private Movement movement;
        [SerializeField] private Controller controller;
        [SerializeField] private Detection detection;
        [SerializeField] private EnemyTargetable targetable;

        private StatisticsController subscribedDeathSource;

        internal bool IsDead { get; private set; }
        public event Action OnDeathCleanupEnd;

        private void Start()
        {
            CacheRequiredComponents();
            if (!HasRequiredComponents())
            {
                enabled = false;
                return;
            }

            SubscribeDeathSource();
        }

        private void OnDisable()
        {
            UnsubscribeDeathSource();
        }

        public void ResetForSpawn()
        {
            CacheRequiredComponents();
            IsDead = false;

            if (movement != null)
            {
                movement.enabled = true;
            }

            if (controller != null)
            {
                controller.enabled = true;
            }

            if (detection != null)
            {
                detection.enabled = true;
            }

            if (targetable != null)
            {
                targetable.enabled = true;
            }

            if (HasRequiredComponents())
            {
                SubscribeDeathSource();
            }
        }

        public void ResetForDespawn()
        {
            UnsubscribeDeathSource();
            IsDead = false;
        }

        private void HandleDeath()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            movement.ResetForDespawn();
            movement.Stop();
            controller.enabled = false;
            detection.enabled = false;
            detection.ClearCurrentTarget();
            targetable.enabled = false;
            OnDeathCleanupEnd?.Invoke();
        }

        private void SubscribeDeathSource()
        {
            if (subscribedDeathSource != null && subscribedDeathSource != deathSource)
            {
                UnsubscribeDeathSource();
            }

            deathSource.Died -= HandleDeath;
            deathSource.Died += HandleDeath;
            subscribedDeathSource = deathSource;
        }

        private void UnsubscribeDeathSource()
        {
            if (subscribedDeathSource != null)
            {
                subscribedDeathSource.Died -= HandleDeath;
                subscribedDeathSource = null;
            }
        }

        private void CacheRequiredComponents()
        {
            if (deathSource == null)
            {
                deathSource = GetComponentInParent<StatisticsController>();
            }

            if (movement == null)
            {
                movement = GetComponent<Movement>();
            }

            if (controller == null)
            {
                controller = GetComponent<Controller>();
            }

            if (detection == null)
            {
                detection = GetComponent<Detection>();
            }

            if (targetable == null)
            {
                targetable = GetComponent<EnemyTargetable>();
            }
        }

        private bool HasRequiredComponents()
        {
            if (deathSource == null)
            {
                Debug.LogError("Missing field deathSource.", this);
                return false;
            }

            if (movement == null)
            {
                Debug.LogError("Missing field movement.", this);
                return false;
            }

            if (controller == null)
            {
                Debug.LogError("Missing field controller.", this);
                return false;
            }

            if (detection == null)
            {
                Debug.LogError("Missing field detection.", this);
                return false;
            }

            if (targetable == null)
            {
                Debug.LogError("Missing field targetable.", this);
                return false;
            }

            return true;
        }
    }
}
