namespace WeatherStation.Core.Observer;

// ============================================================
// PATTERN: Observer – Rolle: konkreter Observer aus einer Lambda
// Für kleine Zuhörer, bei denen sich eine eigene Klasse nicht lohnt:
//   new ActionObserver<WeatherReading>("Anzeige", reading => Console.WriteLine(reading));
// ============================================================
public sealed class ActionObserver<T> : IWeatherObserver<T>
{
    private readonly Action<T> _onUpdate;
    private readonly Action? _onStopped;

    public ActionObserver(string name, Action<T> onUpdate, Action? onStopped = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(onUpdate);
        Name = name;
        _onUpdate = onUpdate;
        _onStopped = onStopped;
    }

    public string Name { get; }

    public void Update(T value) => _onUpdate(value);

    public void StationStopped() => _onStopped?.Invoke();
}
