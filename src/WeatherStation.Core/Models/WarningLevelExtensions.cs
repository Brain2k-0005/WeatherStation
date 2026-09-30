namespace WeatherStation.Core.Models;

public static class WarningLevelExtensions
{
    // Deutsche Anzeige-Texte für die Oberfläche.
    public static string ToGerman(this WarningLevel level)
    {
        switch (level)
        {
            case WarningLevel.Info:
                return "Hinweis";
            case WarningLevel.Warning:
                return "Warnung";
            case WarningLevel.Danger:
                return "Gefahr";
            default:
                return level.ToString();
        }
    }
}
