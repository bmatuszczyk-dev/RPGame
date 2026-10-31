using RPGame.Core.Settings;
using RPGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RPGame.Player
{
    public sealed class ThirdPersonCameraAim : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform aimTarget;
        [SerializeField] private PlayerInput playerInput;

        [Header("Input")]
        [SerializeField] private InputActionProperty lookAction;
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private InputActionProperty alternativeUseAction;
        [SerializeField] private string alternativeUseActionName = "AlternativeUse";

        [Header("Aim")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float cameraSpeedMultiplayer = 0.5f;
        [SerializeField] private float stickSensitivity = 120f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;

        private float yaw;
        private float pitch;
        private InputAction resolvedLookAction;
        private InputAction resolvedAlternativeUseAction;
        private bool isPointerLookInput;
        private float drawingSensitivity;

        private void Awake()
        {
            mouseSensitivity = GameSettings.GetMouseSensitivity();
            drawingSensitivity = GameSettings.GetDrawingSensitivity();

            if (aimTarget == null)
            {
                aimTarget = transform;
            }

            if (playerInput == null)
            {
                playerInput = GetComponentInParent<PlayerInput>();
            }

            Vector3 initialAngles = aimTarget.eulerAngles;
            yaw = initialAngles.y;
            pitch = NormalizeAngle(initialAngles.x);

            ResolveInputActions();
        }

        private void OnEnable()
        {
            GameSettings.MouseSensitivityChanged += SetMouseSensitivity;
            GameSettings.DrawingSensitivityChanged += SetDrawingSensitivity;
            ResolveInputActions();
            EnableAction(resolvedLookAction);
            EnableAction(resolvedAlternativeUseAction);

            if (resolvedLookAction != null)
            {
                resolvedLookAction.performed += OnLookPerformed;
            }
        }

        private void OnDisable()
        {
            GameSettings.MouseSensitivityChanged -= SetMouseSensitivity;
            GameSettings.DrawingSensitivityChanged -= SetDrawingSensitivity;

            if (resolvedLookAction != null)
            {
                resolvedLookAction.performed -= OnLookPerformed;
            }

            DisableAction(resolvedLookAction);
            DisableAction(resolvedAlternativeUseAction);
        }

        private void Update()
        {
            if (GameplayInputBlocker.IsBlocked || aimTarget == null)
            {
                return;
            }

            Vector2 lookDelta = ReadLookDelta();

            yaw += lookDelta.x;
            pitch = Mathf.Clamp(pitch - lookDelta.y, minPitch, maxPitch);

            aimTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private Vector2 ReadLookDelta()
        {
            if (resolvedLookAction != null)
            {
                Vector2 lookInput = resolvedLookAction.ReadValue<Vector2>();
                bool isAlternativeUsePressed = IsAlternativeUsePressed();
                return isPointerLookInput
                    ? lookInput * mouseSensitivity * cameraSpeedMultiplayer * GetDrawingSensitivityMultiplier(isAlternativeUsePressed)
                    : lookInput * stickSensitivity * GetGamepadSensitivityMultiplier(isAlternativeUsePressed) * Time.deltaTime;
            }

            Vector2 delta = Vector2.zero;
            bool isFallbackAlternativeUsePressed = IsAlternativeUsePressed();

            if (Mouse.current != null)
            {
                delta += Mouse.current.delta.ReadValue()
                    * mouseSensitivity
                    * cameraSpeedMultiplayer
                    * GetDrawingSensitivityMultiplier(isFallbackAlternativeUsePressed);
            }

            if (Gamepad.current != null)
            {
                delta += Gamepad.current.rightStick.ReadValue() * stickSensitivity * GetGamepadSensitivityMultiplier(isFallbackAlternativeUsePressed) * Time.deltaTime;
            }

            return delta;
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            InputDevice device = context.control.device;
            isPointerLookInput = device is Mouse || device is Pointer || device is Touchscreen;
        }

        private void SetMouseSensitivity(float value)
        {
            mouseSensitivity = value;
        }

        private void SetDrawingSensitivity(float value)
        {
            drawingSensitivity = value;
        }

        private float GetDrawingSensitivityMultiplier(bool isAlternativeUsePressed)
        {
            return isAlternativeUsePressed ? drawingSensitivity : 1f;
        }

        private float GetGamepadSensitivityMultiplier(bool isAlternativeUsePressed)
        {
            return isAlternativeUsePressed ? cameraSpeedMultiplayer : 1f;
        }

        private void ResolveInputActions()
        {
            resolvedLookAction = ResolveAction(lookAction, lookActionName);
            resolvedAlternativeUseAction = ResolveAction(alternativeUseAction, alternativeUseActionName);
        }

        private InputAction ResolveAction(InputActionProperty actionProperty, string actionName)
        {
            if (playerInput != null && playerInput.actions != null && !string.IsNullOrWhiteSpace(actionName))
            {
                InputAction playerAction = playerInput.actions.FindAction(actionName, false);

                if (playerAction != null)
                {
                    return playerAction;
                }
            }

            return HasBindings(actionProperty.action) ? actionProperty.action : null;
        }

        private static void EnableAction(InputAction action)
        {
            if (action != null)
            {
                action.Enable();
            }
        }

        private static void DisableAction(InputAction action)
        {
            if (action != null)
            {
                action.Disable();
            }
        }

        private static bool HasBindings(InputAction action)
        {
            return action != null && action.bindings.Count > 0;
        }

        private bool IsAlternativeUsePressed()
        {
            return resolvedAlternativeUseAction != null && resolvedAlternativeUseAction.IsPressed();
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private void OnValidate()
        {
            if (aimTarget == null)
            {
                aimTarget = transform;
            }

            if (playerInput == null)
            {
                playerInput = GetComponentInParent<PlayerInput>();
            }

            mouseSensitivity = Mathf.Max(0f, mouseSensitivity);
            cameraSpeedMultiplayer = Mathf.Max(0f, cameraSpeedMultiplayer);
            stickSensitivity = Mathf.Max(0f, stickSensitivity);
            minPitch = Mathf.Clamp(minPitch, -89f, 89f);
            maxPitch = Mathf.Clamp(maxPitch, minPitch, 89f);
        }
    }
}
