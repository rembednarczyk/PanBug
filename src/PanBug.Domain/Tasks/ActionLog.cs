namespace PanBug.Domain.Tasks;

/// <summary>
/// Log kodów akcji wykonywanych przez gracza w zadaniu. Wybrane kody są
/// markerami reprodukcji defektu i są wykorzystywane przez silnik punktacji.
/// </summary>
public interface IActionLog
{
    IReadOnlyList<uint> Actions { get; }
    void Log(uint code);
    void Clear();
}

public sealed class ActionLog : IActionLog
{
    private readonly List<uint> _actions = new();
    public IReadOnlyList<uint> Actions => _actions;
    public void Log(uint code) => _actions.Add(code);
    public void Clear() => _actions.Clear();
}
