namespace WeatherStation.Core.Models;

// Welche Art von Meldung ist es?
public enum WarningType
{
    TemperatureChange,
    Frost,
    Heat,
    Storm,
    PressureDrop,
    AllClear
}
