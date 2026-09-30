using WeatherStation.Core.Models;

namespace WeatherStation.Core.Rules;

// Eine Regel schaut sich jeden neuen Messwert an und entscheidet, ob etwas gemeldet wird.
// Regeln kennen weder Station noch Observer – sie liefern nur Warnungen zurück.
public interface IWarningRule
{
    // Deutscher Anzeigename, z. B. "Frost"
    string Name { get; }

    // Leere Liste = nichts zu melden
    IReadOnlyList<WeatherWarning> Check(WeatherReading reading);
}
