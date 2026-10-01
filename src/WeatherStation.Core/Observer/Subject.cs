namespace WeatherStation.Core.Observer;

// ============================================================
// PATTERN: Observer – Rolle: Subject (das beobachtete Objekt)
// Das Subject verwaltet die Liste seiner Observer und benachrichtigt sie.
// Es kennt seine Observer nur über das Interface IWeatherObserver<T> (lose Kopplung).
// Die Methoden heißen wie in Stufe 1: Subscribe, Unsubscribe, NotifyObservers.
//
// Einfache Fassung zum Einstieg: siehe src/WeatherStation.Beginner/Station.cs
// (eine List, keine Threads, kein Lock). Diese Klasse hier ist die "Praxis-Version".
//
// So heißt das in .NET (nur als Ausblick): IObservable<T>/IObserver<T> mit
// OnNext (= Update), OnCompleted (= StationStopped) und OnError.
// Abmelden geht dort über ein IDisposable-Token statt über Unsubscribe.
//
// ------------------------------------------------------------
// FÜR FORTGESCHRITTENE – beim ersten Lesen überspringen.
// Alles zu Thread-Sicherheit: die zwei Sperren (_lock, _deliveryLock), Copy-on-Write
// und das Wiederholen des letzten Werts unter der Sperre. Nötig, weil in der Web-App
// ein Hintergrunddienst meldet, während Browser-Seiten sich an- und abmelden.
// ------------------------------------------------------------
// Thread-Sicherheit (zwei Sperren mit klarer Aufgabe):
// 1. _lock schützt die Observer-Liste. Jede Änderung erzeugt ein NEUES Array
//    (Copy-on-Write). Beim Verteilen wird eine Momentaufnahme benutzt – so darf
//    sich ein Observer mitten in einer Benachrichtigung ab- oder anmelden.
// 2. _deliveryLock sorgt dafür, dass immer nur EINE Meldung gleichzeitig verteilt
//    wird. Dadurch kommen die Werte bei jedem Observer in der richtigen Reihenfolge an,
//    der letzte Wert für einen neuen Observer kommt nie NACH einem neueren Wert, und nach
//    NotifyStopped kommt garantiert kein Update mehr.
// ============================================================
public abstract class Subject<T>
{
    private readonly object _lock = new();
    private readonly object _deliveryLock = new();
    private readonly bool _sendLastValueToNewObservers;

    private IWeatherObserver<T>[] _observers = [];
    private bool _isStopped;
    private bool _hasLastValue;
    private T? _lastValue;

    // sendLastValueToNewObservers: Ein neuer Observer bekommt sofort den letzten Wert
    // (sonst müsste er bis zur nächsten Messung warten).
    protected Subject(bool sendLastValueToNewObservers = false)
    {
        _sendLastValueToNewObservers = sendLastValueToNewObservers;
    }

    // Wird ausgelöst, wenn ein Observer in Update oder StationStopped eine Exception wirft.
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

    public bool IsStopped
    {
        get
        {
            lock (_lock)
            {
                return _isStopped;
            }
        }
    }

    public IReadOnlyList<string> GetObserverNames()
    {
        IWeatherObserver<T>[] snapshot;
        lock (_lock)
        {
            snapshot = _observers;
        }

        var names = new List<string>();
        foreach (IWeatherObserver<T> observer in snapshot)
        {
            names.Add(observer.Name);
        }
        return names;
    }

    public void Subscribe(IWeatherObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        // Während der Anmeldung (inkl. letztem Wert) darf keine andere Meldung verteilt werden,
        // sonst könnte der neue Observer erst den neuen und DANACH den alten Wert bekommen.
        lock (_deliveryLock)
        {
            bool alreadyStopped;
            bool sendLastValue = false;
            T? lastValue = default;

            lock (_lock)
            {
                alreadyStopped = _isStopped;
                if (!alreadyStopped)
                {
                    if (Array.IndexOf(_observers, observer) >= 0)
                    {
                        return; // doppelt anmelden bringt nichts
                    }

                    // Copy-on-Write: neues Array mit dem zusätzlichen Observer
                    var newArray = new IWeatherObserver<T>[_observers.Length + 1];
                    Array.Copy(_observers, newArray, _observers.Length);
                    newArray[^1] = observer;
                    _observers = newArray;

                    if (_sendLastValueToNewObservers && _hasLastValue)
                    {
                        sendLastValue = true;
                        lastValue = _lastValue;
                    }
                }
            }

            if (alreadyStopped)
            {
                // Zu spät angemeldet: sofort "beendet" melden, es gibt nichts zu beobachten.
                observer.StationStopped();
                return;
            }

            if (sendLastValue)
            {
                DeliverUpdate(observer, lastValue!);
            }
        }
    }

    public void Unsubscribe(IWeatherObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (_lock)
        {
            int index = Array.IndexOf(_observers, observer);
            if (index < 0)
            {
                return; // war gar nicht angemeldet
            }

            // Copy-on-Write: neues Array ohne den Observer
            var newArray = new IWeatherObserver<T>[_observers.Length - 1];
            Array.Copy(_observers, 0, newArray, 0, index);
            Array.Copy(_observers, index + 1, newArray, index, _observers.Length - index - 1);
            _observers = newArray;
        }
    }

    protected void NotifyObservers(T value)
    {
        lock (_deliveryLock)
        {
            IWeatherObserver<T>[] snapshot;
            lock (_lock)
            {
                if (_isStopped)
                {
                    return;
                }
                _lastValue = value;
                _hasLastValue = true;
                snapshot = _observers;
            }

            // Außerhalb von _lock: Observer dürfen sich hier selbst ab- oder anmelden.
            foreach (IWeatherObserver<T> observer in snapshot)
            {
                // Hat ein Observer die Station gerade beendet (z. B. Stop() in Update),
                // bekommen die restlichen Observer kein Update mehr nach StationStopped.
                if (IsStopped)
                {
                    return;
                }
                DeliverUpdate(observer, value);
            }
        }
    }

    protected void NotifyStopped()
    {
        lock (_deliveryLock)
        {
            IWeatherObserver<T>[] snapshot;
            lock (_lock)
            {
                if (_isStopped)
                {
                    return;
                }
                _isStopped = true;
                snapshot = _observers;
                _observers = [];
            }

            foreach (IWeatherObserver<T> observer in snapshot)
            {
                try
                {
                    observer.StationStopped();
                }
                catch (Exception exception)
                {
                    ObserverFailed?.Invoke(new ObserverError(observer.Name, exception));
                }
            }
        }
    }

    // Ein fehlerhafter Observer darf die anderen nicht stören (Fehlerisolation).
    private void DeliverUpdate(IWeatherObserver<T> observer, T value)
    {
        try
        {
            observer.Update(value);
        }
        catch (Exception exception)
        {
            ObserverFailed?.Invoke(new ObserverError(observer.Name, exception));
        }
    }
}
