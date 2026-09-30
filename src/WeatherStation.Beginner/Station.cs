namespace WeatherStation.Beginner;

// PATTERN: Observer – Rolle: Subject
// Die Station führt eine Liste von Zuhörern und sagt ihnen Bescheid,
// sobald es eine neue Messung gibt. Sie kennt nur das Interface IWeatherObserver, nicht die konkreten Klassen.
public class Station
{
    private readonly List<IWeatherObserver> _observers = new();

    public WeatherReading? Current { get; private set; }

    public int ObserverCount => _observers.Count;

    public void Subscribe(IWeatherObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (_observers.Contains(observer))
        {
            return; // doppelt anmelden bringt nichts
        }
        _observers.Add(observer);
    }

    public void Unsubscribe(IWeatherObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observers.Remove(observer);
    }

    public void SetReading(WeatherReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        Current = reading;
        NotifyObservers(reading);
    }

    private void NotifyObservers(WeatherReading reading)
    {
        // Kopie, damit sich ein Observer während der Schleife abmelden darf.
        foreach (IWeatherObserver observer in _observers.ToList())
        {
            observer.Update(reading);
        }
    }
}
