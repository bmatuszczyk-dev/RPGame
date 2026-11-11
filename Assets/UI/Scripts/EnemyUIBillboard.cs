using UnityEngine;

namespace RPGame.UI
{
    public sealed class EnemyUIBillboard : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;

        private void LateUpdate()
        {
            playerCamera ??= Camera.main;
            if (playerCamera == null)
            {
                return;
            }

            Vector3 direction = playerCamera.transform.position - transform.position;
            if (direction.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}
