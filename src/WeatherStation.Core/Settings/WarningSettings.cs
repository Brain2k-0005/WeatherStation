namespace WeatherStation.Core.Settings;

// ============================================================
// PATTERN: Singleton (klassisch, thread-sicher über Lazy<T>)
// Es gibt genau EINE Instanz mit den Grenzwerten für die ganze Anwendung.
// Lazy<T> sorgt dafür, dass sie beim ersten Zugriff genau einmal erzeugt wird –
// auch wenn mehrere Threads gleichzeitig zugreifen. Der Konstruktor ist private,
// niemand sonst kann also weitere Instanzen bauen.
//
// Nachteile des Singletons (darum sparsam einsetzen!):
// - Es ist globaler Zustand: Jeder kann von überall darauf zugreifen, man sieht
//   einer Klasse nicht an, dass sie davon abhängt (versteckte Abhängigkeit).
// - Tests können die Werte nicht austauschen, weil es nur die eine Instanz gibt.
// - In ASP.NET Core nimmt man statt dieses Musters meist die Dependency Injection
//   mit AddSingleton(...): gleiche "nur eine Instanz"-Wirkung, aber austauschbar.
//
// WICHTIG (Testbarkeit): Die Regeln greifen NICHT selbst auf Instance zu.
// Sie bekommen ihre Grenzwerte per Konstruktor übergeben. Nur der
// WeatherStationBuilder liest das Singleton. So lassen sich Regeln in Tests
// mit eigenen Werten prüfen, ohne globalen Zustand.
// ============================================================
public sealed class WarningSettings
{
    private static readonly Lazy<WarningSettings> LazyInstance = new(() => new WarningSettings());

    private WarningSettings()
    {
        InstanceId = Guid.NewGuid();
        CreatedAt = DateTime.Now;
    }

    public static WarningSettings Instance => LazyInstance.Value;

    // Beweist in der UI: es ist immer dieselbe Instanz.
    public Guid InstanceId { get; }

    public DateTime CreatedAt { get; }

    // Temperaturänderung in °C seit der letzten Meldung
    public double TemperatureChangeLimit { get; } = 3.0;

    // Frostwarnung bei <= 0 °C, Entwarnung erst bei >= 1 °C (Hysterese)
    public double FrostLimit { get; } = 0.0;
    public double FrostClearLimit { get; } = 1.0;

    // Hitzewarnung bei >= 30 °C, Entwarnung bei <= 28 °C
    public double HeatLimit { get; } = 30.0;
    public double HeatClearLimit { get; } = 28.0;

    // Sturmwarnung bei >= 75 km/h, Entwarnung bei <= 60 km/h
    public double StormLimit { get; } = 75.0;
    public double StormClearLimit { get; } = 60.0;

    // Luftdruckabfall: 3 hPa innerhalb von 3 Stunden
    public double PressureDropLimit { get; } = 3.0;
    public TimeSpan PressureDropWindow { get; } = TimeSpan.FromHours(3);
}
