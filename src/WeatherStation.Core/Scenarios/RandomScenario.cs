using WeatherStation.Core.Scenarios.Sensors;

namespace WeatherStation.Core.Scenarios;

// PATTERN: Factory Method – Rolle: konkreter Erzeuger (ConcreteCreator).
// Diese Klasse erzeugt einen RandomSensor.
// Mit einem festen Seed liefert der Sensor immer dieselbe Folge (gut für Tests).
public sealed class RandomScenario : WeatherScenario
{
    private readonly int? _seed;

    public RandomScenario(int? seed = null)
    {
        _seed = seed;
    }

    public override string Key => "random";

    public override string Name => "Zufälliges Wetter";

    public override string Description =>
        "Die Werte schwanken zufällig in plausiblen Grenzen: Welche Warnungen kommen, ist offen.";

    protected override ISensor CreateSensor(DateTime startTime)
    {
        return new RandomSensor(startTime, _seed);
    }
}
