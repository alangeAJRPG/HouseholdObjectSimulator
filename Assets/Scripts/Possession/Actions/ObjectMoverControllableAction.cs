using System.Collections;
using UnityEngine;

/// <summary>
/// Animates a target between its starting pose and a configured alternate pose.
/// </summary>
public sealed class ObjectMoverControllableAction : MonoBehaviour, IControllableAction
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [Tooltip("Optional pivot. When assigned, rotation occurs around this pivot using its local axes.")]
    [SerializeField] private Transform pivot;

    [Header("Alternate Pose")]
    [Tooltip("Euler-angle rotation applied at the alternate pose.")]
    [SerializeField] private Vector3 rotationChange;
    [Tooltip("Position offset applied at the alternate pose.")]
    [SerializeField] private Vector3 positionChange;
    [SerializeField] private bool positionChangeIsLocal = true;

    [Header("Animation")]
    [Tooltip("Seconds to wait before rotating.")]
    [SerializeField, Min(0f)] private float rotationDelay;
    [Tooltip("Rotation time in seconds. Higher values rotate more slowly.")]
    [SerializeField, Min(0f)] private float rotationDuration = 0.5f;
    [Tooltip("Seconds to wait before moving.")]
    [SerializeField, Min(0f)] private float positionDelay;
    [Tooltip("Position-move time in seconds. Higher values move more slowly.")]
    [SerializeField, Min(0f)] private float positionDuration = 0.5f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Repeat")]
    [Tooltip("When disabled, the alternate pose is applied only once.")]
    [SerializeField] private bool repeatable = true;

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private bool isAtAlternatePose;
    private bool hasTriggered;
    private Coroutine animationRoutine;

    private void Awake()
    {
        if (target == null)
            target = transform;

        startingPosition = target.position;
        startingRotation = target.rotation;
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = null;
    }

    public void Action()
    {
        if (animationRoutine != null || (!repeatable && hasTriggered))
            return;

        bool moveToAlternatePose = !isAtAlternatePose;
        animationRoutine = StartCoroutine(Animate(moveToAlternatePose));
    }

    private IEnumerator Animate(bool moveToAlternatePose)
    {
        Vector3 fromPosition = target.position;
        Quaternion fromRotation = target.rotation;
        Vector3 toPosition = moveToAlternatePose ? AlternatePosition() : startingPosition;
        Quaternion toRotation = moveToAlternatePose ? AlternateRotation() : startingRotation;
        float totalDuration = Mathf.Max(rotationDelay + rotationDuration, positionDelay + positionDuration);

        if (totalDuration <= 0f)
        {
            target.SetPositionAndRotation(toPosition, toRotation);
        }
        else
        {
            for (float elapsed = 0f; elapsed < totalDuration; elapsed += Time.deltaTime)
            {
                float rotationProgress = Progress(elapsed, rotationDelay, rotationDuration);
                float positionProgress = Progress(elapsed, positionDelay, positionDuration);
                target.SetPositionAndRotation(
                    Vector3.LerpUnclamped(fromPosition, toPosition, Ease(positionProgress)),
                    Quaternion.SlerpUnclamped(fromRotation, toRotation, Ease(rotationProgress)));
                yield return null;
            }

            target.SetPositionAndRotation(toPosition, toRotation);
        }

        isAtAlternatePose = moveToAlternatePose;
        hasTriggered |= moveToAlternatePose;
        animationRoutine = null;
    }

    private Vector3 AlternatePosition()
    {
        Vector3 alternatePosition = startingPosition;
        Quaternion pivotRotation = PivotRotationDelta();

        if (pivot != null && rotationChange.sqrMagnitude > 0f)
            alternatePosition = pivot.position + pivotRotation * (startingPosition - pivot.position);

        Vector3 offset = positionChangeIsLocal ? startingRotation * positionChange : positionChange;
        return alternatePosition + offset;
    }

    private Quaternion AlternateRotation()
    {
        if (pivot == null)
            return startingRotation * Quaternion.Euler(rotationChange);

        return PivotRotationDelta() * startingRotation;
    }

    private Quaternion PivotRotationDelta()
    {
        if (pivot == null)
            return Quaternion.Euler(rotationChange);

        return pivot.rotation * Quaternion.Euler(rotationChange) * Quaternion.Inverse(pivot.rotation);
    }

    private float Ease(float progress) => easing.Evaluate(Mathf.Clamp01(progress));

    private static float Progress(float elapsed, float delay, float duration)
    {
        return duration <= 0f ? (elapsed >= delay ? 1f : 0f) : (elapsed - delay) / duration;
    }
}
