using RPGame.Core.Spells.Symbols;
using RPGame.Core;
using RPGame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RPGame.UI.Symbols
{
    public sealed class SymbolDrawerUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RawImage drawingImage;
        [SerializeField] private RawImage normalizedDebugImage;
        [SerializeField] private DrawingReceiverBase drawingReceiver;
        [SerializeField] private InputActionReference drawModifierAction;
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField] private bool clearAfterSubmit = true;

        private Texture2D drawingTexture;
        private Texture2D debugPreviewTexture;
        private Vector2Int previousPixel;
        private Vector2Int minDrawingPixel;
        private Vector2Int maxDrawingPixel;
        private bool hasDrawingBounds;
        private bool isDrawing;

        private static Color BackgroundColor => Color.clear;

        private void Awake()
        {
            CreateDrawingTexture();
            Clear();
            SetDrawingImageActive(false);
        }

        private void OnEnable()
        {
            InputAction drawModifier = drawModifierAction != null ? drawModifierAction.action : null;
            if (drawModifier == null)
            {
                return;
            }

            drawModifier.Enable();
            drawModifier.started += HandleDrawModifierChanged;
            drawModifier.canceled += HandleDrawModifierChanged;
            UpdateDrawingState();
        }

        private void OnDisable()
        {
            InputAction drawModifier = drawModifierAction != null ? drawModifierAction.action : null;
            if (drawModifier == null)
            {
                return;
            }

            drawModifier.started -= HandleDrawModifierChanged;
            drawModifier.canceled -= HandleDrawModifierChanged;
            drawModifier.Disable();
            isDrawing = false;
            UpdateDrawingState();
        }

        private void OnDestroy()
        {
            ReleaseTexture(ref debugPreviewTexture);
            ReleaseTexture(ref drawingTexture);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (GameplayInputBlocker.IsBlocked
                || eventData.button != PointerEventData.InputButton.Left || !IsDrawModifierPressed())
            {
                return;
            }

            isDrawing = TryGetPixel(eventData, out previousPixel);
            if (isDrawing)
            {
                Debug.Log("Symbol drawing started.", this);
                if (drawingReceiver is SymbolRecognition symbolRecognition)
                {
                    symbolRecognition.OnDrawingStarted();
                }

                ResetDrawingBounds();
                DrawBrush(previousPixel);
                drawingTexture.Apply();
                UpdateDrawingState();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (GameplayInputBlocker.IsBlocked || !isDrawing || !TryGetPixel(eventData, out Vector2Int currentPixel))
            {
                return;
            }

            DrawLine(previousPixel, currentPixel);
            previousPixel = currentPixel;
            drawingTexture.Apply();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !isDrawing)
            {
                return;
            }

            isDrawing = false;
            UpdateDrawingState();
            Debug.Log("Symbol drawing ended; submitting drawing.", this);
            SubmitDrawing();
        }

        public void SubmitDrawing()
        {
            Texture2D submittedTexture = CreateNormalizedDrawingTexture();
            if (submittedTexture == null)
            {
                return;
            }

            if (normalizedDebugImage != null)
            {
                ReleaseTexture(ref debugPreviewTexture);
                debugPreviewTexture = submittedTexture;
                normalizedDebugImage.texture = submittedTexture;
            }

            if (drawingReceiver == null)
            {
                Debug.LogWarning("Symbol drawing was normalized, but drawingReceiver is not assigned.", this);
                ReleaseSubmittedTextureIfNeeded(submittedTexture);
                return;
            }

            Debug.Log("Normalized symbol drawing submitted to drawing receiver.", this);
            drawingReceiver.SubmitDrawing(submittedTexture);
            ReleaseSubmittedTextureIfNeeded(submittedTexture);

            if (clearAfterSubmit)
            {
                Clear();
            }
        }

        public void Clear()
        {
            if (drawingTexture == null)
            {
                CreateDrawingTexture();
            }

            Color[] pixels = drawingTexture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = BackgroundColor;
            }

            drawingTexture.SetPixels(pixels);
            drawingTexture.Apply();
            ResetDrawingBounds();
        }

        private void CreateDrawingTexture()
        {
            ReleaseTexture(ref drawingTexture);

            drawingTexture = new Texture2D(
                SymbolDrawingConstants.DrawingTextureSize,
                SymbolDrawingConstants.DrawingTextureSize,
                TextureFormat.RGBA32,
                false);
            drawingTexture.wrapMode = TextureWrapMode.Clamp;
            drawingTexture.filterMode = FilterMode.Point;

            if (drawingImage != null)
            {
                drawingImage.texture = drawingTexture;
            }
        }

        private void SetDrawingImageActive(bool active)
        {
            if (drawingImage == null || drawingImage.gameObject.activeSelf == active)
            {
                return;
            }

            drawingImage.gameObject.SetActive(active);
        }

        private bool TryGetPixel(PointerEventData eventData, out Vector2Int pixel)
        {
            pixel = default;
            if (drawingImage == null)
            {
                return false;
            }

            RectTransform rectTransform = drawingImage.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            Rect rect = rectTransform.rect;
            float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

            if (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f)
            {
                return false;
            }

            pixel = new Vector2Int(
                Mathf.Clamp(
                    (int)(normalizedX * SymbolDrawingConstants.DrawingTextureSize),
                    0,
                    SymbolDrawingConstants.DrawingTextureSize - 1),
                Mathf.Clamp(
                    (int)(normalizedY * SymbolDrawingConstants.DrawingTextureSize),
                    0,
                    SymbolDrawingConstants.DrawingTextureSize - 1));
            return true;
        }

        private void DrawLine(Vector2Int from, Vector2Int to)
        {
            int x0 = from.x;
            int y0 = from.y;
            int x1 = to.x;
            int y1 = to.y;
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx - dy;

            while (true)
            {
                DrawBrush(new Vector2Int(x0, y0));
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int doubledError = error * 2;
                if (doubledError > -dy)
                {
                    error -= dy;
                    x0 += sx;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private void DrawBrush(Vector2Int center)
        {
            int radius = SymbolDrawingConstants.BrushRadius;
            int radiusSquared = radius * radius;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > radiusSquared)
                    {
                        continue;
                    }

                    int pixelX = center.x + x;
                    int pixelY = center.y + y;
                    if (pixelX < 0
                        || pixelX >= SymbolDrawingConstants.DrawingTextureSize
                        || pixelY < 0
                        || pixelY >= SymbolDrawingConstants.DrawingTextureSize)
                    {
                        continue;
                    }

                    drawingTexture.SetPixel(pixelX, pixelY, lineColor);
                    IncludePixelInBounds(pixelX, pixelY);
                }
            }
        }

        private Texture2D CreateNormalizedDrawingTexture()
        {
            return SymbolDrawingUtility.CreateNormalizedTexture(
                drawingTexture,
                minDrawingPixel,
                maxDrawingPixel,
                hasDrawingBounds);
        }

        private void IncludePixelInBounds(int pixelX, int pixelY)
        {
            if (!hasDrawingBounds)
            {
                minDrawingPixel = new Vector2Int(pixelX, pixelY);
                maxDrawingPixel = new Vector2Int(pixelX, pixelY);
                hasDrawingBounds = true;
                return;
            }

            minDrawingPixel = Vector2Int.Min(minDrawingPixel, new Vector2Int(pixelX, pixelY));
            maxDrawingPixel = Vector2Int.Max(maxDrawingPixel, new Vector2Int(pixelX, pixelY));
        }

        private void ResetDrawingBounds()
        {
            minDrawingPixel = new Vector2Int(
                SymbolDrawingConstants.DrawingTextureSize,
                SymbolDrawingConstants.DrawingTextureSize);
            maxDrawingPixel = Vector2Int.zero;
            hasDrawingBounds = false;
        }

        private void ReleaseSubmittedTextureIfNeeded(Texture2D submittedTexture)
        {
            if (submittedTexture == null || ReferenceEquals(submittedTexture, debugPreviewTexture))
            {
                return;
            }

            Destroy(submittedTexture);
        }

        private static void ReleaseTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            Object.Destroy(texture);
            texture = null;
        }

        private bool IsDrawModifierPressed()
        {
            return drawModifierAction != null && drawModifierAction.action != null && drawModifierAction.action.IsPressed();
        }

        private void HandleDrawModifierChanged(InputAction.CallbackContext context)
        {
            UpdateDrawingState();
        }

        private void UpdateDrawingState()
        {
            bool shouldDraw = !GameplayInputBlocker.IsBlocked && (IsDrawModifierPressed() || isDrawing);
            SetDrawingImageActive(shouldDraw);
            CursorController.SetDrawingCursorActive(shouldDraw);
        }
    }
}
