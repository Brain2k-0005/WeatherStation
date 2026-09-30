using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;

namespace WeatherStation.Core.Observers;

// ============================================================
// PATTERN: Observer – Rolle: konkreter Observer
// Sammelt die letzten Messwerte (älteste zuerst) – ideal für Diagramme.
// Ist die Grenze erreicht, fliegt der älteste Messwert raus.
// ============================================================
public sealed class ReadingHistory : IObserver<WeatherReading>, INamedObserver
{
    private readonly object _lock = new();
    private readonly List<WeatherReading> _readings = new();
    private readonly int _maxCount;

    public ReadingHistory(int maxCount = 144)
    {
        if (maxCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "Es muss mindestens 1 Messwert gespeichert werden.");
        }
        _maxCount = maxCount;
    }

    public string Name => "Messwert-Verlauf";

    public IReadOnlyList<WeatherReading> GetAll()
    {
        lock (_lock)
        {
            return _readings.ToArray();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _readings.Clear();
        }
    }

    public void OnNext(WeatherReading reading)
    {
        lock (_lock)
        {
            _readings.Add(reading);
            if (_readings.Count > _maxCount)
            {
                _readings.RemoveAt(0);
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
