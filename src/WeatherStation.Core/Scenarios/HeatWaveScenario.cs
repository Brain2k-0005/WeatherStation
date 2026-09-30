using WeatherStation.Core.Scenarios.Sensors;

namespace WeatherStation.Core.Scenarios;

// PATTERN: Factory Method – Rolle: konkreter Erzeuger (ConcreteCreator).
// Diese Klasse erzeugt einen HeatWaveSensor.
public sealed class HeatWaveScenario : WeatherScenario
{
    public override string Key => "heat";

    public override string Name => "Hitzewelle";

    public override string Description =>
        "Die Temperatur steigt am Nachmittag von etwa 24 auf 34 °C: Temperaturanstieg und Hitzewarnung, abends Entwarnung.";

    protected override ISensor CreateSensor(DateTime startTime)
    {
        return new HeatWaveSensor(startTime);
    }
}
