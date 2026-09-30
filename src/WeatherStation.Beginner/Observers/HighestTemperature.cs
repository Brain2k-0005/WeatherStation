namespace WeatherStation.Beginner.Observers;

// PATTERN: Observer – Rolle: Observer
// Merkt sich still im Hintergrund die höchste Temperatur, ohne etwas auszugeben.
public class HighestTemperature : IWeatherObserver
{
    public double? Highest { get; private set; }

    public void Update(WeatherReading reading)
    {
        if (Highest is null || reading.Temperature > Highest)
        {
            Highest = reading.Temperature;
        }
    }
}
