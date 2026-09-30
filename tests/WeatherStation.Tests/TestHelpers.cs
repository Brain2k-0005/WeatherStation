using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;

namespace WeatherStation.Tests;

// Kleines Subject, das die geschützten Methoden von Subject<T> für Tests öffentlich macht.
public sealed class TestSubject<T> : Subject<T>
{
    public TestSubject(bool replayLastValue = false) : base(replayLastValue)
    {
    }

    public void Publish(T value) => Notify(value);

    public void Complete() => NotifyCompleted();

    public void Fail(Exception error) => NotifyError(error);
}

// Observer, der alles aufschreibt, was er empfängt.
public sealed class RecordingObserver<T> : IObserver<T>
{
    private readonly object _lock = new();
    private readonly List<T> _values = new();

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

    public int CompletedCount { get; private set; }
    public List<Exception> Errors { get; } = new();

    public void OnNext(T value)
    {
        lock (_lock)
        {
            _values.Add(value);
        }
    }

    public void OnError(Exception error) => Errors.Add(error);

    public void OnCompleted() => CompletedCount++;
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
