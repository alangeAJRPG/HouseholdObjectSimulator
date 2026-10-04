using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A target-agnostic third-person orbit camera. It never parents itself to a
/// possessed object, so it can smoothly follow physics objects and be retargeted.
/// </summary>
public sealed class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 focusOffset = new(0f, 1f, 0f);

    [Header("Orbit")]
    [SerializeField, Min(0.1f)] private float distance = 5f;
    [SerializeField, Range(-89f, 0f)] private float minPitch = -35f;
    [SerializeField, Range(0f, 89f)] private float maxPitch = 70f;
    [SerializeField, Min(0.01f)] private float lookSensitivity = 0.12f;
    [SerializeField, Min(0f)] private float followSmoothTime = 0.25f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.2f;
    [SerializeField, Min(0f)] private float collisionPadding = 0.05f;

    private float yaw;
    private float pitch = 15f;
    private Vector3 smoothedFocus;
    private Vector3 focusVelocity;
    private bool lookInputEnabled = true;
    private bool discardNextLookDelta;

    public bool IsTransitioning { get; private set; }

    private void Awake()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = NormalizePitch(angles.x);
        if (target != null)
            smoothedFocus = GetFocusPoint();
    }

    public void SetTarget(Transform nextTarget, bool snap = false)
    {
        target = nextTarget;
        focusVelocity = Vector3.zero;
        IsTransitioning = target != null && !snap &&
                          (smoothedFocus - GetFocusPoint()).sqrMagnitude > 0.0001f;

        if (target != null && snap)
            smoothedFocus = GetFocusPoint();
    }

    public void SetLookInputEnabled(bool enabled)
    {
        if (enabled && !lookInputEnabled)
            discardNextLookDelta = true;

        lookInputEnabled = enabled;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        ReadLookInput();
        Vector3 desiredFocus = GetFocusPoint();
        smoothedFocus = followSmoothTime <= 0f
            ? desiredFocus
            : Vector3.SmoothDamp(smoothedFocus, desiredFocus, ref focusVelocity, followSmoothTime);

        if (IsTransitioning && (smoothedFocus - desiredFocus).sqrMagnitude <= 0.0001f)
        {
            smoothedFocus = desiredFocus;
            focusVelocity = Vector3.zero;
            IsTransitioning = false;
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = smoothedFocus - rotation * Vector3.forward * distance;
        transform.position = ResolveCollision(smoothedFocus, desiredPosition);
        transform.rotation = rotation;
    }

    private void ReadLookInput()
    {
        if (!lookInputEnabled || Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        if (discardNextLookDelta)
        {
            discardNextLookDelta = false;
            return;
        }

        yaw += delta.x * lookSensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, minPitch, maxPitch);
    }

    private Vector3 ResolveCollision(Vector3 focus, Vector3 desiredPosition)
    {
        Vector3 direction = desiredPosition - focus;
        float desiredDistance = direction.magnitude;
        if (desiredDistance <= 0f)
            return focus;

        if (Physics.SphereCast(focus, collisionRadius, direction / desiredDistance, out RaycastHit hit,
                desiredDistance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            return focus + direction / desiredDistance * Mathf.Max(0f, hit.distance - collisionPadding);
        }

        return desiredPosition;
    }

    private Vector3 GetFocusPoint() => target.position + focusOffset;

    private static float NormalizePitch(float value) => value > 180f ? value - 360f : value;
}
