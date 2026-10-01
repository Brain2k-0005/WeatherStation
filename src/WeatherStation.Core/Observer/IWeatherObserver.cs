namespace WeatherStation.Core.Observer;

// ============================================================
// PATTERN: Observer – Rolle: Observer (die Schnittstelle)
// Der gleiche Vertrag wie in Stufe 1 (src/WeatherStation.Beginner/IWeatherObserver.cs):
// Wer Update hat, kann sich anmelden. Hier ist die Schnittstelle generisch (T = Messwert
// oder Warnung) und hat zusätzlich einen Namen und die Methode StationStopped.
//
// So heißt das in .NET (nur als Ausblick): IObservable<T>/IObserver<T> mit
// OnNext (= Update), OnCompleted (= StationStopped) und OnError.
// Abmelden geht dort über ein IDisposable-Token statt über Unsubscribe.
// ============================================================
public interface IWeatherObserver<T>
{
    // Anzeigename, z. B. für das Observer-Labor.
    string Name { get; }

    // Ein neuer Wert ist da (Stufe 1: Update(reading)).
    void Update(T value);

    // Die Quelle hat aufgehört, es kommen keine Werte mehr.
    void StationStopped();
}
