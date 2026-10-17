using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks;

/// <summary>
/// Czysta logika domenowa pojedynczego zadania: stan i zachowanie wraz z
/// wstrzykniętym defektem, w pełni testowalna bez warstwy UI. Komponent widoku
/// jest osobnym adapterem odwołującym się do konkretnego typu.
/// </summary>
public interface ITaskSimulation
{
    uint Number { get; }
    PointValue Points { get; }
    string Description { get; }

    /// <summary>Kategorie defektów prezentowane w oknie zgłoszenia.</summary>
    IReadOnlyList<Answer> Categories { get; }

    /// <summary>Log akcji wykonanych w bieżącym przebiegu zadania.</summary>
    IReadOnlyList<uint> Actions { get; }

    /// <summary>Przywraca zadanie do stanu początkowego.</summary>
    void Reset();
}
