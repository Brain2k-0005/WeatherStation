namespace WeatherStation.Core.Observer;

// Beschreibt einen Fehler, den ein Observer in OnNext ausgelöst hat.
public sealed record ObserverError(string ObserverName, Exception Error);
