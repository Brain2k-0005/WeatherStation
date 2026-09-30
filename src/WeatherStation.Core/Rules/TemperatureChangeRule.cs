using WeatherStation.Core.Models;

namespace WeatherStation.Core.Rules;

// Meldet, wenn sich die Temperatur seit der LETZTEN MELDUNG um mindestens "limit" geändert hat.
// Bezugswert ist der zuletzt gemeldete Wert (nicht der vorherige Messwert),
// sonst würde eine langsame, stetige Änderung nie auffallen – und kleine
// Schwankungen erzeugen keinen Spam.
public sealed class TemperatureChangeRule : IWarningRule
{
    private readonly double _limit;
    private double? _referenceTemperature;

    public TemperatureChangeRule(double limit)
    {
        if (limit <= 0)
        {
            throw new ArgumentException("Der Grenzwert muss größer als 0 sein.", nameof(limit));
        }
        _limit = limit;
    }

    public string Name => "Temperaturänderung";

    public IReadOnlyList<WeatherWarning> Check(WeatherReading reading)
    {
        // Der erste Messwert ist nur der Bezugswert.
        if (_referenceTemperature == null)
        {
            _referenceTemperature = reading.Temperature;
            return [];
        }

        double change = reading.Temperature - _referenceTemperature.Value;
        if (Math.Abs(change) < _limit)
        {
            return [];
        }

        _referenceTemperature = reading.Temperature;

        if (change > 0)
        {
            return [WeatherWarning.TemperatureRise(reading, change)];
        }
        return [WeatherWarning.TemperatureFall(reading, change)];
    }
}
