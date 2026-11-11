using System.Collections.Generic;
using RPGame.Combat.Spells;
using RPGame.Core.Damage;
using RPGame.Core.Spells;
using UnityEngine;

namespace RPGame.Combat.Projectiles
{
    public sealed class OrbitProjectileController : MonoBehaviour
    {
        private readonly Dictionary<Collider, IDamageable> contactedColliders = new();
        private readonly Dictionary<IDamageable, int> activeContactCounts = new();

        private CasterData casterData;
        private float damageCapacity;
        private float appliedDamage;
        private bool isInitialized;

        public void Initialize(CasterData casterData, float damageCapacity)
        {
            this.casterData = casterData;
            this.damageCapacity = Mathf.Max(0f, damageCapacity);
            appliedDamage = 0f;
            isInitialized = this.damageCapacity > 0f;

            if (!isInitialized)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            RegisterContact(other);
        }

        private void OnTriggerExit(Collider other)
        {
            UnregisterContact(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            RegisterContact(collision != null ? collision.collider : null);
        }

        private void OnCollisionExit(Collision collision)
        {
            UnregisterContact(collision != null ? collision.collider : null);
        }

        private void RegisterContact(Collider other)
        {
            if (!isInitialized || other == null || contactedColliders.ContainsKey(other) || ShouldIgnore(other))
            {
                return;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            contactedColliders.Add(other, damageable);
            activeContactCounts.TryGetValue(damageable, out int contactCount);
            activeContactCounts[damageable] = contactCount + 1;
            if (contactCount > 0)
            {
                return;
            }

            GameObject targetObject = ((Component)damageable).gameObject;
            DamageResolveBehavior resolveBehavior = new(damageable, casterData);
            SpellBehaviorPipelineExecutor.Resolve(casterData, targetObject, resolveBehavior);
            DamageResult result = resolveBehavior.Result;
            appliedDamage += result.AppliedAmount;
            if (appliedDamage >= damageCapacity)
            {
                isInitialized = false;
                Destroy(gameObject);
            }
        }

        private void UnregisterContact(Collider other)
        {
            if (other == null || !contactedColliders.TryGetValue(other, out IDamageable damageable))
            {
                return;
            }

            contactedColliders.Remove(other);
            if (!activeContactCounts.TryGetValue(damageable, out int contactCount))
            {
                return;
            }

            if (contactCount <= 1)
            {
                activeContactCounts.Remove(damageable);
            }
            else
            {
                activeContactCounts[damageable] = contactCount - 1;
            }
        }

        private bool ShouldIgnore(Collider other)
        {
            GameObject casterObject = casterData.CasterObject;
            return other.gameObject == gameObject
                || other.transform.IsChildOf(transform)
                || (casterObject != null
                    && (other.gameObject == casterObject
                        || other.transform.IsChildOf(casterObject.transform)));
        }
    }
}
