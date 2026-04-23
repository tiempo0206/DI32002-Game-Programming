using UnityEngine;

/// <summary>
/// Basic third-person player controller for the MVP.
/// Uses CharacterController for predictable movement without full Rigidbody physics setup.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : MonoBehaviour
{
    private enum RotationMode
    {
        MoveDirection,
        CameraForward
    }

    [Header("References")]
    [SerializeField] private Transform cameraTransform = null;
    [SerializeField] private InkWeapon weapon = null;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private RotationMode rotationMode = RotationMode.MoveDirection;

    [Header("Jump And Gravity")]
    [SerializeField] private bool enableJump = true;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedStickForce = -2f;

    private CharacterController characterController;
    private PlayerInputHandler input;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputHandler>();

        if (weapon == null)
        {
            weapon = GetComponentInChildren<InkWeapon>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        Vector3 moveDirection = GetCameraRelativeMoveDirection(input.MoveInput);

        ApplyRotation(moveDirection);
        ApplyJumpAndGravity();
        MoveCharacter(moveDirection);
        HandleFireInput();
    }

    private Vector3 GetCameraRelativeMoveDirection(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            right = cameraTransform.right;
        }

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;
        return moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
    }

    private void ApplyRotation(Vector3 moveDirection)
    {
        Vector3 targetDirection = Vector3.zero;

        if (rotationMode == RotationMode.CameraForward && cameraTransform != null)
        {
            targetDirection = cameraTransform.forward;
            targetDirection.y = 0f;
        }
        else if (moveDirection.sqrMagnitude > 0.0001f)
        {
            targetDirection = moveDirection;
        }

        if (targetDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ApplyJumpAndGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        if (enableJump && characterController.isGrounded && input.JumpPressed)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void MoveCharacter(Vector3 moveDirection)
    {
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleFireInput()
    {
        if (!input.FireHeld || weapon == null)
        {
            return;
        }

        weapon.TryFire();
    }
}
