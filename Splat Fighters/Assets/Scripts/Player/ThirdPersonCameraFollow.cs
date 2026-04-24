using UnityEngine;

/// <summary>
/// Minimal third-person follow camera for the course-project MVP.
/// Keeps the camera behind and above the player with smooth movement.
/// </summary>
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

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = GetDesiredPosition();
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            GetSharpness(positionSmoothSpeed));

        Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;
        Vector3 lookDirection = lookTarget - transform.position;

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            GetSharpness(rotationSmoothSpeed));
    }

    private Vector3 GetDesiredPosition()
    {
        if (!useTargetRotation)
        {
            return target.position + followOffset;
        }

        return target.TransformPoint(followOffset);
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

        Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;
        Vector3 lookDirection = lookTarget - transform.position;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    private void OnValidate()
    {
        positionSmoothSpeed = Mathf.Max(0f, positionSmoothSpeed);
        rotationSmoothSpeed = Mathf.Max(0f, rotationSmoothSpeed);
    }
}
