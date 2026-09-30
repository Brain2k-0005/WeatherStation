using WeatherStation.Core.Models;

namespace WeatherStation.Core.Scenarios.Sensors;

// PATTERN: Factory Method – Rolle: konkretes Produkt (ConcreteProduct), erzeugt von (StormScenario): Eine Sturmfront zieht auf und vorüber.
// Stützpunkte (Schritt, Wert) – ein Schritt sind 10 Minuten.
internal sealed class StormSensor : CycleSensor
{
    private static readonly (int Step, double Value)[] TemperatureCurve =
        [(0, 18), (36, 11), (48, 12), (60, 16), (72, 18)];

    // Druck fällt von 1015 auf 998 hPa (mehr als 3 hPa in 3 Stunden), steigt dann wieder.
    private static readonly (int Step, double Value)[] PressureCurve =
        [(0, 1015), (30, 998), (60, 1010), (72, 1015)];

    // Wind steigt auf 90 km/h (über 75 = Sturm) und beruhigt sich wieder (unter 60 = Entwarnung).
    private static readonly (int Step, double Value)[] WindCurve =
        [(0, 15), (20, 20), (36, 90), (40, 90), (48, 30), (60, 15), (72, 15)];

    private static readonly (int Step, double Value)[] HumidityCurve =
        [(0, 55), (36, 90), (60, 70), (72, 55)];

    public StormSensor(DateTime startTime) : base(startTime, noiseSeed: 1)
    {
    }

    protected override WeatherReading CreateReading(DateTime time, int stepInCycle)
    {
        return new WeatherReading(
            time,
            Math.Round(Curve(stepInCycle, TemperatureCurve) + Noise(0.1), 1),
            Math.Round(Math.Clamp(Curve(stepInCycle, HumidityCurve) + Noise(1), 0, 100), 1),
            Math.Round(Curve(stepInCycle, PressureCurve) + Noise(0.1), 1),
            Math.Round(Math.Max(0, Curve(stepInCycle, WindCurve) + Noise(1)), 1));
    }
}
