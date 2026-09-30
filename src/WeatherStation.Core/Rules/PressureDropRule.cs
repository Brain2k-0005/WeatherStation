using WeatherStation.Core.Models;

namespace WeatherStation.Core.Rules;

// Meldet, wenn der Luftdruck innerhalb des Zeitfensters (z. B. 3 Stunden) um mindestens
// "limit" hPa gefallen ist. Dafür merkt sich die Regel alle Messwerte im Fenster.
// Wichtig: Es zählt die Uhrzeit der Messwerte (reading.Time), nicht die echte Uhr.
public sealed class PressureDropRule : IWarningRule
{
    private readonly double _limit;
    private readonly TimeSpan _window;
    private readonly Queue<WeatherReading> _readingsInWindow = new();
    private bool _isActive;

    public PressureDropRule(double limit, TimeSpan window)
    {
        if (limit <= 0)
        {
            throw new ArgumentException("Der Grenzwert muss größer als 0 sein.", nameof(limit));
        }
        if (window <= TimeSpan.Zero)
        {
            throw new ArgumentException("Das Zeitfenster muss größer als 0 sein.", nameof(window));
        }
        _limit = limit;
        _window = window;
    }

    public string Name => "Luftdruckabfall";

    public IReadOnlyList<WeatherWarning> Check(WeatherReading reading)
    {
        _readingsInWindow.Enqueue(reading);

        // Zu alte Messwerte aus dem Fenster entfernen.
        DateTime oldestAllowed = reading.Time - _window;
        while (_readingsInWindow.Count > 0 && _readingsInWindow.Peek().Time < oldestAllowed)
        {
            _readingsInWindow.Dequeue();
        }

        // Höchsten Druck im Fenster suchen (bei Gleichstand der früheste).
        WeatherReading highest = _readingsInWindow.Peek();
        foreach (WeatherReading candidate in _readingsInWindow)
        {
            if (candidate.Pressure > highest.Pressure)
            {
                highest = candidate;
            }
        }

        double drop = highest.Pressure - reading.Pressure;

        if (!_isActive && drop >= _limit)
        {
            _isActive = true;
            double hours = Math.Round((reading.Time - highest.Time).TotalHours, 1);
            return [WeatherWarning.PressureDrop(reading, drop, hours)];
        }

        // Erst zurücksetzen, wenn der Abfall deutlich kleiner geworden ist (halber Grenzwert).
        // Das verhindert ständig neue Meldungen bei einem Druck, der um den Grenzwert pendelt.
        if (_isActive && drop < _limit / 2)
        {
            _isActive = false;
        }

        return [];
    }
}
