using WeatherStation.Core.Models;

namespace WeatherStation.Core.Scenarios.Sensors;

// Gemeinsame Basis der Sensoren mit festem Ablauf ("Drehbuch"):
// Alle 72 Schritte (= 12 Stunden bei 10 Minuten pro Schritt) beginnt der Ablauf von vorn.
// Die Unterklassen legen nur fest, wie die Werte in einem Schritt aussehen.
internal abstract class CycleSensor : ISensor
{
    protected const int CycleLength = 72;
    private static readonly TimeSpan StepDuration = TimeSpan.FromMinutes(10);

    private readonly DateTime _startTime;
    private readonly Random _noise;
    private int _stepCount;

    protected CycleSensor(DateTime startTime, int noiseSeed)
    {
        _startTime = startTime;
        // Fester Seed: Das Rauschen ist bei jedem Programmlauf gleich (deterministisch).
        _noise = new Random(noiseSeed);
    }

    public WeatherReading ReadNext()
    {
        DateTime time = _startTime + _stepCount * StepDuration;
        int stepInCycle = _stepCount % CycleLength;
        _stepCount++;

        return CreateReading(time, stepInCycle);
    }

    protected abstract WeatherReading CreateReading(DateTime time, int stepInCycle);

    // Kleines Rauschen zwischen -amount und +amount, damit die Kurven lebendig wirken.
    protected double Noise(double amount)
    {
        return (_noise.NextDouble() * 2 - 1) * amount;
    }

    // Liest einen Wert aus einer Kurve ab. Die Kurve besteht aus Stützpunkten
    // (Schritt, Wert); dazwischen wird gerade verbunden (lineare Interpolation).
    protected static double Curve(int step, (int Step, double Value)[] points)
    {
        for (int index = 1; index < points.Length; index++)
        {
            (int Step, double Value) before = points[index - 1];
            (int Step, double Value) after = points[index];

            if (step <= after.Step)
            {
                double share = (double)(step - before.Step) / (after.Step - before.Step);
                return before.Value + (after.Value - before.Value) * share;
            }
        }
        return points[^1].Value;
    }
}
