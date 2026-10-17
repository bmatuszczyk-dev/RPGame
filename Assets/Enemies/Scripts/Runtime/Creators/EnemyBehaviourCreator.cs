using UnityEngine;

namespace RPGame.Enemies
{
    public abstract class EnemyBehaviourCreator : MonoBehaviour
    {
        public bool TryCreate(Config config, out IEnemyBehaviour behaviour)
        {
            behaviour = null;

            if (config == null)
            {
                Debug.LogError("Missing field config.", this);
                return false;
            }

            if (config.BehaviourConfig == null)
            {
                Debug.LogError("Missing field behaviourConfig.", this);
                return false;
            }

            if (!TryResolveCommonDependencies(out Detection detection, out Movement movement, out Attack attack))
            {
                return false;
            }

            movement.SetKnockbackResistance(config.KnockbackResistance);
            attack?.SetConfig(config);
            return TryCreateBehaviour(config, detection, movement, attack, out behaviour);
        }

        protected abstract bool TryCreateBehaviour(
            Config config,
            Detection detection,
            Movement movement,
            Attack attack,
            out IEnemyBehaviour behaviour);

        protected bool TryGetAttack(Attack attack, AttackType type, out IEnemyAttack runtimeAttack)
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

        protected bool TryGetRequiredComponent<T>(out T component, string fieldName) where T : Component
        {
            component = GetComponent<T>();
            if (component != null)
            {
                return true;
            }

            Debug.LogError($"Missing field {fieldName}.", this);
            return false;
        }

        private bool TryResolveCommonDependencies(out Detection detection, out Movement movement, out Attack attack)
        {
            detection = GetComponent<Detection>();
            if (detection == null)
            {
                movement = null;
                attack = null;
                Debug.LogError("Missing field detection.", this);
                return false;
            }

            movement = GetComponent<Movement>();
            if (movement == null)
            {
                attack = null;
                Debug.LogError("Missing field movement.", this);
                return false;
            }

            attack = GetComponent<Attack>();
            return true;
        }
    }
}
