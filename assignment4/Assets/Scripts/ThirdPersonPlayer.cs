using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class ThirdPersonPlayer : MonoBehaviour
{
    public Transform view;
    public Animator animator;
    [Tooltip("The rectangular, axis-aligned ground the player must stay on.")]
    public Renderer movementArea;
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float turnSpeed = 12f;
    public float gravity = -24f;

    private CharacterController controller;
    private float verticalSpeed;
    private Vector3 spawnPosition;
    private static readonly int Speed = Animator.StringToHash("Speed");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        spawnPosition = transform.position;
        if (view == null && Camera.main != null) view = Camera.main.transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        Vector2 input = Vector2.zero;
        bool running = false;
        // The Input System handles Game-view and application focus for these devices.
        if (keyboard != null && keyboard.enabled)
        {
            input.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            running = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 forward = view != null ? Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 direction = forward * input.y + right * input.x;
        float speed = running ? runSpeed : walkSpeed;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }

        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;
        Vector3 displacement = (direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime;
        if (movementArea != null)
        {
            Bounds bounds = movementArea.bounds;
            // Keep the entire controller capsule inside the plane, including its skin.
            float margin = (controller.radius + controller.skinWidth) *
                Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            Vector3 next = transform.position + displacement;
            float halfWidth = Mathf.Max(0f, bounds.extents.x - margin);
            float halfDepth = Mathf.Max(0f, bounds.extents.z - margin);
            next.x = Mathf.Clamp(next.x, bounds.center.x - halfWidth, bounds.center.x + halfWidth);
            next.z = Mathf.Clamp(next.z, bounds.center.z - halfDepth, bounds.center.z + halfDepth);
            displacement = next - transform.position;
        }
        controller.Move(displacement);
        if (animator != null)
        {
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(controller.velocity, Vector3.up);
            animator.SetFloat(Speed, horizontalVelocity.magnitude, 0.12f, Time.deltaTime);
        }

        // Recover from unexpected vertical displacement without allowing edge crossings.
        if (transform.position.y < -20f)
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            controller.enabled = true;
            verticalSpeed = 0f;
        }
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(16, 16, 230, 32), "WASD  Move     Shift  Run");
    }
}
