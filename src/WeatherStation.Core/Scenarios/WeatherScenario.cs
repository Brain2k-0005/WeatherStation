namespace WeatherStation.Core.Scenarios;

// ============================================================
// PATTERN: Factory Method (GoF) – Rolle: Erzeuger (Creator)
// Die Basisklasse legt fest, WIE ein Szenario gestartet wird (Start), überlässt
// es aber der UNTERKLASSE, WELCHER Sensor dafür erzeugt wird: CreateSensor ist
// die "Factory Method". Wer ein Szenario startet, kennt nur ISensor und muss
// nicht wissen, welche konkrete Sensor-Klasse dahintersteckt.
// Rollen: Creator = WeatherScenario, ConcreteCreator = StormScenario usw.,
//         Product = ISensor, ConcreteProduct = StormSensor usw.
// ============================================================
public abstract class WeatherScenario
{
    // Kurzer, eindeutiger Schlüssel: "storm", "heat", "frost", "random"
    public abstract string Key { get; }

    // Anzeigename, z. B. "Sturmfront zieht auf"
    public abstract string Name { get; }

    // Ein Satz: Was passiert, welche Warnungen sind zu erwarten?
    public abstract string Description { get; }

    // <-- Die Factory Method: Die Unterklasse entscheidet, welcher Sensor entsteht.
    protected abstract ISensor CreateSensor(DateTime startTime);

    public ISensor Start(DateTime startTime)
    {
        ISensor? sensor = CreateSensor(startTime);
        if (sensor == null)
        {
            throw new InvalidOperationException($"Das Szenario '{Key}' hat keinen Sensor erzeugt.");
        }
        return sensor;
    }
}
