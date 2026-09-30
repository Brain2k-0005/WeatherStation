using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;
using WeatherStation.Core.Rules;

namespace WeatherStation.Core.Services;

// ============================================================
// PATTERN: Observer – ist GLEICHZEITIG Observer und Subject
// Als Observer bekommt der Warn-Dienst Messwerte von der Station (OnNext).
// Als Subject (Basisklasse Subject<WeatherWarning>) meldet er die erzeugten
// Warnungen an seine eigenen Observer weiter (Historie, Anzeige, ...).
// So entsteht eine Kette: Station -> WarningService -> Anzeige.
// ============================================================
public sealed class WarningService : Subject<WeatherWarning>, IObserver<WeatherReading>, INamedObserver
{
    // ------------------------------------------------------------
    // FÜR FORTGESCHRITTENE – beim ersten Lesen überspringen.
    // Thread-Sicherheit: Die Regeln haben Zustand und werden unter _ruleLock geprüft.
    // ------------------------------------------------------------
    private readonly object _ruleLock = new();
    private readonly List<IWarningRule> _rules;

    public WarningService(IEnumerable<IWarningRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = new List<IWarningRule>(rules);
    }

    public string Name => "Warn-Dienst";

    public IReadOnlyList<string> RuleNames
    {
        get
        {
            var names = new List<string>();
            foreach (IWarningRule rule in _rules)
            {
                names.Add(rule.Name);
            }
            return names;
        }
    }

    public void OnNext(WeatherReading reading)
    {
        // Regeln haben einen eigenen Zustand (z. B. "Warnung aktiv") und sind nicht thread-sicher.
        // Deshalb prüfen wir unter einem lock, melden aber außerhalb davon.
        var warnings = new List<WeatherWarning>();
        lock (_ruleLock)
        {
            foreach (IWarningRule rule in _rules)
            {
                warnings.AddRange(rule.Check(reading));
            }
        }

        foreach (WeatherWarning warning in warnings)
        {
            Notify(warning);
        }
    }

    public void OnError(Exception error) => NotifyError(error);

    public void OnCompleted() => NotifyCompleted();
}
