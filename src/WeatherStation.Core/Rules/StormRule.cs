using System.Globalization;
using WeatherStation.Core.Models;

namespace WeatherStation.Core.Rules;

// Sturmwarnung mit Hysterese: Warnung bei Wind >= limit, Entwarnung erst bei
// Wind <= clearLimit (siehe FrostRule für die Begründung).
public sealed class StormRule : IWarningRule
{
    private readonly double _limit;
    private readonly double _clearLimit;
    private bool _isActive;

    public StormRule(double limit, double clearLimit)
    {
        if (clearLimit >= limit)
        {
            throw new ArgumentException("Die Entwarnungsgrenze muss kleiner als die Warngrenze sein.", nameof(clearLimit));
        }
        _limit = limit;
        _clearLimit = clearLimit;
    }

    public string Name => "Sturm";

    public IReadOnlyList<WeatherWarning> Check(WeatherReading reading)
    {
        if (!_isActive && reading.WindSpeed >= _limit)
        {
            _isActive = true;
            return [WeatherWarning.Storm(reading)];
        }

        if (_isActive && reading.WindSpeed <= _clearLimit)
        {
            _isActive = false;
            string windSpeed = reading.WindSpeed.ToString("F1", CultureInfo.GetCultureInfo("de-DE"));
            return [WeatherWarning.AllClear(reading, $"Der Sturm ist vorbei: Der Wind weht nur noch mit {windSpeed} km/h.")];
        }

        return [];
    }
}
