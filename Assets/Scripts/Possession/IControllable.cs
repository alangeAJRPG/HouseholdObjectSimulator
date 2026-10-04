using UnityEngine;

/// <summary>
/// The small contract shared by every object that may be possessed.
/// Keep gameplay-specific controls on separate components rather than adding them here.
/// </summary>
public interface IControllable
{
    Transform CameraTarget { get; }
    string DisplayName { get; }
    bool CanBePossessed { get; }
    bool IsPossessed { get; }

    void OnPossessed(PossessionManager manager);
    void OnReleased(PossessionManager manager);
    void OnAction(PossessionManager manager);
    void SetPossessionInputEnabled(bool enabled);
}
