using RPGame.Combat.Damage;
using RPGame.Core.Damage;
using RPGame.Core.Progression;
using UnityEngine;

namespace RPGame.Enemies
{
    [RequireComponent(typeof(Controller))]
    [RequireComponent(typeof(DamageReceiver))]
    public sealed class EnemyExperienceReward : MonoBehaviour
    {
        private DamageReceiver damageReceiver;
        private Controller controller;

        private void Awake()
        {
            CacheComponents();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void HandleDamageReceived(DamageResult result)
        {
            if (!result.WasFatal
                || controller == null
                || controller.Config == null
                || controller.Config.ExperienceReward <= 0
                || result.Data.Source == null)
            {
                return;
            }

            IExperienceReceiver receiver = FindExperienceReceiver(result.Data.Source);
            receiver?.AddExperience(controller.Config.ExperienceReward);
        }

        private void CacheComponents()
        {
            damageReceiver ??= GetComponent<DamageReceiver>();
            controller ??= GetComponent<Controller>();
        }

        private void Subscribe()
        {
            if (damageReceiver != null)
            {
                damageReceiver.DamageReceived -= HandleDamageReceived;
                damageReceiver.DamageReceived += HandleDamageReceived;
            }
        }

        private void Unsubscribe()
        {
            if (damageReceiver != null)
            {
                damageReceiver.DamageReceived -= HandleDamageReceived;
            }
        }

        private static IExperienceReceiver FindExperienceReceiver(GameObject source)
        {
            return source.GetComponentInParent<IExperienceReceiver>();
        }
    }
}
