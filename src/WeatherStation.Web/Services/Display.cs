using System.Globalization;
using WeatherStation.Core.Models;

namespace WeatherStation.Web.Services;

// Kleine Hilfsmethoden, damit alle Seiten Zahlen und Zeiten gleich anzeigen.
// Deutsche Schreibweise: Komma als Dezimaltrenner (12,5 statt 12.5).
public static class Display
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    // Leerwert laut Design Guide: Geviertstrich.
    public const string Empty = "—";

    public static string Temperature(double? value) =>
        value is null ? Empty : value.Value.ToString("0.0", German) + " °C";

    public static string Humidity(double? value) =>
        value is null ? Empty : value.Value.ToString("0", German) + " %";

    public static string Pressure(double? value) =>
        value is null ? Empty : value.Value.ToString("0.0", German) + " hPa";

    public static string Wind(double? value) =>
        value is null ? Empty : value.Value.ToString("0", German) + " km/h";

    // Einfache Dezimalzahl, z. B. für die Grenzwert-Tabelle.
    public static string Number(double value) => value.ToString("0.0", German);

    // Die Zeit ist die SIMULIERTE Uhrzeit des Messwerts, nicht die echte.
    public static string Time(DateTime time) => time.ToString("HH:mm", German);

    public static string DateAndTime(DateTime time) => time.ToString("dd.MM.yyyy HH:mm", German);

    // Deutsche Bezeichnung der Warnungs-Art (für die Tabelle).
    public static string TypeName(WarningType type)
    {
        switch (type)
        {
            case WarningType.TemperatureChange:
                return "Temperaturänderung";
            case WarningType.Frost:
                return "Frost";
            case WarningType.Heat:
                return "Hitze";
            case WarningType.Storm:
                return "Sturm";
            case WarningType.PressureDrop:
                return "Luftdruckfall";
            case WarningType.AllClear:
                return "Entwarnung";
            default:
                return type.ToString();
        }
    }
}
