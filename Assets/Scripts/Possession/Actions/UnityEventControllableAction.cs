using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Invokes an inspector-configured UnityEvent when its Controllable receives an action.
/// </summary>
public sealed class UnityEventControllableAction : MonoBehaviour, IControllableAction
{
    [SerializeField] private UnityEvent onAction = new();
    [Tooltip("When disabled, this event can be invoked only once.")]
    [SerializeField] private bool repeatable = true;

    private bool hasInvoked;

    public void Action()
    {
        if (!repeatable && hasInvoked)
            return;

        onAction.Invoke();
        hasInvoked = true;
    }
}
