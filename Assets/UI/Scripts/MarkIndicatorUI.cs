using System.Collections;
using RPGame.Core.Pooling;
using RPGame.Core.Statuses;
using UnityEngine;
using UnityEngine.UI;

namespace RPGame.UI
{
    public sealed class MarkIndicatorUI : MonoBehaviour, IPooledEnemyResettable
    {
        [SerializeField] private Image center;
        [SerializeField] private Image left;
        [SerializeField] private Image right;
        [SerializeField] private float fadeDuration = 0.15f;

        private readonly Image[] parts = new Image[3];
        private readonly Coroutine[] fadeRoutines = new Coroutine[3];
        private readonly bool[] visibleParts = new bool[3];
        private IMarkProgressReader markProgress;
        private Transform worldSpaceCanvas;
        private Camera playerCamera;

        private void Awake()
        {
            parts[0] = center;
            parts[1] = left;
            parts[2] = right;
            worldSpaceCanvas = GetComponentInParent<Canvas>()?.transform;
        }

        private void OnEnable()
        {
            TryBindMarkProgress();
            Refresh();
        }

        private void OnDisable()
        {
            if (markProgress != null)
            {
                markProgress.ProgressChanged -= Refresh;
                markProgress = null;
            }

            StopFades();
        }

        private void LateUpdate()
        {
            if (markProgress == null)
            {
                TryBindMarkProgress();
                Refresh();
            }

            if (!HasVisiblePart() || worldSpaceCanvas == null)
            {
                return;
            }

            playerCamera ??= Camera.main;
            if (playerCamera == null)
            {
                return;
            }

            Vector3 direction = playerCamera.transform.position - worldSpaceCanvas.position;
            if (direction.sqrMagnitude > 0f)
            {
                worldSpaceCanvas.rotation = Quaternion.LookRotation(direction);
            }
        }

        public void ResetForSpawn()
        {
            HideAll();
        }

        public void ResetForDespawn()
        {
            HideAll();
        }

        private void Refresh()
        {
            int visiblePartCount = markProgress != null && markProgress.IsMarkActive
                ? 3
                : markProgress?.CurrentUniqueSpellProgress ?? 0;
            for (int i = 0; i < parts.Length; i++)
            {
                SetPartVisible(i, i < visiblePartCount);
            }
        }

        private void SetPartVisible(int index, bool shouldBeVisible)
        {
            Image part = parts[index];
            if (part == null)
            {
                return;
            }

            if (!shouldBeVisible)
            {
                StopFade(index);
                visibleParts[index] = false;
                SetAlpha(part, 0f);
                return;
            }

            if (visibleParts[index])
            {
                return;
            }

            visibleParts[index] = true;
            StopFade(index);
            SetAlpha(part, 0f);
            if (fadeDuration <= 0f)
            {
                SetAlpha(part, 1f);
                return;
            }

            fadeRoutines[index] = StartCoroutine(FadeIn(index, part));
        }

        private IEnumerator FadeIn(int index, Image part)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(part, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            SetAlpha(part, 1f);
            fadeRoutines[index] = null;
        }

        private void HideAll()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                SetPartVisible(i, false);
            }
        }

        private bool HasVisiblePart()
        {
            for (int i = 0; i < visibleParts.Length; i++)
            {
                if (visibleParts[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void StopFades()
        {
            for (int i = 0; i < fadeRoutines.Length; i++)
            {
                StopFade(i);
            }
        }

        private void StopFade(int index)
        {
            if (fadeRoutines[index] == null)
            {
                return;
            }

            StopCoroutine(fadeRoutines[index]);
            fadeRoutines[index] = null;
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private IMarkProgressReader FindMarkProgressReader()
        {
            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMarkProgressReader reader)
                {
                    return reader;
                }
            }

            return null;
        }

        private void TryBindMarkProgress()
        {
            if (markProgress != null)
            {
                return;
            }

            markProgress = FindMarkProgressReader();
            if (markProgress != null)
            {
                markProgress.ProgressChanged += Refresh;
            }
        }
    }
}
