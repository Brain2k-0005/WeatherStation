using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;

namespace WeatherStation.Core.Observers;

// ============================================================
// PATTERN: Observer – Rolle: konkreter Observer
// Merkt sich die letzten Warnungen (neueste zuerst), z. B. für eine Tabelle.
// Der lock schützt die Liste, weil Meldungen aus einem Hintergrund-Thread
// kommen, während die Oberfläche gleichzeitig liest.
// ============================================================
public sealed class WarningHistory : IObserver<WeatherWarning>, INamedObserver
{
    private const int MaxCount = 500;

    private readonly object _lock = new();
    private readonly List<WeatherWarning> _warnings = new();

    public string Name => "Warn-Verlauf";

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _warnings.Count;
            }
        }
    }

    // Gibt eine Kopie zurück, damit der Aufrufer nie in die laufende Änderung hineinliest.
    public IReadOnlyList<WeatherWarning> GetAll()
    {
        lock (_lock)
        {
            return _warnings.ToArray();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _warnings.Clear();
        }
    }

    public void OnNext(WeatherWarning warning)
    {
        lock (_lock)
        {
            // Neueste zuerst: vorne einfügen.
            _warnings.Insert(0, warning);
            if (_warnings.Count > MaxCount)
            {
                _warnings.RemoveAt(_warnings.Count - 1);
            }
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }
}
