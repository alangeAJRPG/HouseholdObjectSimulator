using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles mouse-driven possession selection and hover feedback while the
/// PossessionManager is in possession mode.
/// </summary>
public sealed class PossessionSelector : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PossessionManager possessionManager;
    [SerializeField] private LayerMask selectableLayers = ~0;
    [SerializeField, Min(0f)] private float maxDistance = 30f;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    private Controllable hoveredControllable;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null || playerCamera == null || possessionManager == null)
            return;

        if (!possessionManager.IsPossessionMode || possessionManager.IsInputLocked)
        {
            SetHovered(null);
            return;
        }

        Controllable candidate = GetControllableUnderMouse();
        SetHovered(candidate);

        if (Mouse.current.leftButton.wasPressedThisFrame && candidate != null)
            possessionManager.TryPossess(candidate);
    }

    private void OnDisable()
    {
        SetHovered(null);
    }

    private Controllable GetControllableUnderMouse()
    {
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, selectableLayers, triggerInteraction))
            return null;

        Controllable controllable = hit.collider.GetComponentInParent<Controllable>();
        if (controllable == null || !controllable.CanBePossessed)
            return null;

        if (ReferenceEquals(controllable, possessionManager.Current))
        {
            controllable.SetOutlineVisible(false);
            return null;
        }

        return controllable;
    }

    private void SetHovered(Controllable next)
    {
        if (ReferenceEquals(hoveredControllable, next))
            return;

        hoveredControllable?.SetOutlineVisible(false);
        hoveredControllable = next;
        hoveredControllable?.SetOutlineVisible(true);
    }
}
