using System.Globalization;
using WeatherStation.Core.Models;

namespace WeatherStation.Core.Rules;

// Hitzewarnung mit Hysterese: Warnung bei Temperatur >= limit, Entwarnung erst bei
// Temperatur <= clearLimit (siehe FrostRule für die Begründung).
public sealed class HeatRule : IWarningRule
{
    private readonly double _limit;
    private readonly double _clearLimit;
    private bool _isActive;

    public HeatRule(double limit, double clearLimit)
    {
        if (clearLimit >= limit)
        {
            throw new ArgumentException("Die Entwarnungsgrenze muss kleiner als die Warngrenze sein.", nameof(clearLimit));
        }
        _limit = limit;
        _clearLimit = clearLimit;
    }

    public string Name => "Hitze";

    public IReadOnlyList<WeatherWarning> Check(WeatherReading reading)
    {
        if (!_isActive && reading.Temperature >= _limit)
        {
            _isActive = true;
            return [WeatherWarning.Heat(reading)];
        }

        if (_isActive && reading.Temperature <= _clearLimit)
        {
            _isActive = false;
            string temperature = reading.Temperature.ToString("F1", CultureInfo.GetCultureInfo("de-DE"));
            return [WeatherWarning.AllClear(reading, $"Die Hitze lässt nach: Es hat nur noch {temperature} °C.")];
        }

        return [];
    }
}
