using System.Linq;
using UnityEngine;

/// <summary>
/// Toggles the assigned lights whenever its Controllable receives an action.
/// If no lights are assigned, all lights on this object and its children are used.
/// </summary>
public sealed class LampControllableAction : MonoBehaviour, IControllableAction
{
    [Tooltip("Lights toggled by this action. Leave empty to use child lights automatically.")]
    [SerializeField] private Light[] lights;

    private void Awake()
    {
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light>(true);
    }

    public void Action()
    {
        bool enableLights = lights.Any(light => light != null && !light.enabled);

        foreach (Light light in lights)
        {
            if (light != null)
                light.enabled = enableLights;
        }
    }
}
