using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;

namespace WeatherStation.Tests;

// Kleines Subject, das die geschützten Methoden von Subject<T> für Tests öffentlich macht.
public sealed class TestSubject<T> : Subject<T>
{
    public TestSubject(bool sendLastValueToNewObservers = false) : base(sendLastValueToNewObservers)
    {
    }

    public void Publish(T value) => NotifyObservers(value);

    public void Stop() => NotifyStopped();
}

// Observer, der alles aufschreibt, was er empfängt.
public sealed class RecordingObserver<T> : IWeatherObserver<T>
{
    private readonly object _lock = new();
    private readonly List<T> _values = new();
    private int _stoppedCount;

    public string Name => "Recorder";

    public List<T> Values
    {
        get
        {
            lock (_lock)
            {
                return new List<T>(_values);
            }
        }
    }

    public int StoppedCount
    {
        get
        {
            lock (_lock)
            {
                return _stoppedCount;
            }
        }
    }

    public void Update(T value)
    {
        lock (_lock)
        {
            _values.Add(value);
        }
    }

    public void StationStopped()
    {
        lock (_lock)
        {
            _stoppedCount++;
        }
    }
}

public static class TestData
{
    public static readonly DateTime Start = new(2026, 1, 1, 12, 0, 0);

    public static WeatherReading Reading(
        double temperature = 15,
        double humidity = 50,
        double pressure = 1013,
        double windSpeed = 10,
        int minutes = 0)
    {
        return new WeatherReading(Start.AddMinutes(minutes), temperature, humidity, pressure, windSpeed);
    }
}
