using UnityEngine;

/// <summary>
/// Minimal third-person follow camera for the course-project MVP.
/// Keeps the camera behind and above the player with smooth movement.
/// </summary>
[DefaultExecutionOrder(50)]
public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target = null;

    [Header("Follow")]
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 4.5f, -6f);
    [SerializeField] private bool useTargetRotation = true;
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 12f;

    [Header("Look Target")]
    [SerializeField] private float lookAtHeight = 1.25f;
    [SerializeField] private float lookAheadDistance = 1.5f;
    [SerializeField] private float lookSideOffset = 0.25f;

    [Header("Mouse Orbit")]
    [SerializeField] private bool enableMouseOrbit = true;
    [SerializeField] private bool lockCursorOnPlay = true;
    [SerializeField] private string mouseXAxis = "Mouse X";
    [SerializeField] private string mouseYAxis = "Mouse Y";
    [SerializeField] private float yawSensitivity = 180f;
    [SerializeField] private float pitchSensitivity = 120f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float shoulderOffset = 0.75f;
    [SerializeField] private float orbitDistance = 6.5f;
    [SerializeField] private float initialPitch = 18f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 65f;

    private float yaw;
    private float pitch;
    private bool orbitInitialized;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Awake()
    {
        InitializeOrbitAngles();
        ApplyCursorLock();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateOrbitInput();

        Vector3 desiredPosition = GetDesiredPosition();
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            GetSharpness(positionSmoothSpeed));

        Quaternion desiredRotation = enableMouseOrbit ? GetCameraRotation() : GetFollowLookRotation();
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            GetSharpness(rotationSmoothSpeed));
    }

    private Vector3 GetDesiredPosition()
    {
        if (enableMouseOrbit)
        {
            Quaternion cameraRotation = GetCameraRotation();
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 focusPoint = GetFocusPoint();

            return focusPoint
                - cameraRotation * Vector3.forward * orbitDistance
                + yawRotation * Vector3.right * shoulderOffset;
        }

        if (!useTargetRotation)
        {
            return target.position + followOffset;
        }

        return target.TransformPoint(followOffset);
    }

    private Vector3 GetFocusPoint()
    {
        Vector3 baseTarget = target.position + Vector3.up * lookAtHeight;

        if (!enableMouseOrbit)
        {
            return baseTarget;
        }

        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
        return baseTarget + yawRotation * new Vector3(lookSideOffset, 0f, lookAheadDistance);
    }

    private Quaternion GetCameraRotation()
    {
        return Quaternion.Euler(pitch, yaw, 0f);
    }

    private void InitializeOrbitAngles()
    {
        if (orbitInitialized)
        {
            return;
        }

        yaw = target != null ? target.eulerAngles.y : transform.eulerAngles.y;
        pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
        orbitInitialized = true;
    }

    private void UpdateOrbitInput()
    {
        if (!enableMouseOrbit)
        {
            return;
        }

        InitializeOrbitAngles();

        float mouseX = Input.GetAxis(mouseXAxis);
        float mouseY = Input.GetAxis(mouseYAxis);
        float ySign = invertY ? 1f : -1f;

        yaw += mouseX * yawSensitivity * Time.deltaTime;
        pitch += mouseY * pitchSensitivity * ySign * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void ApplyCursorLock()
    {
        if (!Application.isPlaying || !lockCursorOnPlay)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private float GetSharpness(float speed)
    {
        if (speed <= 0f)
        {
            return 1f;
        }

        return 1f - Mathf.Exp(-speed * Time.deltaTime);
    }

    [ContextMenu("Snap To Target")]
    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.position = GetDesiredPosition();
        transform.rotation = enableMouseOrbit ? GetCameraRotation() : GetFollowLookRotation();
    }

    private Quaternion GetFollowLookRotation()
    {
        Vector3 lookDirection = GetFocusPoint() - transform.position;

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private void OnValidate()
    {
        positionSmoothSpeed = Mathf.Max(0f, positionSmoothSpeed);
        rotationSmoothSpeed = Mathf.Max(0f, rotationSmoothSpeed);
        lookAheadDistance = Mathf.Max(0f, lookAheadDistance);
        yawSensitivity = Mathf.Max(0f, yawSensitivity);
        pitchSensitivity = Mathf.Max(0f, pitchSensitivity);
        shoulderOffset = Mathf.Max(0f, shoulderOffset);
        orbitDistance = Mathf.Max(0.1f, orbitDistance);
        minPitch = Mathf.Clamp(minPitch, -89f, 89f);
        maxPitch = Mathf.Clamp(maxPitch, minPitch, 89f);
        initialPitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
    }
}
