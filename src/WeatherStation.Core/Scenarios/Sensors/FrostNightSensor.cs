using WeatherStation.Core.Models;

namespace WeatherStation.Core.Scenarios.Sensors;

// PATTERN: Factory Method – Rolle: konkretes Produkt (ConcreteProduct), erzeugt von (FrostNightScenario): Klare Nacht mit Frost, morgens wird es milder.
internal sealed class FrostNightSensor : CycleSensor
{
    // 6 °C am Abend, -4 °C in der Nacht (Frost bei <= 0), morgens 8 °C (Entwarnung bei >= 1).
    private static readonly (int Step, double Value)[] TemperatureCurve =
        [(0, 6), (30, -4), (40, -4), (56, 8), (72, 6)];

    private static readonly (int Step, double Value)[] HumidityCurve =
        [(0, 70), (30, 92), (56, 65), (72, 70)];

    private static readonly (int Step, double Value)[] PressureCurve =
        [(0, 1022), (36, 1024), (72, 1022)];

    public FrostNightSensor(DateTime startTime) : base(startTime, noiseSeed: 2)
    {
    }

    protected override WeatherReading CreateReading(DateTime time, int stepInCycle)
    {
        return new WeatherReading(
            time,
            Math.Round(Curve(stepInCycle, TemperatureCurve) + Noise(0.1), 1),
            Math.Round(Math.Clamp(Curve(stepInCycle, HumidityCurve) + Noise(1), 0, 100), 1),
            Math.Round(Curve(stepInCycle, PressureCurve) + Noise(0.1), 1),
            Math.Round(Math.Max(0, 6 + Noise(2)), 1));
    }
}
