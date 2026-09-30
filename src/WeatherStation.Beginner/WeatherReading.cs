namespace WeatherStation.Beginner;

// Eine einzelne Messung. Ein "record" ist eine kurze Schreibweise für ein reines Datenobjekt.
public record WeatherReading(string Time, double Temperature, double WindSpeed);
