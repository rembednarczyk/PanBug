using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks;

/// <summary>
/// Pomysł na test w karcie misji: nazwa heurystyki i konkretne pytanie do aplikacji.
/// <see cref="Area"/> nie jest pokazywany graczowi. Służy tylko testowi, który pilnuje,
/// żeby karta nie pokrywała się z kategoriami defektów aplikacji (zob. <see cref="MissionCharter"/>).
/// </summary>
public sealed record TestIdea(string Heuristic, string Question, DefectCategory Area);

/// <summary>
/// Karta misji aplikacji, jak w sesji testów eksploracyjnych: cel i kilka pomysłów, od czego
/// zacząć. Wszystkie trzy zadania aplikacji mają tę samą kartę (tak jak ten sam opis wymagań),
/// więc karta nie zdradza, czym różnią się zadania. Każda karta zawiera co najmniej jeden pomysł
/// z obszaru, w którym aplikacja nie ma defektu, żeby lista nie była ściągą z kategorii.
/// </summary>
public sealed record MissionCharter(string Goal, IReadOnlyList<TestIdea> Ideas)
{
    /// <summary>Proponowany czas sesji na jedno zadanie, w minutach.</summary>
    public const int SuggestedMinutes = 10;
}
