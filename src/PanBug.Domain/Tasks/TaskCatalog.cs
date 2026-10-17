using PanBug.Domain.Tasks.Bill;
using PanBug.Domain.Tasks.Cinema;
using PanBug.Domain.Tasks.Delivery;
using PanBug.Domain.Tasks.Leave;
using PanBug.Domain.Tasks.Loan;
using PanBug.Domain.Tasks.Login;
using PanBug.Domain.Tasks.Parcel;
using PanBug.Domain.Tasks.Todo;

namespace PanBug.Domain.Tasks;

/// <summary>
/// Rejestr i fabryka 24 symulacji zadań: osiem aplikacji, w każdej trzy warianty z innym defektem.
/// Zadania jednej aplikacji są rozrzucone po grze i nigdy nie sąsiadują, żeby powtórzony opis
/// wymagań nie zdradzał, że to ta sama aplikacja. Trudność rośnie z numerem zadania.
/// </summary>
public static class TaskCatalog
{
    public const int TaskCount = 24;

    private static readonly IReadOnlyDictionary<uint, Func<ITaskSimulation>> Factories =
        new Dictionary<uint, Func<ITaskSimulation>>
        {
            [1] = () => new Task01Cinema(),
            [2] = () => new Task02Parcel(),
            [3] = () => new Task03Leave(),
            [4] = () => new Task04Bill(),
            [5] = () => new Task05Login(),
            [6] = () => new Task06Parcel(),
            [7] = () => new Task07Loan(),
            [8] = () => new Task08Delivery(),
            [9] = () => new Task09Cinema(),
            [10] = () => new Task10Todo(),
            [11] = () => new Task11Bill(),
            [12] = () => new Task12Login(),
            [13] = () => new Task13Parcel(),
            [14] = () => new Task14Loan(),
            [15] = () => new Task15Todo(),
            [16] = () => new Task16Leave(),
            [17] = () => new Task17Delivery(),
            [18] = () => new Task18Cinema(),
            [19] = () => new Task19Login(),
            [20] = () => new Task20Loan(),
            [21] = () => new Task21Bill(),
            [22] = () => new Task22Leave(),
            [23] = () => new Task23Todo(),
            [24] = () => new Task24Delivery(),
        };

    public static IReadOnlyCollection<uint> Numbers { get; } = Factories.Keys.OrderBy(n => n).ToArray();

    public static ITaskSimulation Create(uint number) => Factories[number]();

    public static IReadOnlyList<ITaskSimulation> CreateAll() =>
        Enumerable.Range(1, TaskCount).Select(n => Create((uint)n)).ToList();
}
