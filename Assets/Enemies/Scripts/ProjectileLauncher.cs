using UnityEngine;

namespace RPGame.Enemies
{
    public sealed class ProjectileLauncher : MonoBehaviour, IProjectileLauncher
    {
        [SerializeField] private Transform projectileSpawnPoint;

        public bool Launch(ProjectileLaunchData data)
        {
            if (data.ProjectilePrefab == null)
            {
                return false;
            }

            Transform spawnPoint = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
            Vector3 direction = data.TargetPosition - spawnPoint.position;
            Quaternion rotation = direction.sqrMagnitude > 0f
                ? Quaternion.LookRotation(direction.normalized)
                : spawnPoint.rotation;

            GameObject projectileObject = Instantiate(data.ProjectilePrefab, spawnPoint.position, rotation);
            IEnemyProjectile projectile = GetEnemyProjectile(projectileObject);
            if (projectile == null)
            {
                Debug.LogError("Projectile prefab is missing an IEnemyProjectile component.", projectileObject);
                DestroyProjectile(projectileObject);
                return false;
            }

            projectile.Initialize(
                data.TargetPosition,
                data.TargetDamageable,
                data.DamageParts,
                data.Source,
                data.Buildup);

            return true;
        }

        private static void DestroyProjectile(GameObject projectileObject)
        {
            if (Application.isPlaying)
            {
                Destroy(projectileObject);
            }
            else
            {
                DestroyImmediate(projectileObject);
            }
        }

        private static IEnemyProjectile GetEnemyProjectile(GameObject projectileObject)
        {
            MonoBehaviour[] behaviours = projectileObject.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEnemyProjectile projectile)
                {
                    return projectile;
                }
            }

            return null;
        }
    }
}
