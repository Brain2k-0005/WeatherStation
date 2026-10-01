using System.Globalization;
using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;

namespace WeatherStation.ConsoleApp.Observers;

// ============================================================
// PATTERN: Observer – Rolle: konkreter Observer (Messwerte)
// Der ReadingPrinter kennt nur IWeatherObserver<WeatherReading>, die Station
// kennt nur dieses Interface. Beide wissen nichts voneinander.
// ============================================================
public sealed class ReadingPrinter : IWeatherObserver<WeatherReading>
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public string Name => "Messwert-Anzeige";

    // Wird von der Station bei jedem neuen Messwert aufgerufen.
    public void Update(WeatherReading reading)
    {
        string time = reading.Time.ToString("HH:mm", German);
        string line = string.Format(German,
            "  {0}  | {1,6:0.0} °C | {2,5:0} % | {3,7:0.0} hPa | {4,5:0} km/h",
            time, reading.Temperature, reading.Humidity, reading.Pressure, reading.WindSpeed);

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(line);
        Console.ResetColor();
    }

    // Die Station wurde gestoppt: keine Messwerte mehr.
    public void StationStopped()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  [Messwert-Anzeige] Station beendet (StationStopped).");
        Console.ResetColor();
    }
}
