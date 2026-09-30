using WeatherStation.Core.Models;

namespace WeatherStation.Core.Scenarios.Sensors;

// PATTERN: Factory Method – Rolle: konkretes Produkt (ConcreteProduct), erzeugt von (RandomScenario): "Random Walk" – jeder Wert
// verändert sich nur ein kleines Stück gegenüber dem vorherigen und bleibt
// in plausiblen Grenzen. Mit festem Seed ist die Folge reproduzierbar.
internal sealed class RandomSensor : ISensor
{
    private static readonly TimeSpan StepDuration = TimeSpan.FromMinutes(10);

    private readonly Random _random;
    private readonly DateTime _startTime;
    private int _stepCount;

    private double _temperature = 15;
    private double _humidity = 60;
    private double _pressure = 1013;
    private double _windSpeed = 12;

    public RandomSensor(DateTime startTime, int? seed)
    {
        _startTime = startTime;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public WeatherReading ReadNext()
    {
        DateTime time = _startTime + _stepCount * StepDuration;

        // Der erste Messwert ist der Startwert, danach wird jedes Mal ein kleiner Schritt gemacht.
        if (_stepCount > 0)
        {
            _temperature = Math.Clamp(_temperature + Step(1.0), -20, 40);
            _humidity = Math.Clamp(_humidity + Step(3.0), 20, 100);
            _pressure = Math.Clamp(_pressure + Step(0.8), 960, 1050);
            _windSpeed = Math.Clamp(_windSpeed + Step(5.0), 0, 120);
        }
        _stepCount++;

        return new WeatherReading(
            time,
            Math.Round(_temperature, 1),
            Math.Round(_humidity, 1),
            Math.Round(_pressure, 1),
            Math.Round(_windSpeed, 1));
    }

    // Zufälliger Schritt zwischen -maxStep und +maxStep
    private double Step(double maxStep)
    {
        return (_random.NextDouble() * 2 - 1) * maxStep;
    }
}
