using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;

namespace WeatherStation.Core.Services;

// ============================================================
// PATTERN: Observer – Rolle: Subject
// Die Wetterstation meldet Messwerte. Sie weiß nicht, wer zuhört
// (Bildschirm, Warn-Dienst, Statistik ...) – sie ruft nur Notify().
// replayLastValue: Wer sich später anmeldet, bekommt sofort den letzten Messwert
// und muss nicht auf die nächste Messung warten.
// ============================================================
public sealed class Station : Subject<WeatherReading>
{
    private readonly object _stateLock = new();
    private WeatherReading? _lastReading;
    private int _readingCount;

    public Station(string name) : base(replayLastValue: true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Die Station braucht einen Namen.", nameof(name));
        }
        Name = name;
    }

    public string Name { get; }

    public WeatherReading? LastReading
    {
        get
        {
            lock (_stateLock)
            {
                return _lastReading;
            }
        }
    }

    public int ReadingCount
    {
        get
        {
            lock (_stateLock)
            {
                return _readingCount;
            }
        }
    }

    public void Report(WeatherReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        if (IsCompleted)
        {
            throw new InvalidOperationException("Die Station wurde gestoppt und nimmt keine Messwerte mehr an.");
        }

        Validate(reading);

        lock (_stateLock)
        {
            _lastReading = reading;
            _readingCount++;
        }

        Notify(reading);
    }

    public void Stop()
    {
        NotifyCompleted();
    }

    // Unmögliche Werte (Sensorfehler) werden abgewiesen, bevor sie jemand sieht.
    private static void Validate(WeatherReading reading)
    {
        CheckRange(reading.Temperature, -60, 60, nameof(reading.Temperature), "°C");
        CheckRange(reading.Humidity, 0, 100, nameof(reading.Humidity), "%");
        CheckRange(reading.Pressure, 850, 1100, nameof(reading.Pressure), "hPa");
        CheckRange(reading.WindSpeed, 0, 300, nameof(reading.WindSpeed), "km/h");
    }

    private static void CheckRange(double value, double min, double max, string name, string unit)
    {
        // "!(a && b)" statt "a || b": so wird auch NaN abgewiesen.
        if (!(value >= min && value <= max))
        {
            throw new ArgumentOutOfRangeException(name, value,
                $"{name} muss zwischen {min} und {max} {unit} liegen.");
        }
    }
}
