using RPGame.Core.Movement;
using RPGame.Core.Statistics;
using RPGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace RPGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMovement : MonoBehaviour, IMovement
    {
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private StatisticsController statisticsController;

        [Header("Input")]
        [SerializeField] private InputActionProperty moveAction;
        [SerializeField] private InputActionProperty sprintAction;
        [SerializeField] private InputActionProperty jumpAction;
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string jumpActionName = "Jump";

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float deceleration = 22f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private bool rotateWithCamera = true;
        [SerializeField] private float sprintForwardThreshold = 0.1f;
        [SerializeField, Range(0f, 1f)] private float sprintSidewaysMultiplier = 0.35f;

        [Header("Stamina")]
        [SerializeField] private float sprintStaminaCostPerSecond = 20f;
        [SerializeField] private float sprintMinimumStaminaToStart = 10f;
        [FormerlySerializedAs("sprintStartStamina")]
        [SerializeField] private float sprintStartStaminaCost = 5f;

        [Header("Jump And Gravity")]
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float jumpStaminaCost = 10f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedVerticalVelocity = -2f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        private Vector3 verticalVelocity;
        private Vector3 groundedMoveVelocity;
        private Vector3 lockedAirMoveVelocity;
        private InputAction resolvedMoveAction;
        private InputAction resolvedSprintAction;
        private InputAction resolvedJumpAction;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private int movementBlockCount;
        private bool isAirMoveLocked;
        private bool isSprinting;
        private bool isBlockedByDeath;
        private readonly MovementSpeedModifiers movementSpeedModifiers = new();

        public bool IsGrounded { get; private set; }
        public bool IsSprinting => isSprinting;
        public Vector2 MoveInput { get; private set; }
        public Vector3 HorizontalVelocity => isAirMoveLocked ? lockedAirMoveVelocity : groundedMoveVelocity;
        public float VerticalSpeed => verticalVelocity.y;

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }

            if (statisticsController == null)
            {
                statisticsController = GetComponent<StatisticsController>();
            }

            ResolveInputActions();
        }

        private void OnEnable()
        {
            ResolveInputActions();

            EnableAction(resolvedMoveAction);
            EnableAction(resolvedSprintAction);
            EnableAction(resolvedJumpAction);

            if (resolvedJumpAction != null)
            {
                resolvedJumpAction.performed += OnJumpPerformed;
            }

            if (statisticsController != null)
            {
                statisticsController.Died -= HandleDied;
                statisticsController.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (resolvedJumpAction != null)
            {
                resolvedJumpAction.performed -= OnJumpPerformed;
            }

            if (statisticsController != null)
            {
                statisticsController.Died -= HandleDied;
            }

            DisableAction(resolvedMoveAction);
            DisableAction(resolvedSprintAction);
            DisableAction(resolvedJumpAction);
        }

        private void OnDestroy()
        {
            if (statisticsController != null)
            {
                statisticsController.Died -= HandleDied;
            }
        }

        private void Update()
        {
            if (GameplayInputBlocker.IsBlocked)
            {
                StopNormalMovement();
                return;
            }

            if (IsMovementBlocked)
            {
                StopNormalMovement();
                ApplyVerticalMovement();
                return;
            }

            if (resolvedJumpAction == null && WasFallbackJumpPressed())
            {
                BufferJump();
            }

            IsGrounded = characterController.isGrounded;

            if (IsGrounded)
            {
                coyoteTimer = coyoteTime;
                isAirMoveLocked = false;

                if (verticalVelocity.y < 0f)
                {
                    verticalVelocity.y = groundedVerticalVelocity;
                }

                Vector3 targetMoveVelocity = GetCameraRelativeTargetMoveVelocity();
                float speedChangeRate = GetSpeedChangeRate(targetMoveVelocity);
                groundedMoveVelocity = Vector3.MoveTowards(
                    groundedMoveVelocity,
                    targetMoveVelocity,
                    speedChangeRate * Time.deltaTime);

            }
            else
            {
                coyoteTimer -= Time.deltaTime;

                if (!isAirMoveLocked)
                {
                    lockedAirMoveVelocity = groundedMoveVelocity;
                    isAirMoveLocked = true;
                }
            }

            UpdateJumpBuffer();
            verticalVelocity.y += gravity * Time.deltaTime;

            Vector3 horizontalVelocity = isAirMoveLocked ? lockedAirMoveVelocity : groundedMoveVelocity;
            characterController.Move((horizontalVelocity + verticalVelocity) * Time.deltaTime);
            RotateTowardsCameraForward();
        }

        private Vector3 GetCameraRelativeTargetMoveVelocity()
        {
            Vector2 moveInput = ReadMoveInput();

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            MoveInput = moveInput;

            Transform movementCamera = cameraTransform != null ? cameraTransform : transform;
            Vector3 cameraForward = movementCamera.forward;
            Vector3 cameraRight = movementCamera.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            UpdateSprintState(moveInput);

            if (isSprinting)
            {
                moveInput.x *= sprintSidewaysMultiplier;
                moveInput.y = Mathf.Max(moveInput.y, 1f);
            }

            Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
            float speed = GetModifiedSpeed(isSprinting ? sprintSpeed : walkSpeed);

            return moveDirection.sqrMagnitude > 1f ? moveDirection.normalized * speed : moveDirection * speed;
        }

        private float GetSpeedChangeRate(Vector3 targetMoveVelocity)
        {
            if (targetMoveVelocity.sqrMagnitude < 0.0001f || groundedMoveVelocity.sqrMagnitude < 0.0001f)
            {
                return targetMoveVelocity.sqrMagnitude < 0.0001f ? deceleration : acceleration;
            }

            bool isSlowingDown = targetMoveVelocity.sqrMagnitude < groundedMoveVelocity.sqrMagnitude;
            bool isChangingDirection = Vector3.Dot(groundedMoveVelocity.normalized, targetMoveVelocity.normalized) < 0f;

            if (isSlowingDown || isChangingDirection)
            {
                return deceleration;
            }

            return acceleration;
        }

        private void UpdateJumpBuffer()
        {
            if (jumpBufferTimer <= 0f)
            {
                return;
            }

            if (IsGrounded || coyoteTimer > 0f)
            {
                if (!TrySpendJumpStamina())
                {
                    jumpBufferTimer = 0f;
                    return;
                }

                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lockedAirMoveVelocity = groundedMoveVelocity;
                isAirMoveLocked = true;
                IsGrounded = false;
                coyoteTimer = 0f;
                jumpBufferTimer = 0f;
                return;
            }

            jumpBufferTimer -= Time.deltaTime;
        }

        private Vector2 ReadMoveInput()
        {
            if (resolvedMoveAction != null)
            {
                return resolvedMoveAction.ReadValue<Vector2>();
            }

            Vector2 input = Vector2.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    input.x -= 1f;
                }

                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    input.x += 1f;
                }

                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                {
                    input.y -= 1f;
                }

                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                {
                    input.y += 1f;
                }
            }

            if (Gamepad.current != null)
            {
                input += Gamepad.current.leftStick.ReadValue();
            }

            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private bool WasSprintInputPressedThisFrame()
        {
            if (resolvedSprintAction != null)
            {
                return resolvedSprintAction.WasPressedThisFrame();
            }

            return Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame
                || Keyboard.current != null && Keyboard.current.rightShiftKey.wasPressedThisFrame
                || Gamepad.current != null && Gamepad.current.leftStickButton.wasPressedThisFrame;
        }

        private void UpdateSprintState(Vector2 moveInput)
        {
            bool isForwardPressed = moveInput.y > sprintForwardThreshold;
            bool isBackwardPressed = IsBackwardInputPressed(moveInput);

            if (isBackwardPressed)
            {
                isSprinting = false;
                return;
            }

            if (isSprinting && WasSprintInputPressedThisFrame())
            {
                isSprinting = false;
                return;
            }

            if (!isSprinting && (!WasSprintInputPressedThisFrame() || !isForwardPressed || !TrySpendSprintStartStamina()))
            {
                return;
            }

            if (!isSprinting)
            {
                isSprinting = true;
            }

            if (isSprinting && !TrySpendSprintStamina())
            {
                isSprinting = false;
            }
        }

        private bool TrySpendSprintStartStamina()
        {
            if (statisticsController == null)
            {
                return true;
            }

            if (statisticsController.CurrentStamina < sprintMinimumStaminaToStart)
            {
                return false;
            }

            if (sprintStartStaminaCost <= 0f)
            {
                return true;
            }

            if (!statisticsController.CanSpendStamina(sprintStartStaminaCost))
            {
                return false;
            }

            return statisticsController.TrySpendStamina(sprintStartStaminaCost);
        }

        private bool TrySpendSprintStamina()
        {
            if (statisticsController == null || sprintStaminaCostPerSecond <= 0f)
            {
                return true;
            }

            float staminaCost = sprintStaminaCostPerSecond * Time.deltaTime;

            if (statisticsController.CanSpendStamina(staminaCost))
            {
                return statisticsController.TrySpendStamina(staminaCost);
            }

            if (statisticsController.CurrentStamina > 0f)
            {
                statisticsController.TrySpendStamina(statisticsController.CurrentStamina);
            }

            return false;
        }

        private bool TrySpendJumpStamina()
        {
            if (statisticsController == null || jumpStaminaCost <= 0f)
            {
                return true;
            }

            return statisticsController.TrySpendStamina(jumpStaminaCost);
        }

        private bool IsBackwardInputPressed(Vector2 moveInput)
        {
            if (moveInput.y < -sprintForwardThreshold)
            {
                return true;
            }

            return Keyboard.current != null && Keyboard.current.sKey.isPressed
                || Keyboard.current != null && Keyboard.current.downArrowKey.isPressed
                || Gamepad.current != null && Gamepad.current.leftStick.ReadValue().y < -sprintForwardThreshold;
        }

        private void RotateTowardsCameraForward()
        {
            if (!rotateWithCamera)
            {
                return;
            }

            Transform movementCamera = cameraTransform != null ? cameraTransform : transform;
            Vector3 direction = movementCamera.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            float rotationStep = Mathf.Clamp01(rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationStep);
        }

        private void StopNormalMovement()
        {
            MoveInput = Vector2.zero;
            isSprinting = false;
            isAirMoveLocked = false;
            jumpBufferTimer = 0f;
            groundedMoveVelocity = Vector3.zero;
            lockedAirMoveVelocity = Vector3.zero;
        }

        private void ApplyVerticalMovement()
        {
            IsGrounded = characterController.isGrounded;
            if (IsGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = groundedVerticalVelocity;
            }

            verticalVelocity.y += gravity * Time.deltaTime;
            characterController.Move(verticalVelocity * Time.deltaTime);
        }

        private bool IsMovementBlocked => movementBlockCount > 0;

        private float GetModifiedSpeed(float baseSpeed)
        {
            return baseSpeed * movementSpeedModifiers.Multiplier;
        }

        public void BlockMovement()
        {
            movementBlockCount++;
            StopNormalMovement();
        }

        private void HandleDied()
        {
            if (isBlockedByDeath)
            {
                return;
            }

            isBlockedByDeath = true;
            BlockMovement();
        }

        public void UnblockMovement()
        {
            movementBlockCount = Mathf.Max(0, movementBlockCount - 1);
        }

        public int AddMovementSpeedModifier(float multiplier)
        {
            return movementSpeedModifiers.Add(multiplier);
        }

        public void RemoveMovementSpeedModifier(int modifierId)
        {
            movementSpeedModifiers.Remove(modifierId);
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            BufferJump();
        }

        private void BufferJump()
        {
            jumpBufferTimer = Mathf.Max(jumpBufferTime, Time.deltaTime);
        }

        private void ResolveInputActions()
        {
            resolvedMoveAction = ResolveAction(moveAction, moveActionName);
            resolvedSprintAction = ResolveAction(sprintAction, sprintActionName);
            resolvedJumpAction = ResolveAction(jumpAction, jumpActionName);
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

        private static bool WasFallbackJumpPressed()
        {
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame
                || Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        }

        private void OnValidate()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }

            if (statisticsController == null)
            {
                statisticsController = GetComponent<StatisticsController>();
            }

            walkSpeed = Mathf.Max(0f, walkSpeed);
            sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            acceleration = Mathf.Max(0f, acceleration);
            deceleration = Mathf.Max(0f, deceleration);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            sprintForwardThreshold = Mathf.Max(0.01f, sprintForwardThreshold);
            sprintSidewaysMultiplier = Mathf.Clamp01(sprintSidewaysMultiplier);
            jumpHeight = Mathf.Max(0f, jumpHeight);
            jumpStaminaCost = Mathf.Max(0f, jumpStaminaCost);
            gravity = Mathf.Min(-0.01f, gravity);
            groundedVerticalVelocity = Mathf.Min(0f, groundedVerticalVelocity);
            coyoteTime = Mathf.Max(0f, coyoteTime);
            jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
            sprintStaminaCostPerSecond = Mathf.Max(0f, sprintStaminaCostPerSecond);
            sprintMinimumStaminaToStart = Mathf.Max(0f, sprintMinimumStaminaToStart);
            sprintStartStaminaCost = Mathf.Max(0f, sprintStartStaminaCost);
        }
    }
}
