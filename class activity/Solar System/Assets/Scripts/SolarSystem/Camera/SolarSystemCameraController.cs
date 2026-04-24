using UnityEngine;

internal sealed class SolarSystemCameraController : MonoBehaviour
{
    private Transform homeTarget;
    private Transform focusTarget;
    private float distance;
    private float targetDistance;
    private float minDistance;
    private float maxDistance;
    private float homeDistance;
    private float yaw = -28f;
    private float pitch = 28f;
    private Vector3 panOffset;
    private Vector3 smoothFocusPoint;
    private Vector3 focusVelocity;

    public void Initialize(Transform targetTransform, float startDistance, float minimumDistance, float maximumDistance)
    {
        homeTarget = targetTransform;
        distance = startDistance;
        targetDistance = startDistance;
        homeDistance = startDistance;
        minDistance = minimumDistance;
        maxDistance = maximumDistance;
        panOffset = Vector3.zero;
        smoothFocusPoint = targetTransform.position;
        UpdateCameraTransform(true);
    }

    public void FocusOn(Transform targetTransform, float desiredDistance)
    {
        focusTarget = targetTransform;
        targetDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
    }

    public void ReturnHome()
    {
        focusTarget = null;
        panOffset = Vector3.zero;
        targetDistance = homeDistance;
    }

    private void LateUpdate()
    {
        if (homeTarget == null)
        {
            return;
        }

        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * 3.2f;
            pitch -= Input.GetAxis("Mouse Y") * 2.6f;
            pitch = Mathf.Clamp(pitch, -8f, 84f);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetDistance = Mathf.Clamp(targetDistance * (1f - scroll * 0.12f), minDistance, maxDistance);
        }

        if (focusTarget == null)
        {
            float panSpeed = Mathf.Max(0.4f, distance * 0.018f) * Time.deltaTime;
            Vector3 right = transform.right;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            if (Input.GetKey(KeyCode.A))
            {
                panOffset -= right * panSpeed;
            }
            if (Input.GetKey(KeyCode.D))
            {
                panOffset += right * panSpeed;
            }
            if (Input.GetKey(KeyCode.W))
            {
                panOffset += forward * panSpeed;
            }
            if (Input.GetKey(KeyCode.S))
            {
                panOffset -= forward * panSpeed;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                panOffset -= Vector3.up * panSpeed;
            }
            if (Input.GetKey(KeyCode.E))
            {
                panOffset += Vector3.up * panSpeed;
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                panOffset = Vector3.zero;
            }
        }
        else
        {
            panOffset = Vector3.Lerp(panOffset, Vector3.zero, Time.deltaTime * 8f);
        }

        UpdateCameraTransform(false);
    }

    private void UpdateCameraTransform(bool snap)
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPoint = focusTarget != null ? focusTarget.position : homeTarget.position + panOffset;
        float smoothTime = focusTarget != null ? 0.14f : 0.24f;

        if (snap)
        {
            smoothFocusPoint = targetPoint;
            distance = targetDistance;
        }
        else
        {
            smoothFocusPoint = Vector3.SmoothDamp(smoothFocusPoint, targetPoint, ref focusVelocity, smoothTime);
            distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * 5f);
        }

        transform.position = smoothFocusPoint + rotation * new Vector3(0f, 0f, -distance);
        transform.rotation = rotation;
        transform.LookAt(smoothFocusPoint);
    }
}
