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
        [SerializeField] private Detection detection;
        [SerializeField] private Movement movement;
        [SerializeField] private Config config;
        [SerializeField] private Attack attack;

        private IEnemyBehaviour behaviour;

        internal IEnemyBehaviour Behaviour => behaviour;
        internal Config Config => config;

        public void ResetForSpawn()
        {
            CacheRequiredComponents();
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
            CacheRequiredComponents();
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

        private void CacheRequiredComponents()
        {
            if (detection == null)
            {
                detection = GetComponent<Detection>();
            }

            if (movement == null)
            {
                movement = GetComponent<Movement>();
            }

            if (attack == null)
            {
                attack = GetComponent<Attack>();
            }

        }

        private bool TryCreateBehaviour(out IEnemyBehaviour createdBehaviour)
        {
            createdBehaviour = null;

            if (!HasRequiredComponents())
            {
                return false;
            }

            movement.SetKnockbackResistance(config.KnockbackResistance);
            attack?.SetConfig(config);

            if (config.BehaviourConfig == null)
            {
                Debug.LogError("Missing field behaviourConfig.", this);
                return false;
            }

            switch (config.BehaviourConfig)
            {
                case MeleeEnemyBehaviourConfig:
                    if (!TryGetAttack(AttackType.Melee, out IEnemyAttack meleeAttack))
                    {
                        return false;
                    }

                    createdBehaviour = new MeleeEnemyBehaviour(detection, movement, meleeAttack);
                    return true;

                case ChargingMeleeEnemyBehaviourConfig chargingConfig:
                    if (!TryGetAttack(AttackType.Melee, out IEnemyAttack chargingMeleeAttack))
                    {
                        return false;
                    }

                    createdBehaviour = new ChargingMeleeEnemyBehaviour(
                        detection,
                        movement,
                        chargingMeleeAttack,
                        chargingConfig,
                        gameObject);
                    return true;

                case RangedEnemyBehaviourConfig rangedConfig:
                    if (!TryGetAttack(AttackType.StraightProjectile, out IEnemyAttack straightAttack)
                        || !TryGetAttack(AttackType.ParabolicProjectile, out IEnemyAttack parabolicAttack))
                    {
                        return false;
                    }

                    createdBehaviour = new RangedEnemyBehaviour(
                        detection,
                        movement,
                        rangedConfig,
                        GetComponent<LineOfSight>(),
                        straightAttack,
                        parabolicAttack);
                    return true;

                case HealerEnemyBehaviourConfig healerConfig:
                    HealerTargetSensor healerTargetSensor = GetComponentInChildren<HealerTargetSensor>(true);
                    LineOfSight healerLineOfSight = GetComponent<LineOfSight>();
                    StatisticsController healerStatistics = GetComponent<StatisticsController>();
                    EnemyTargetable healerTargetable = GetComponent<EnemyTargetable>();
                    if (healerTargetSensor == null
                        || healerLineOfSight == null
                        || healerStatistics == null
                        || healerTargetable == null)
                    {
                        Debug.LogError("Missing healer dependencies.", this);
                        return false;
                    }

                    healerTargetSensor.SetSearchRadius(healerConfig.AllySearchRange);
                    createdBehaviour = new HealerEnemyBehaviour(
                        detection,
                        movement,
                        healerTargetSensor,
                        healerLineOfSight,
                        healerStatistics,
                        healerConfig,
                        healerTargetable);
                    return true;

                case AssassinEnemyBehaviourConfig assassinConfig:
                    if (!TryGetAttack(AttackType.AssassinSlash, out IEnemyAttack assassinSlashAttack)
                        || !TryGetAttack(AttackType.StraightProjectile, out IEnemyAttack assassinRangedAttack))
                    {
                        return false;
                    }

                    LineOfSight assassinLineOfSight = GetComponent<LineOfSight>();
                    if (assassinLineOfSight == null)
                    {
                        Debug.LogError("Missing field lineOfSight.", this);
                        return false;
                    }

                    CombatLeap combatLeap = new(assassinConfig, movement);
                    createdBehaviour = new AssassinEnemyBehaviour(
                        detection,
                        movement,
                        assassinLineOfSight,
                        assassinSlashAttack,
                        assassinRangedAttack,
                        assassinConfig,
                        combatLeap);
                    return true;

                default:
                    Debug.LogError(
                        $"Unsupported behaviour config '{config.BehaviourConfig.GetType().Name}'.",
                        this);
                    return false;
            }
        }

        private bool HasRequiredComponents()
        {
            if (detection == null)
            {
                Debug.LogError("Missing field detection.", this);
                return false;
            }

            if (movement == null)
            {
                Debug.LogError("Missing field movement.", this);
                return false;
            }

            if (config == null)
            {
                Debug.LogError("Missing field config.", this);
                return false;
            }

            return true;
        }

        private bool TryGetAttack(AttackType type, out IEnemyAttack runtimeAttack)
        {
            if (attack == null)
            {
                runtimeAttack = null;
                Debug.LogError("Missing field attack.", this);
                return false;
            }

            if (attack.TryGetRuntimeAttack(type, out runtimeAttack))
            {
                return true;
            }

            Debug.LogError($"Attack '{type}' failed to initialize.", this);
            return false;
        }
    }
}
