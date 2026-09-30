namespace WeatherStation.Core.Observer;

// ============================================================
// PATTERN: Observer – Rolle: Subject (das beobachtete Objekt)
// Das Subject verwaltet die Liste seiner Observer und benachrichtigt sie.
// Es kennt seine Observer nur über das Interface IObserver<T> (lose Kopplung).
//
// Thread-Sicherheit (zwei Sperren mit klarer Aufgabe):
// 1. _lock schützt die Observer-Liste. Jede Änderung erzeugt ein NEUES Array
//    (Copy-on-Write). Beim Verteilen wird eine Momentaufnahme benutzt – so darf
//    sich ein Observer mitten in einer Benachrichtigung ab- oder anmelden.
// 2. _deliveryLock sorgt dafür, dass immer nur EINE Meldung gleichzeitig verteilt
//    wird. Dadurch kommen die Werte bei jedem Observer in der richtigen Reihenfolge an,
//    der "Replay"-Wert kommt nie NACH einem neueren Wert, und nach OnCompleted
//    kommt garantiert kein OnNext mehr.
// ============================================================
public abstract class Subject<T> : IObservable<T>
{
    private readonly object _lock = new();
    private readonly object _deliveryLock = new();
    private readonly bool _replayLastValue;

    private IObserver<T>[] _observers = [];
    private bool _isCompleted;
    private bool _hasLastValue;
    private T? _lastValue;

    protected Subject(bool replayLastValue = false)
    {
        _replayLastValue = replayLastValue;
    }

    // Wird ausgelöst, wenn ein Observer in OnNext eine Exception wirft.
    // Die anderen Observer werden trotzdem benachrichtigt (Fehlerisolation).
    public event Action<ObserverError>? ObserverFailed;

    public int ObserverCount
    {
        get
        {
            lock (_lock)
            {
                return _observers.Length;
            }
        }
    }

    public bool IsCompleted
    {
        get
        {
            lock (_lock)
            {
                return _isCompleted;
            }
        }
    }

    public IReadOnlyList<string> GetObserverNames()
    {
        IObserver<T>[] snapshot;
        lock (_lock)
        {
            snapshot = _observers;
        }

        var names = new List<string>();
        foreach (IObserver<T> observer in snapshot)
        {
            names.Add(GetName(observer));
        }
        return names;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        // Während der Anmeldung (inkl. Replay) darf keine andere Meldung verteilt werden,
        // sonst könnte der neue Observer erst den neuen und DANACH den alten Wert bekommen.
        lock (_deliveryLock)
        {
            bool alreadyCompleted;
            bool replay = false;
            T? replayValue = default;

            lock (_lock)
            {
                alreadyCompleted = _isCompleted;
                if (!alreadyCompleted)
                {
                    // Copy-on-Write: neues Array mit dem zusätzlichen Observer
                    var newArray = new IObserver<T>[_observers.Length + 1];
                    Array.Copy(_observers, newArray, _observers.Length);
                    newArray[^1] = observer;
                    _observers = newArray;

                    if (_replayLastValue && _hasLastValue)
                    {
                        replay = true;
                        replayValue = _lastValue;
                    }
                }
            }

            if (alreadyCompleted)
            {
                // Zu spät angemeldet: sofort "fertig" melden, es gibt nichts zu beobachten.
                observer.OnCompleted();
                return EmptySubscription.Instance;
            }

            if (replay)
            {
                DeliverToOne(observer, replayValue!);
            }

            return new Subscription(this, observer);
        }
    }

    protected void Notify(T value)
    {
        lock (_deliveryLock)
        {
            IObserver<T>[] snapshot;
            lock (_lock)
            {
                if (_isCompleted)
                {
                    return;
                }
                _lastValue = value;
                _hasLastValue = true;
                snapshot = _observers;
            }

            // Außerhalb von _lock: Observer dürfen sich hier selbst ab- oder anmelden.
            foreach (IObserver<T> observer in snapshot)
            {
                // Hat ein Observer das Subject gerade beendet (z. B. Stop() in OnNext),
                // bekommen die restlichen Observer kein OnNext mehr nach ihrem OnCompleted.
                if (IsCompleted)
                {
                    return;
                }
                DeliverToOne(observer, value);
            }
        }
    }

    protected void NotifyCompleted()
    {
        lock (_deliveryLock)
        {
            IObserver<T>[]? snapshot = Complete();
            if (snapshot == null)
            {
                return;
            }

            foreach (IObserver<T> observer in snapshot)
            {
                try
                {
                    observer.OnCompleted();
                }
                catch (Exception exception)
                {
                    ObserverFailed?.Invoke(new ObserverError(GetName(observer), exception));
                }
            }
        }
    }

    protected void NotifyError(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        lock (_deliveryLock)
        {
            IObserver<T>[]? snapshot = Complete();
            if (snapshot == null)
            {
                return;
            }

            foreach (IObserver<T> observer in snapshot)
            {
                try
                {
                    observer.OnError(error);
                }
                catch (Exception exception)
                {
                    ObserverFailed?.Invoke(new ObserverError(GetName(observer), exception));
                }
            }
        }
    }

    // Markiert das Subject als beendet und leert die Liste.
    // Gibt die bisherigen Observer zurück (null, wenn schon beendet).
    private IObserver<T>[]? Complete()
    {
        lock (_lock)
        {
            if (_isCompleted)
            {
                return null;
            }
            _isCompleted = true;
            IObserver<T>[] snapshot = _observers;
            _observers = [];
            return snapshot;
        }
    }

    // Ein fehlerhafter Observer darf die anderen nicht stören (Fehlerisolation).
    private void DeliverToOne(IObserver<T> observer, T value)
    {
        try
        {
            observer.OnNext(value);
        }
        catch (Exception exception)
        {
            ObserverFailed?.Invoke(new ObserverError(GetName(observer), exception));
        }
    }

    private static string GetName(IObserver<T> observer)
    {
        if (observer is INamedObserver named)
        {
            return named.Name;
        }
        return observer.GetType().Name;
    }

    private void Unsubscribe(IObserver<T> observer)
    {
        lock (_lock)
        {
            // Nur GENAU EINE Anmeldung entfernen (derselbe Observer kann mehrfach angemeldet sein).
            int index = Array.IndexOf(_observers, observer);
            if (index < 0)
            {
                return;
            }

            var newArray = new IObserver<T>[_observers.Length - 1];
            Array.Copy(_observers, 0, newArray, 0, index);
            Array.Copy(_observers, index + 1, newArray, index, _observers.Length - index - 1);
            _observers = newArray;
        }
    }

    // Das "Abmelde-Token": Wer Dispose() aufruft, wird nicht mehr benachrichtigt.
    private sealed class Subscription : IDisposable
    {
        private Subject<T>? _subject;
        private readonly IObserver<T> _observer;

        public Subscription(Subject<T> subject, IObserver<T> observer)
        {
            _subject = subject;
            _observer = observer;
        }

        public void Dispose()
        {
            // Interlocked: auch bei parallelem Mehrfach-Dispose wird nur einmal abgemeldet.
            Subject<T>? subject = Interlocked.Exchange(ref _subject, null);
            subject?.Unsubscribe(_observer);
        }
    }

    private sealed class EmptySubscription : IDisposable
    {
        public static readonly EmptySubscription Instance = new();

        public void Dispose()
        {
        }
    }
}
