/// <summary>
/// Implement this on a movement, ability, or animation controller that should
/// only read player input while its Controllable is possessed.
/// </summary>
public interface IPossessionInputReceiver
{
    void SetPossessionInputEnabled(bool enabled);
}
