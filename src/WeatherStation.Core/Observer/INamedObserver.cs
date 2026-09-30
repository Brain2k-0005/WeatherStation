namespace WeatherStation.Core.Observer;

// Optionales Zusatz-Interface: Ein Observer kann sich einen lesbaren Namen geben.
// Die UI zeigt damit an, wer gerade angemeldet ist.
public interface INamedObserver
{
    string Name { get; }
}
