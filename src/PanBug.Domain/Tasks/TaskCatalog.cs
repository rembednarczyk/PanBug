using PanBug.Domain.Tasks.Bill;
using PanBug.Domain.Tasks.Cinema;
using PanBug.Domain.Tasks.Delivery;
using PanBug.Domain.Tasks.Leave;
using PanBug.Domain.Tasks.Loan;
using PanBug.Domain.Tasks.Login;
using PanBug.Domain.Tasks.Parcel;
using PanBug.Domain.Tasks.Todo;

namespace PanBug.Domain.Tasks;

/// <summary>Rejestr i fabryka 24 symulacji zadań: osiem aplikacji, w każdej trzy warianty z innym defektem.</summary>
public static class TaskCatalog
{
    public const int TaskCount = 24;

    private static readonly IReadOnlyDictionary<uint, Func<ITaskSimulation>> Factories =
        new Dictionary<uint, Func<ITaskSimulation>>
        {
            [1] = () => new Task01Cinema(),
            [2] = () => new Task02Cinema(),
            [3] = () => new Task03Cinema(),
            [4] = () => new Task04Parcel(),
            [5] = () => new Task05Parcel(),
            [6] = () => new Task06Parcel(),
            [7] = () => new Task07Loan(),
            [8] = () => new Task08Loan(),
            [9] = () => new Task09Loan(),
            [10] = () => new Task10Leave(),
            [11] = () => new Task11Leave(),
            [12] = () => new Task12Leave(),
            [13] = () => new Task13Login(),
            [14] = () => new Task14Login(),
            [15] = () => new Task15Login(),
            [16] = () => new Task16Todo(),
            [17] = () => new Task17Todo(),
            [18] = () => new Task18Todo(),
            [19] = () => new Task19Bill(),
            [20] = () => new Task20Bill(),
            [21] = () => new Task21Bill(),
            [22] = () => new Task22Delivery(),
            [23] = () => new Task23Delivery(),
            [24] = () => new Task24Delivery(),
        };

    public static IReadOnlyCollection<uint> Numbers { get; } = Factories.Keys.OrderBy(n => n).ToArray();

    public static ITaskSimulation Create(uint number) => Factories[number]();

    public static IReadOnlyList<ITaskSimulation> CreateAll() =>
        Enumerable.Range(1, TaskCount).Select(n => Create((uint)n)).ToList();
}
