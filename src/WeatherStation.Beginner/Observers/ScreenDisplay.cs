namespace WeatherStation.Beginner.Observers;

// PATTERN: Observer – Rolle: Observer
// Zeigt jede neue Messung einfach auf dem Bildschirm an.
public class ScreenDisplay : IWeatherObserver
{
    public void Update(WeatherReading reading)
    {
        Console.WriteLine($"[Anzeige] {reading.Time} Uhr: {reading.Temperature:0.0} °C, Wind {reading.WindSpeed:0} km/h");
    }
}
