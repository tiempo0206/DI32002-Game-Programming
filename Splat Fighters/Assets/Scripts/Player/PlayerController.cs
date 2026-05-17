using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic third-person player controller for the MVP.
/// Uses CharacterController for predictable movement without full Rigidbody physics setup.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
[DefaultExecutionOrder(100)]
public class PlayerController : MonoBehaviour
{
    private enum RotationMode
    {
        MoveDirection,
        CameraForward,
        ExternalAim
    }

    [Header("References")]
    [SerializeField] private Transform cameraTransform = null;
    [SerializeField] private InkWeapon weapon = null;
    [SerializeField] private AimController aimController = null;
    [SerializeField] private PlayerToolSwitcher toolSwitcher = null;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private RotationMode rotationMode = RotationMode.MoveDirection;

    [Header("Swim Form")]
    [SerializeField] private Team playerTeam = Team.TeamA;
    [SerializeField, Min(1f)] private float swimMoveSpeedMultiplier = 1.55f;
    [SerializeField, Range(0.1f, 1f)] private float enemyPaintMoveSpeedMultiplier = 0.55f;
    [SerializeField, Min(1f)] private float swimInkRecoveryMultiplier = 1.8f;
    [SerializeField] private bool disableFireWhileSwimming = true;
    [SerializeField] private Transform groundProbe = null;
    [SerializeField] private GameObject swimFormVisual = null;
    [SerializeField] private Renderer[] humanoidRenderers = new Renderer[0];

    [Header("Paint Routes")]
    [SerializeField] private bool enablePaintRoutes = true;
    [SerializeField, Min(0.1f)] private float paintRouteProbeRadius = 0.7f;
    [SerializeField] private Vector3 paintRouteProbeOffset = new Vector3(0f, 0.45f, 0f);

    [Header("Jump And Gravity")]
    [SerializeField] private bool enableJump = true;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedStickForce = -2f;

    private CharacterController characterController;
    private PlayerInputHandler input;
    private float verticalVelocity;
    private bool isOnOwnPaint;
    private bool isOnEnemyPaint;
    private bool isSwimming;
    private bool isUsingPaintRoute;
    private PaintRouteSurface activePaintRoute;
    private readonly Collider[] paintRouteHits = new Collider[12];

    public bool IsOnOwnPaint => isOnOwnPaint;
    public bool IsOnEnemyPaint => isOnEnemyPaint;
    public bool IsSwimming => isSwimming;
    public bool IsUsingPaintRoute => isUsingPaintRoute;
    public bool WantsToSwim => input != null && input.SwimHeld;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputHandler>();

        if (weapon == null)
        {
            weapon = GetComponentInChildren<InkWeapon>();
        }

        if (aimController == null)
        {
            aimController = GetComponentInChildren<AimController>();
        }

        if (toolSwitcher == null)
        {
            toolSwitcher = GetComponentInChildren<PlayerToolSwitcher>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (groundProbe == null)
        {
            groundProbe = transform;
        }

        if (humanoidRenderers == null || humanoidRenderers.Length == 0)
        {
            humanoidRenderers = FindDefaultHumanoidRenderers();
        }

        ApplySwimVisualState(false);
    }

    private void Update()
    {
        Vector3 moveDirection = GetCameraRelativeMoveDirection(input.MoveInput);

        UpdateSwimState();
        ApplyRotation(moveDirection);
        ApplyJumpAndGravity();
        MoveCharacter(moveDirection);
        ApplySwimVisualState(isSwimming);
    }

