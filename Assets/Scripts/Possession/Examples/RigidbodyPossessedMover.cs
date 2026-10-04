using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Example object-specific controller. Put it beside Controllable on a Rigidbody
/// object; replace this component with a chair, lamp, or creature controller as needed.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class RigidbodyPossessedMover : MonoBehaviour, IPossessionInputReceiver
{
    [SerializeField, Min(0f)] private float acceleration = 25f;
    [SerializeField, Min(0f)] private float turnSpeed = 180f;

    private Rigidbody body;
    private bool receivesInput;
    private Vector2 moveInput;

    private void Awake() => body = GetComponent<Rigidbody>();

    public void SetPossessionInputEnabled(bool enabled)
    {
        receivesInput = enabled;
        if (!enabled)
            moveInput = Vector2.zero;
    }

    private void Update()
    {
        moveInput = receivesInput && Keyboard.current != null
            ? new Vector2(
                (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f),
                (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f))
            : Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!receivesInput)
            return;

        if (moveInput.sqrMagnitude > 0.001f)
        {
            Vector3 localDirection = new(moveInput.x, 0f, moveInput.y);
            Vector3 worldDirection = transform.TransformDirection(localDirection.normalized);
            body.AddForce(worldDirection * acceleration, ForceMode.Acceleration);
            Quaternion desiredRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, desiredRotation, turnSpeed * Time.fixedDeltaTime));
        }
    }
}
