using WeatherStation.Core.Scenarios.Sensors;

namespace WeatherStation.Core.Scenarios;

// PATTERN: Factory Method – Rolle: konkreter Erzeuger (ConcreteCreator).
// Diese Klasse erzeugt einen StormSensor.
public sealed class StormScenario : WeatherScenario
{
    public override string Key => "storm";

    public override string Name => "Sturmfront zieht auf";

    public override string Description =>
        "Der Luftdruck fällt, der Wind steigt auf etwa 90 km/h: Warnung vor Luftdruckabfall, Temperatursturz und Sturm, danach Entwarnung.";

    protected override ISensor CreateSensor(DateTime startTime)
    {
        return new StormSensor(startTime);
    }
}
