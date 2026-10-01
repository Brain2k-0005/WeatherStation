using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;

namespace WeatherStation.Core.Observers;

// ============================================================
// PATTERN: Observer – Rolle: konkreter Observer
// Rechnet mit jedem Messwert Minimum, Maximum und Mittelwert mit.
// Es werden keine Messwerte gespeichert, nur Summen – das spart Speicher.
// ============================================================
public sealed class WeatherStatistics : IWeatherObserver<WeatherReading>
{
    // ------------------------------------------------------------
    // FÜR FORTGESCHRITTENE – beim ersten Lesen überspringen.
    // Thread-Sicherheit: Der lock schützt die Summen, weil Hintergrund-Thread und Oberfläche gleichzeitig zugreifen.
    // ------------------------------------------------------------
    private readonly object _lock = new();
    private int _count;
    private double _minTemperature;
    private double _maxTemperature;
    private double _temperatureSum;
    private double _maxWindSpeed;

    public string Name => "Statistik";

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    public double? MinTemperature
    {
        get
        {
            lock (_lock)
            {
                return _count == 0 ? null : _minTemperature;
            }
        }
    }

    public double? MaxTemperature
    {
        get
        {
            lock (_lock)
            {
                return _count == 0 ? null : _maxTemperature;
            }
        }
    }

    public double? AverageTemperature
    {
        get
        {
            lock (_lock)
            {
                return _count == 0 ? null : _temperatureSum / _count;
            }
        }
    }

    public double? MaxWindSpeed
    {
        get
        {
            lock (_lock)
            {
                return _count == 0 ? null : _maxWindSpeed;
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _count = 0;
            _minTemperature = 0;
            _maxTemperature = 0;
            _temperatureSum = 0;
            _maxWindSpeed = 0;
        }
    }

    public void Update(WeatherReading reading)
    {
        lock (_lock)
        {
            if (_count == 0)
            {
                _minTemperature = reading.Temperature;
                _maxTemperature = reading.Temperature;
                _maxWindSpeed = reading.WindSpeed;
            }
            else
            {
                _minTemperature = Math.Min(_minTemperature, reading.Temperature);
                _maxTemperature = Math.Max(_maxTemperature, reading.Temperature);
                _maxWindSpeed = Math.Max(_maxWindSpeed, reading.WindSpeed);
            }

            _temperatureSum += reading.Temperature;
            _count++;
        }
    }

    public void StationStopped()
    {
        // Nichts zu tun: Der bisherige Stand bleibt einfach erhalten.
    }
}
