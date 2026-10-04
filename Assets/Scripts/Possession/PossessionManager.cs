using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Owns the one active possession. Nothing else needs to know how to swap
/// objects or how the camera follows them.
/// </summary>
public sealed class PossessionManager : MonoBehaviour
{
    [SerializeField] private OrbitCamera orbitCamera;
    [SerializeField] private PossessionHud possessionHud;
    [Header("Possession Mode Vignette")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField, Range(0f, 1f)] private float possessionModeVignetteIntensity = 0.12f;
    [SerializeField, Min(0.01f)] private float vignetteLerpSpeed = 5f;

    public IControllable Current { get; private set; }
    public bool IsPossessionMode { get; private set; }
    public bool IsInputLocked { get; private set; }
    public event Action<IControllable> PossessionChanged;
    public event Action<bool> PossessionModeChanged;

    private Vignette vignette;

    private void Awake()
    {
        if (possessionHud == null)
            possessionHud = GetComponent<PossessionHud>();

        if (possessionHud == null)
            possessionHud = gameObject.AddComponent<PossessionHud>();

        if (postProcessVolume == null)
            postProcessVolume = FindFirstObjectByType<Volume>();

        if (postProcessVolume != null)
        {
            VolumeProfile profile = postProcessVolume.profile;
            if (!profile.TryGet(out vignette))
                vignette = profile.Add<Vignette>(true);

            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0f;
        }

        SetPossessionMode(false);
    }

    private void Update()
    {
        if (IsInputLocked)
        {
            if (orbitCamera == null || !orbitCamera.IsTransitioning)
                FinishPossessionTransition();

            return;
        }

        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            SetPossessionMode(!IsPossessionMode);
            return;
        }

        if (!IsPossessionMode && Mouse.current.leftButton.wasPressedThisFrame)
            Current?.OnAction(this);
    }

    private void LateUpdate()
    {
        if (vignette == null)
            return;

        float targetIntensity = IsPossessionMode ? possessionModeVignetteIntensity : 0f;
        float interpolation = 1f - Mathf.Exp(-vignetteLerpSpeed * Time.deltaTime);
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, interpolation);
    }

    public void SetPossessionMode(bool enabled)
    {
        IsPossessionMode = enabled;
        orbitCamera?.SetLookInputEnabled(!enabled);
        Cursor.lockState = enabled ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = enabled;
        PossessionModeChanged?.Invoke(enabled);
    }

    public bool TryPossess(IControllable next)
    {
        if (next == null || !next.CanBePossessed || ReferenceEquals(Current, next))
            return false;

        Current?.OnReleased(this);
        Current = next;
        Current.OnPossessed(this);
        Current.SetPossessionInputEnabled(false);
        orbitCamera?.SetTarget(Current.CameraTarget);
        PossessionChanged?.Invoke(Current);
        IsInputLocked = true;
        SetPossessionMode(true);
        return true;
    }

    public void ReleaseCurrent()
    {
        if (Current == null)
            return;

        Current.OnReleased(this);
        Current = null;
        orbitCamera?.SetTarget(null);
        PossessionChanged?.Invoke(null);
    }

    private void FinishPossessionTransition()
    {
        IsInputLocked = false;
        Current?.SetPossessionInputEnabled(true);
        SetPossessionMode(false);
    }
}
