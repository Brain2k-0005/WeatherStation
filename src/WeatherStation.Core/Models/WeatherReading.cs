namespace WeatherStation.Core.Models;

// Ein einzelner Messwert der Wetterstation.
// Einheiten: Temperatur °C, Luftfeuchte %, Luftdruck hPa, Wind km/h.
// Time ist die (simulierte) Uhrzeit der Messung – Regeln benutzen nur diese, nie DateTime.Now.
public sealed record WeatherReading(DateTime Time, double Temperature, double Humidity,
                                    double Pressure, double WindSpeed);
