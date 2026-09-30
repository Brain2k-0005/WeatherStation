using WeatherStation.Core.Models;

namespace WeatherStation.Core.Scenarios.Sensors;

// PATTERN: Factory Method – Rolle: konkretes Produkt (ConcreteProduct), erzeugt von (HeatWaveScenario): Heißer Nachmittag, abends kühlt es ab.
internal sealed class HeatWaveSensor : CycleSensor
{
    // 24 °C am Vormittag, 34 °C am Nachmittag (Hitze ab 30), abends 22 °C (Entwarnung bei <= 28).
    private static readonly (int Step, double Value)[] TemperatureCurve =
        [(0, 24), (30, 34), (40, 34), (52, 22), (72, 24)];

    private static readonly (int Step, double Value)[] HumidityCurve =
        [(0, 50), (30, 35), (52, 55), (72, 50)];

    private static readonly (int Step, double Value)[] PressureCurve =
        [(0, 1018), (36, 1016), (72, 1018)];

    public HeatWaveSensor(DateTime startTime) : base(startTime, noiseSeed: 3)
    {
    }

    protected override WeatherReading CreateReading(DateTime time, int stepInCycle)
    {
        return new WeatherReading(
            time,
            Math.Round(Curve(stepInCycle, TemperatureCurve) + Noise(0.1), 1),
            Math.Round(Math.Clamp(Curve(stepInCycle, HumidityCurve) + Noise(1), 0, 100), 1),
            Math.Round(Curve(stepInCycle, PressureCurve) + Noise(0.1), 1),
            Math.Round(Math.Max(0, 8 + Noise(2)), 1));
    }
}
