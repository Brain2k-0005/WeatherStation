using WeatherStation.ConsoleApp;

// Program.cs bleibt bewusst kurz: die eigentliche Logik steckt in WeatherConsole.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var app = new WeatherConsole();
app.Run();
