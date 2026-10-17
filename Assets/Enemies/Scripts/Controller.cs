using RPGame.Combat.Damage;
using RPGame.Core.Pooling;
using RPGame.Core.Statuses;
using RPGame.Core.Statistics;
using RPGame.Core.Targeting;
using UnityEngine;

namespace RPGame.Enemies
{
    [RequireComponent(typeof(StatisticsController))]
    [RequireComponent(typeof(Detection))]
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(EnemyTargetable))]
    [RequireComponent(typeof(DamageReceiver))]
    [RequireComponent(typeof(Death))]
    [RequireComponent(typeof(StatusAggregator))]
    [RequireComponent(typeof(PooledEnemy))]
    public sealed class Controller : MonoBehaviour, IPooledEnemyResettable
    {
        [SerializeField] private Config config;
        [SerializeField] private EnemyBehaviourCreator behaviourCreator;

        private IEnemyBehaviour behaviour;

        internal IEnemyBehaviour Behaviour => behaviour;
        internal Config Config => config;

        public void ResetForSpawn()
        {
            behaviour = null;
            if (!TryCreateBehaviour(out behaviour))
            {
                enabled = false;
            }
        }

        public void ResetForDespawn()
        {
            behaviour = null;
        }

        private void Start()
        {
            if (!TryCreateBehaviour(out behaviour))
            {
                enabled = false;
            }
        }

        private void Update()
        {
            behaviour?.Tick(Time.deltaTime);
        }

        internal void Tick()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            behaviour?.Tick(Time.deltaTime);
        }

        private bool TryCreateBehaviour(out IEnemyBehaviour createdBehaviour)
        {
            createdBehaviour = null;

            if (behaviourCreator == null)
            {
                Debug.LogError("Missing field behaviourCreator.", this);
                return false;
            }

            return behaviourCreator.TryCreate(config, out createdBehaviour);
        }
    }
}
