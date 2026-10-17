using PanBug.Domain.Tasks;

namespace PanBug.Domain.Session;

/// <summary>
/// Stan sesyjny pojedynczego zadania (oddzielony od samej symulacji wg SRP):
/// czy ukończone, zgłoszone kategorie i przechwycony log akcji z chwili zgłoszenia.
/// </summary>
public sealed class TaskSlot
{
    public ITaskSimulation Simulation { get; }
    public bool IsCompleted { get; internal set; }
    public IReadOnlyList<uint> SelectedAnswerNumbers { get; internal set; } = Array.Empty<uint>();
    public IReadOnlyList<uint> CapturedActions { get; internal set; } = Array.Empty<uint>();

    public uint Number => Simulation.Number;

    public TaskSlot(ITaskSimulation simulation) => Simulation = simulation;
}
