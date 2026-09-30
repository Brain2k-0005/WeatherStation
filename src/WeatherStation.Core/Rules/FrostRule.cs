using System.Globalization;
using WeatherStation.Core.Models;

namespace WeatherStation.Core.Rules;

// Frostwarnung mit Hysterese: Warnung bei Temperatur <= limit, Entwarnung erst bei
// Temperatur >= clearLimit. Dazwischen passiert nichts. Ohne diese "Lücke" würde die
// Regel bei 0,0 / 0,1 / 0,0 / 0,1 °C ständig zwischen Warnung und Entwarnung springen.
public sealed class FrostRule : IWarningRule
{
    private readonly double _limit;
    private readonly double _clearLimit;
    private bool _isActive;

    public FrostRule(double limit, double clearLimit)
    {
        if (clearLimit <= limit)
        {
            throw new ArgumentException("Die Entwarnungsgrenze muss größer als die Warngrenze sein.", nameof(clearLimit));
        }
        _limit = limit;
        _clearLimit = clearLimit;
    }

    public string Name => "Frost";

    public IReadOnlyList<WeatherWarning> Check(WeatherReading reading)
    {
        if (!_isActive && reading.Temperature <= _limit)
        {
            _isActive = true;
            return [WeatherWarning.Frost(reading)];
        }

        if (_isActive && reading.Temperature >= _clearLimit)
        {
            _isActive = false;
            string temperature = reading.Temperature.ToString("F1", CultureInfo.GetCultureInfo("de-DE"));
            return [WeatherWarning.AllClear(reading, $"Kein Frost mehr: Es hat wieder {temperature} °C.")];
        }

        return [];
    }
}
