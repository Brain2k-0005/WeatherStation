namespace WeatherStation.Core.Scenarios;

// Verzeichnis aller Szenarien. Die Oberflächen bauen ihr Menü daraus,
// ohne die konkreten Szenario-Klassen zu kennen.
public static class ScenarioCatalog
{
    private static readonly IReadOnlyList<WeatherScenario> Scenarios =
    [
        new StormScenario(),
        new FrostNightScenario(),
        new HeatWaveScenario(),
        new RandomScenario()
    ];

    public static IReadOnlyList<WeatherScenario> All => Scenarios;

    public static WeatherScenario Find(string key)
    {
        foreach (WeatherScenario scenario in Scenarios)
        {
            if (string.Equals(scenario.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return scenario;
            }
        }
        throw new ArgumentException($"Unbekanntes Szenario: '{key}'.", nameof(key));
    }
}
