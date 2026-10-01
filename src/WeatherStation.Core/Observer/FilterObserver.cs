namespace WeatherStation.Core.Observer;

// ============================================================
// PATTERN: Observer – Filter als "Hülle" um einen anderen Observer
// Der FilterObserver bekommt ALLE Werte, reicht aber nur die passenden an
// den eigentlichen Observer (target) weiter. Beispiel: nur Gefahr-Warnungen:
//   new FilterObserver<WeatherWarning>(banner, warning => warning.Level == WarningLevel.Danger)
// Das Subject merkt nichts davon – für es ist der Filter ein ganz normaler Observer.
// ============================================================
public sealed class FilterObserver<T> : IWeatherObserver<T>
{
    private readonly IWeatherObserver<T> _target;
    private readonly Func<T, bool> _filter;

    public FilterObserver(IWeatherObserver<T> target, Func<T, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(filter);
        _target = target;
        _filter = filter;
    }

    public string Name => _target.Name + " (gefiltert)";

    public void Update(T value)
    {
        if (_filter(value))
        {
            _target.Update(value);
        }
    }

    // "Station beendet" ist immer wichtig und wird nicht gefiltert.
    public void StationStopped() => _target.StationStopped();
}
