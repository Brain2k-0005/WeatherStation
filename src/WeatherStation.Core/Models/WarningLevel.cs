namespace WeatherStation.Core.Models;

// Wie ernst ist eine Meldung? Die Zahlen sind sortierbar (Danger ist am wichtigsten).
public enum WarningLevel
{
    Info = 0,
    Warning = 1,
    Danger = 2
}
