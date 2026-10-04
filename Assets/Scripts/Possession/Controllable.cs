using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base component for a possessable scene object. Add specialised behaviour
/// (for example, a chair, toaster, or rolling ball controller) alongside it.
/// Components that implement IControllableAction receive the primary action.
/// </summary>
public class Controllable : MonoBehaviour, IControllable
{
    private const string OutlineShaderName = "HouseholdObjectSimulator/Controllable Outline";

    [Header("Possession")]
    [SerializeField] private bool canBePossessed = true;
    [Tooltip("Optional child transform used as the orbit camera's focus point.")]
    [SerializeField] private Transform cameraTarget;
    [Header("Display")]
    [Tooltip("Name shown in the possession HUD. Uses the GameObject name when left empty.")]
    [SerializeField] private string displayName;
    [SerializeField] private UnityEvent onPossessed;
    [SerializeField] private UnityEvent onReleased;
    [SerializeField] private Material outlineMaterial;
    private readonly List<IPossessionInputReceiver> inputReceivers = new();
    private readonly List<Renderer> outlineRenderers = new();

    public Transform CameraTarget => cameraTarget != null ? cameraTarget : transform;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public bool CanBePossessed => canBePossessed && isActiveAndEnabled;
    public bool IsPossessed { get; private set; }

    protected virtual void Awake()
    {
        CacheInputReceivers();
        CreateOutlineRenderers();
        SetInputEnabled(false);
    }

    public virtual void OnPossessed(PossessionManager manager)
    {
        IsPossessed = true;
        SetOutlineVisible(false);
        SetInputEnabled(true);
        onPossessed.Invoke();
    }

    public virtual void OnReleased(PossessionManager manager)
    {
        IsPossessed = false;
        SetOutlineVisible(false);
        SetInputEnabled(false);
        onReleased.Invoke();
    }

    public virtual void OnAction(PossessionManager manager)
    {
        // Look these up when clicked so actions added after Awake are still usable.
        // Child actions are included unless a nested Controllable owns them instead.
        foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is IControllableAction action
                && behaviour.isActiveAndEnabled
                && behaviour.GetComponentInParent<Controllable>() == this)
                action.Action();
        }
    }

    public void SetPossessionInputEnabled(bool enabled)
    {
        SetInputEnabled(enabled);
    }

    public void SetOutlineVisible(bool visible)
    {
        foreach (Renderer outlineRenderer in outlineRenderers)
            outlineRenderer.enabled = visible && !IsPossessed;
    }

    private void CacheInputReceivers()
    {
        inputReceivers.Clear();
        foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is IPossessionInputReceiver receiver)
                inputReceivers.Add(receiver);
        }
    }

    private void SetInputEnabled(bool enabled)
    {
        foreach (IPossessionInputReceiver receiver in inputReceivers)
            receiver.SetPossessionInputEnabled(enabled);
    }

    private void CreateOutlineRenderers()
    {
        if (outlineMaterial == null)
        {
            Shader outlineShader = Shader.Find(OutlineShaderName);
            if (outlineShader == null)
            {
                Debug.LogWarning($"Could not find the outline shader for {name}.", this);
                return;
            }

            outlineMaterial = new Material(outlineShader);
        }

        foreach (MeshRenderer sourceRenderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            GameObject outlineObject = new($"{sourceRenderer.name} Outline");
            outlineObject.transform.SetParent(sourceRenderer.transform, false);
            outlineObject.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            Material[] materials = new Material[sourceRenderer.sharedMaterials.Length];
            for (int index = 0; index < materials.Length; index++)
                materials[index] = outlineMaterial;

            outlineRenderer.sharedMaterials = materials;
            outlineRenderer.enabled = false;
            outlineRenderers.Add(outlineRenderer);
        }
    }
}
