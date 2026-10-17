using PanBug.Domain.Tasks;
using PanBug.Domain.Tasks.Bill;
using PanBug.Domain.Tasks.Cinema;
using PanBug.Domain.Tasks.Delivery;
using PanBug.Domain.Tasks.Leave;
using PanBug.Domain.Tasks.Loan;
using PanBug.Domain.Tasks.Login;
using PanBug.Domain.Tasks.Parcel;
using PanBug.Domain.Tasks.Todo;

namespace PanBug.Web.Tasks;

/// <summary>Dobiera komponent widoku do symulacji (trzy zadania jednej aplikacji dzielą widok).</summary>
public static class TaskComponentRegistry
{
    public static Type ComponentFor(ITaskSimulation simulation) => simulation switch
    {
        CinemaSimulationBase => typeof(CinemaTask),
        ParcelSimulationBase => typeof(ParcelTask),
        LoanSimulationBase => typeof(LoanTask),
        LeaveSimulationBase => typeof(LeaveTask),
        LoginSimulationBase => typeof(LoginTask),
        TodoSimulationBase => typeof(TodoTask),
        BillSimulationBase => typeof(BillTask),
        DeliverySimulationBase => typeof(DeliveryTask),
        _ => throw new NotSupportedException($"Brak widoku dla zadania {simulation.Number}."),
    };
}
