using WeatherStation.Core.Models;

namespace WeatherStation.Core.Scenarios;

// PATTERN: Factory Method – Rolle: Produkt (Product).
// Ein (simulierter) Sensor. Er ist das "Produkt", das die Factory Method erzeugt.
public interface ISensor
{
    // Liefert den nächsten Messwert; jeder Aufruf = 10 simulierte Minuten später.
    // Der erste Messwert trägt genau die Startzeit des Szenarios.
    WeatherReading ReadNext();
}
