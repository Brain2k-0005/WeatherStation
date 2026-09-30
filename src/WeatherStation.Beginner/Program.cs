using System.Globalization;
using System.Text;
using WeatherStation.Beginner;
using WeatherStation.Beginner.Observers;

// Stufe 1 – Einstieg: Das Observer-Muster in einer Geschichte.
// Die Station (Subject) meldet neue Messwerte, viele Observer hören zu.

Console.OutputEncoding = Encoding.UTF8;
// Zahlen immer deutsch formatieren (12,5 statt 12.5) – egal, wie der Rechner eingestellt ist.
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

// 1) Das Subject: die Wetterstation.
var station = new Station();

// 2) Die Observer: vier Zuhörer mit ganz verschiedenen Aufgaben.
var display = new ScreenDisplay();
var frostWarner = new FrostWarner();
var stormWarner = new StormWarner();
var highest = new HighestTemperature();

// 3) Anmelden: Ab jetzt bekommen alle vier jede neue Messung.
station.Subscribe(display);
station.Subscribe(frostWarner);
station.Subscribe(stormWarner);
station.Subscribe(highest);
Console.WriteLine($"Angemeldete Observer: {station.ObserverCount}");
Console.WriteLine();

// 4) Ein Tag in Kurzform: Abend, Frost in der Nacht, dann ein Sturm.
var readings = new[]
{
    new WeatherReading("18:00", 12.5, 20),
    new WeatherReading("20:00", 8.0, 25),
    new WeatherReading("22:00", 3.5, 30),
    new WeatherReading("00:00", -1.0, 35),
    new WeatherReading("02:00", -3.5, 40),
    new WeatherReading("04:00", 0.0, 60),
    new WeatherReading("06:00", 2.0, 80),
    new WeatherReading("08:00", 5.0, 95),
};

for (int i = 0; i < readings.Length; i++)
{
    // Die Station kennt die Observer nicht im Detail – sie ruft nur SetReading auf.
    station.SetReading(readings[i]);

    if (i == 3)
    {
        // 5) Abmelden: Die Anzeige hört ab jetzt nicht mehr zu, die anderen schon.
        Console.WriteLine();
        Console.WriteLine("--- Die Anzeige meldet sich ab (Unsubscribe) ---");
        Console.WriteLine();
        station.Unsubscribe(display);
    }

    if (!Console.IsOutputRedirected)
    {
        Thread.Sleep(600); // nur damit man beim Zuschauen mitlesen kann
    }
}

// 6) Ergebnis: Die stillen Observer haben mitgezählt.
Console.WriteLine();
Console.WriteLine($"Höchste Temperatur: {highest.Highest:0.0} °C");
Console.WriteLine($"Frostwarnungen: {frostWarner.WarningCount}, Sturmwarnungen: {stormWarner.WarningCount}");
