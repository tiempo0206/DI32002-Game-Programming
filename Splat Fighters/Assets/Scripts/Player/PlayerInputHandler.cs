using UnityEngine;

/// <summary>
/// Minimal keyboard and mouse input bridge for the MVP player controller.
/// This can later be replaced by Unity's New Input System without changing PlayerController.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Move Input")]
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] private string verticalAxis = "Vertical";

    [Header("Action Keys")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;

    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool FireHeld { get; private set; }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw(horizontalAxis);
        float vertical = Input.GetAxisRaw(verticalAxis);

        MoveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        JumpPressed = Input.GetKeyDown(jumpKey);
        FireHeld = Input.GetKey(fireKey);
    }
}
