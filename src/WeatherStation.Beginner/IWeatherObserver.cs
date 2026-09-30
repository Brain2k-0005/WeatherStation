namespace WeatherStation.Beginner;

// PATTERN: Observer – Rolle: Observer (die Schnittstelle)
// Das ist der Vertrag, den jeder Zuhörer erfüllen muss:
// Wer eine Update-Methode hat, kann sich bei der Station anmelden.
public interface IWeatherObserver
{
    void Update(WeatherReading reading);
}
