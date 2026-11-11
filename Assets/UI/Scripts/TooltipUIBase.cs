using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RPGame.UI
{
    public abstract class TooltipUIBase : MonoBehaviour
    {
        [SerializeField] private RectTransform tooltipTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Vector2 screenOffset = new(16f, -16f);
        [SerializeField] private Vector2 screenPadding = new(8f, 8f);

        protected virtual void Awake()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            Hide();
        }

        public void Hide()
        {
            SetVisible(false);
        }

        protected void ShowAt(Vector2 screenPosition)
        {
            transform.SetAsLastSibling();
            RectTransform rectTransform = tooltipTransform != null ? tooltipTransform : transform as RectTransform;
            if (rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }

            SetPosition(screenPosition);
            SetVisible(true);
        }

        protected static void SetTextSection(TextMeshProUGUI text, string value)
        {
            string displayValue = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            text.text = displayValue;

            Transform parent = text.transform.parent;
            if (parent != null)
            {
                parent.gameObject.SetActive(!string.IsNullOrEmpty(displayValue));
            }
            else
            {
                text.gameObject.SetActive(!string.IsNullOrEmpty(displayValue));
            }
        }

        private void SetPosition(Vector2 screenPosition)
        {
            RectTransform rectTransform = tooltipTransform != null ? tooltipTransform : transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            Vector2 position = GetPositionBelowCursor(screenPosition, rectTransform);
            if (IsBelowScreen(position, rectTransform))
            {
                position = GetPositionAboveCursor(screenPosition, rectTransform);
            }

            rectTransform.position = ClampToScreen(position, rectTransform);
        }

        private Vector2 GetPositionBelowCursor(Vector2 screenPosition, RectTransform rectTransform)
        {
            Vector2 position = screenPosition + screenOffset;
            position.y -= rectTransform.rect.height * (1f - rectTransform.pivot.y);
            return position;
        }

        private Vector2 GetPositionAboveCursor(Vector2 screenPosition, RectTransform rectTransform)
        {
            Vector2 position = screenPosition + new Vector2(screenOffset.x, Mathf.Abs(screenOffset.y));
            position.y += rectTransform.rect.height * rectTransform.pivot.y;
            return position;
        }

        private bool IsBelowScreen(Vector2 position, RectTransform rectTransform)
        {
            return position.y - rectTransform.rect.height * rectTransform.pivot.y < screenPadding.y;
        }

        private Vector2 ClampToScreen(Vector2 position, RectTransform rectTransform)
        {
            Rect rect = rectTransform.rect;
            Vector2 pivot = rectTransform.pivot;

            float minX = screenPadding.x + rect.width * pivot.x;
            float maxX = Screen.width - screenPadding.x - rect.width * (1f - pivot.x);
            float minY = screenPadding.y + rect.height * pivot.y;
            float maxY = Screen.height - screenPadding.y - rect.height * (1f - pivot.y);

            position.x = minX > maxX ? Screen.width * 0.5f : Mathf.Clamp(position.x, minX, maxX);
            position.y = minY > maxY ? Screen.height * 0.5f : Mathf.Clamp(position.y, minY, maxY);

            return position;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
