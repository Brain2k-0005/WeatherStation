namespace WeatherStation.Beginner.Observers;

// PATTERN: Observer – Rolle: Observer
// Reagiert nur, wenn es friert (0 °C oder kälter), und warnt dann vor Glätte.
public class FrostWarner : IWeatherObserver
{
    public int WarningCount { get; private set; }

    public void Update(WeatherReading reading)
    {
        if (reading.Temperature <= 0)
        {
            WarningCount++;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Frostwarner] Achtung Glätte! {reading.Temperature:0.0} °C um {reading.Time} Uhr");
            Console.ResetColor();
        }
    }
}
