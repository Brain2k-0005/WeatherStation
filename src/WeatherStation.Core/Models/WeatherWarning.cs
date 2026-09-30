using System.Globalization;

namespace WeatherStation.Core.Models;

// Eine Meldung, die eine Regel erzeugt hat (z. B. "Frostwarnung").
public sealed record WeatherWarning(DateTime Time, WarningType Type, WarningLevel Level,
                                    string Title, string Message)
{
    // Zahlen immer deutsch formatieren (Komma), eine Nachkommastelle.
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    // ============================================================
    // KEIN Design Pattern: einfache statische Erzeugungsmethoden.
    // Nicht verwechseln mit dem GoF-Muster "Factory Method" (siehe WeatherScenario):
    // Dort entscheidet eine UNTERKLASSE per Vererbung, was erzeugt wird – hier nicht.
    // Sie bündeln Titel, Text und Stufe an einer Stelle, damit die Regeln
    // nur noch "Frost(reading)" schreiben müssen.
    // ============================================================
    public static WeatherWarning TemperatureRise(WeatherReading reading, double change)
    {
        return new WeatherWarning(reading.Time, WarningType.TemperatureChange, WarningLevel.Info,
            "Temperaturanstieg",
            $"Die Temperatur ist um {Format(Math.Abs(change))} °C gestiegen (jetzt {Format(reading.Temperature)} °C).");
    }

    public static WeatherWarning TemperatureFall(WeatherReading reading, double change)
    {
        return new WeatherWarning(reading.Time, WarningType.TemperatureChange, WarningLevel.Info,
            "Temperatursturz",
            $"Die Temperatur ist um {Format(Math.Abs(change))} °C gefallen (jetzt {Format(reading.Temperature)} °C).");
    }

    public static WeatherWarning Frost(WeatherReading reading)
    {
        return new WeatherWarning(reading.Time, WarningType.Frost, WarningLevel.Warning,
            "Frostwarnung",
            $"Es hat {Format(reading.Temperature)} °C. Glättegefahr!");
    }

    public static WeatherWarning Heat(WeatherReading reading)
    {
        return new WeatherWarning(reading.Time, WarningType.Heat, WarningLevel.Warning,
            "Hitzewarnung",
            $"Es hat {Format(reading.Temperature)} °C. Viel trinken und Schatten suchen!");
    }

    public static WeatherWarning Storm(WeatherReading reading)
    {
        return new WeatherWarning(reading.Time, WarningType.Storm, WarningLevel.Danger,
            "Sturmwarnung",
            $"Der Wind weht mit {Format(reading.WindSpeed)} km/h. Nicht im Freien aufhalten!");
    }

    public static WeatherWarning PressureDrop(WeatherReading reading, double drop, double hours)
    {
        return new WeatherWarning(reading.Time, WarningType.PressureDrop, WarningLevel.Warning,
            "Luftdruck fällt",
            $"Der Luftdruck ist in {Format(hours)} Stunden um {Format(drop)} hPa gefallen (jetzt {Format(reading.Pressure)} hPa). Das Wetter wird schlechter.");
    }

    public static WeatherWarning AllClear(WeatherReading reading, string reason)
    {
        return new WeatherWarning(reading.Time, WarningType.AllClear, WarningLevel.Info,
            "Entwarnung",
            reason);
    }

    private static string Format(double value)
    {
        return value.ToString("F1", German);
    }
}
