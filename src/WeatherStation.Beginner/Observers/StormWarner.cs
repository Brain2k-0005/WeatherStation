namespace WeatherStation.Beginner.Observers;

// PATTERN: Observer – Rolle: Observer
// Reagiert nur bei starkem Wind (ab 75 km/h) und gibt eine Sturmwarnung aus.
public class StormWarner : IWeatherObserver
{
    public int WarningCount { get; private set; }

    public void Update(WeatherReading reading)
    {
        if (reading.WindSpeed >= 75)
        {
            WarningCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Sturmwarner] Sturm! {reading.WindSpeed:0} km/h um {reading.Time} Uhr");
            Console.ResetColor();
        }
    }
}
