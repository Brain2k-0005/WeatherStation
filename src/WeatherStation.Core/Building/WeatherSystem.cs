using WeatherStation.Core.Observers;
using WeatherStation.Core.Services;

namespace WeatherStation.Core.Building;

// Das fertige, verkabelte Produkt des Builders (siehe WeatherStationBuilder).
// Es enthält nur Verweise – die Verkabelung (Subscribe) hat der Builder schon erledigt.
public sealed class WeatherSystem
{
    public WeatherSystem(Station station, WarningService warnings, WarningHistory warningHistory,
                         ReadingHistory readingHistory, WeatherStatistics statistics)
    {
        Station = station;
        Warnings = warnings;
        WarningHistory = warningHistory;
        ReadingHistory = readingHistory;
        Statistics = statistics;
    }

    public Station Station { get; }
    public WarningService Warnings { get; }
    public WarningHistory WarningHistory { get; }
    public ReadingHistory ReadingHistory { get; }
    public WeatherStatistics Statistics { get; }
}
