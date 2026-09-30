using WeatherStation.Core.Scenarios.Sensors;

namespace WeatherStation.Core.Scenarios;

// PATTERN: Factory Method – Rolle: konkreter Erzeuger (ConcreteCreator).
// Diese Klasse erzeugt einen FrostNightSensor.
public sealed class FrostNightScenario : WeatherScenario
{
    public override string Key => "frost";

    public override string Name => "Frostnacht";

    public override string Description =>
        "Die Temperatur sinkt über Nacht von etwa 6 auf -4 °C: Temperatursturz und Frostwarnung, morgens Entwarnung und Temperaturanstieg.";

    protected override ISensor CreateSensor(DateTime startTime)
    {
        return new FrostNightSensor(startTime);
    }
}
