using System.Collections.Generic;
using RPGame.Core.Damage;
using UnityEngine;

namespace RPGame.Combat.Spells
{
    internal static class DetonationExplosionQueue
    {
        private static readonly Queue<ExplosionData> pendingExplosions = new();
        private static bool isProcessing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            pendingExplosions.Clear();
            isProcessing = false;
        }

        public static void Enqueue(ExplosionData explosion)
        {
            pendingExplosions.Enqueue(explosion);
            if (isProcessing)
            {
                return;
            }

            isProcessing = true;
            try
            {
                while (pendingExplosions.Count > 0)
                {
                    ProcessExplosion(pendingExplosions.Dequeue());
                }
            }
            catch
            {
                pendingExplosions.Clear();
                throw;
            }
            finally
            {
                isProcessing = false;
            }
        }

        private static void ProcessExplosion(ExplosionData explosion)
        {
            Collider[] colliders = Physics.OverlapSphere(explosion.Position, explosion.Radius);
            HashSet<IDamageable> hitTargets = new();

            for (int i = 0; i < colliders.Length; i++)
            {
                IDamageable target = colliders[i].GetComponentInParent<IDamageable>();
                if (target == null
                    || IsOrigin(target, explosion.OriginDamageableId)
                    || !target.CanReceiveDamage
                    || !hitTargets.Add(target)
                    || IsSource(target, explosion.Source))
                {
                    continue;
                }

                target.ApplyDamage(new DamageData(
                    new[]
                    {
                        new PartialDamage(
                            explosion.Damage,
                            DamageType.Magical,
                            DamageElement.Fire)
                    },
                    explosion.Source,
                    DamageApplicationType.Direct,
                    DamageOrigin.Spell));
            }
        }

        private static bool IsSource(IDamageable target, GameObject source)
        {
            if (source == null || target is not Component targetComponent)
            {
                return false;
            }

            Transform sourceTransform = source.transform;
            Transform targetTransform = targetComponent.transform;
            return targetTransform == sourceTransform
                || targetTransform.IsChildOf(sourceTransform)
                || sourceTransform.IsChildOf(targetTransform);
        }

        private static bool IsOrigin(IDamageable target, EntityId originDamageableId)
        {
            return target is Component targetComponent
                && targetComponent.GetEntityId().Equals(originDamageableId);
        }
    }

    internal readonly struct ExplosionData
    {
        public ExplosionData(
            Vector3 position,
            float radius,
            float damage,
            GameObject source,
            EntityId originDamageableId)
        {
            Position = position;
            Radius = Mathf.Max(0f, radius);
            Damage = Mathf.Max(0f, damage);
            Source = source;
            OriginDamageableId = originDamageableId;
        }

        public Vector3 Position { get; }
        public float Radius { get; }
        public float Damage { get; }
        public GameObject Source { get; }
        public EntityId OriginDamageableId { get; }
    }
}
