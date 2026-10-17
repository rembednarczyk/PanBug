using Microsoft.AspNetCore.Components;
using PanBug.Domain.Tasks;
using PanBug.Domain.Tasks.Common;

namespace PanBug.Web.Tasks;

/// <summary>
/// Baza komponentu zadania: widok nad symulacją domenową. Komponent tylko
/// prezentuje stan i przekazuje akcje - logika z defektem żyje w symulacji.
/// </summary>
public abstract class TaskComponentBase<TSimulation> : ComponentBase where TSimulation : class
{
    [Parameter, EditorRequired] public ITaskSimulation Simulation { get; set; } = default!;

    protected TSimulation Sim => (TSimulation)Simulation;

    /// <summary>Komunikat dla gracza pod formularzem zadania.</summary>
    protected string? Message { get; set; }
    protected bool MessageIsError { get; set; } = true;

    protected void ShowResult(OperationResult result, string? successText = null)
    {
        Message = result.Success ? successText ?? result.Message : result.Message;
        MessageIsError = !result.Success;
    }

    protected void ShowError(string? text)
    {
        Message = text;
        MessageIsError = true;
    }

    protected void ClearMessage() => Message = null;
}