    private void LateUpdate()
    {
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
        if (rotationMode == RotationMode.ExternalAim)
        {
            return;
        }

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
        if (isUsingPaintRoute)
        {
            verticalVelocity = 0f;
            return;
        }

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
        float currentMoveSpeed = moveSpeed;

        if (isSwimming)
        {
            currentMoveSpeed *= swimMoveSpeedMultiplier;
        }
        else if (isOnEnemyPaint)
        {
            currentMoveSpeed *= enemyPaintMoveSpeedMultiplier;
        }

        Vector3 velocity = moveDirection * currentMoveSpeed;

        if (isUsingPaintRoute && activePaintRoute != null)
        {
            Vector3 routeVelocity = activePaintRoute.GetRouteVelocity();
            velocity += routeVelocity;
            velocity.y = Mathf.Max(routeVelocity.y, 0f);
        }
        else
        {
            velocity.y = verticalVelocity;
        }

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleFireInput()
    {
        if (disableFireWhileSwimming && isSwimming)
        {
            return;
        }

        if (!input.FireHeld || weapon == null || toolSwitcher != null && !toolSwitcher.IsShooterActive)
        {
            return;
        }

        if (aimController != null)
        {
            aimController.RefreshAimNow();
        }

        weapon.TryFire();
    }

    public void ResetMotionState()
    {
        verticalVelocity = 0f;
        isSwimming = false;
        isUsingPaintRoute = false;
        isOnOwnPaint = false;
        isOnEnemyPaint = false;
        activePaintRoute = null;

        if (weapon != null)
        {
            weapon.SetExternalRecoveryMultiplier(1f);
            weapon.SetExternalFireBlocked(false);
        }

        if (toolSwitcher != null)
        {
            toolSwitcher.ResetToDefaultTool();
        }

        ApplySwimVisualState(false);
    }

    private void UpdateSwimState()
    {
        Team groundTeam = GetGroundTeamUnderPlayer();
        isOnOwnPaint = groundTeam == playerTeam;
        isOnEnemyPaint = groundTeam != Team.None && playerTeam != Team.None && groundTeam != playerTeam;
        activePaintRoute = ResolveActivePaintRoute();
        isUsingPaintRoute = input.SwimHeld && activePaintRoute != null;
        isSwimming = input.SwimHeld && (isOnOwnPaint || isUsingPaintRoute);

        if (weapon != null)
        {
            weapon.SetExternalRecoveryMultiplier(isSwimming ? swimInkRecoveryMultiplier : 1f);
            bool toolBlocksShooter = toolSwitcher != null && !toolSwitcher.IsShooterActive;
            weapon.SetExternalFireBlocked(disableFireWhileSwimming && isSwimming || toolBlocksShooter);
        }
    }

    private Team GetGroundTeamUnderPlayer()
    {
        if (PaintManager.Instance == null || playerTeam == Team.None)
        {
            return Team.None;
        }

        Vector3 probePosition = groundProbe != null ? groundProbe.position : transform.position;

        if (!PaintManager.Instance.TryGetTeamAtWorldPosition(probePosition, out Team groundTeam))
        {
            return Team.None;
        }

        return groundTeam;
    }

    private PaintRouteSurface ResolveActivePaintRoute()
    {
        if (!enablePaintRoutes || !input.SwimHeld || playerTeam == Team.None)
        {
            return null;
        }

        Vector3 probeCenter = transform.position + paintRouteProbeOffset;
        int hitCount = Physics.OverlapSphereNonAlloc(
            probeCenter,
            paintRouteProbeRadius,
            paintRouteHits,
            ~0,
            QueryTriggerInteraction.Collide);

        PaintRouteSurface bestRoute = null;
        float bestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = paintRouteHits[i];
            paintRouteHits[i] = null;

            if (hit == null)
            {
                continue;
            }

            PaintRouteSurface route = hit.GetComponentInParent<PaintRouteSurface>();

            if (route == null || !route.IsActiveForTeam(playerTeam))
            {
                continue;
            }

            float distanceSqr = (route.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            bestRoute = route;
        }

        return bestRoute;
    }

    private void ApplySwimVisualState(bool swimming)
    {
        for (int i = 0; i < humanoidRenderers.Length; i++)
        {
            Renderer humanoidRenderer = humanoidRenderers[i];

            if (humanoidRenderer != null)
            {
                humanoidRenderer.enabled = !swimming;
            }
        }

        if (swimFormVisual != null && swimFormVisual.activeSelf != swimming)
        {
            swimFormVisual.SetActive(swimming);
        }
    }

    private Renderer[] FindDefaultHumanoidRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        if (swimFormVisual == null)
        {
            return renderers;
        }

        List<Renderer> humanoidOnly = new List<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer candidate = renderers[i];

            if (candidate == null || candidate.transform.IsChildOf(swimFormVisual.transform))
            {
                continue;
            }

            humanoidOnly.Add(candidate);
        }

        return humanoidOnly.ToArray();
    }
}
