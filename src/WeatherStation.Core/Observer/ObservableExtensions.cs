namespace WeatherStation.Core.Observer;

public static class ObservableExtensions
{
    // Kurzform: station.Subscribe("Bildschirm", reading => ...)
    public static IDisposable Subscribe<T>(this IObservable<T> source, string name, Action<T> onNext)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Subscribe(new ActionObserver<T>(name, onNext));
    }

    // Filter-"Operator" wie in Rx: warnings.Where(w => w.Level == WarningLevel.Danger)
    public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(filter);
        return new FilteredObservable<T>(source, filter);
    }

    // ============================================================
    // PATTERN: Observer – Filter als "Zwischenstation"
    // Diese Klasse ist selbst ein IObservable: Sie meldet beim Subscribe einen
    // inneren Observer an der Quelle an, der nur passende Werte weiterreicht.
    // Das Abmelde-Token der Quelle wird einfach durchgereicht.
    // ============================================================
    private sealed class FilteredObservable<T> : IObservable<T>
    {
        private readonly IObservable<T> _source;
        private readonly Func<T, bool> _filter;

        public FilteredObservable(IObservable<T> source, Func<T, bool> filter)
        {
            _source = source;
            _filter = filter;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);

            string outerName = observer is INamedObserver named ? named.Name : observer.GetType().Name;
            var inner = new FilterObserver<T>(outerName + " (gefiltert)", observer, _filter);
            return _source.Subscribe(inner);
        }
    }

    private sealed class FilterObserver<T> : IObserver<T>, INamedObserver
    {
        private readonly IObserver<T> _target;
        private readonly Func<T, bool> _filter;

        public FilterObserver(string name, IObserver<T> target, Func<T, bool> filter)
        {
            Name = name;
            _target = target;
            _filter = filter;
        }

        public string Name { get; }

        public void OnNext(T value)
        {
            if (_filter(value))
            {
                _target.OnNext(value);
            }
        }

        // Fehler und "fertig" werden immer weitergereicht.
        public void OnError(Exception error) => _target.OnError(error);

        public void OnCompleted() => _target.OnCompleted();
    }
}
