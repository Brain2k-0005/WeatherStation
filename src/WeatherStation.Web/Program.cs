using Lumeo;
using WeatherStation.Core.Building;
using WeatherStation.Core.Settings;
using WeatherStation.Web.Components;
using WeatherStation.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Lumeo registriert ToastService, ThemeService, OverlayService usw.
builder.Services.AddLumeo();

// ============================================================
// PATTERN: Builder - hier wird das WeatherSystem zusammengebaut
// Statt einen langen Konstruktor mit allen Teilen aufzurufen, "bestellen" wir
// Schritt für Schritt, was wir brauchen. Build() verkabelt am Ende alles:
// Station -> WarningService -> WarningHistory (und Station -> Verlauf / Statistik).
//
// Mit AddSingleton registriert: Es gibt genau EIN System für die ganze Anwendung.
// Alle Browser-Tabs (= Blazor-Circuits) sehen dieselbe Station und dieselben Daten.
// Achtung, Begriffsfalle: "Singleton" heißt hier nur "Lebensdauer im DI-Container".
// Das ist NICHT das Singleton-Muster (siehe WarningSettings) – WeatherSystem hat einen
// öffentlichen Konstruktor, man könnte also weitere Instanzen bauen.
// ============================================================
builder.Services.AddSingleton(_ => new WeatherStationBuilder()
    .SetName("Wetterstation Berufsschule")
    .AddAllWarnings()          // Frost, Hitze, Sturm, Luftdruck, Temperatursprung
    .KeepReadings(144)         // 144 Messwerte x 10 Minuten = 24 Stunden im Diagramm
    .Build());

// Der SimulationRunner liefert alle 1,5 Sekunden einen neuen Messwert.
// Er wird als Singleton UND als Hintergrunddienst registriert. Der zweite Aufruf
// holt dieselbe Instanz, damit die Seiten ("Pause", "Szenario wechseln") und der
// Hintergrunddienst mit demselben Objekt arbeiten.
builder.Services.AddSingleton<SimulationRunner>();
builder.Services.AddHostedService(services => services.GetRequiredService<SimulationRunner>());

var app = builder.Build();

// PATTERN: Singleton - hier wird die Einstellungs-Instanz einmal angefasst,
// damit ihr Erstellzeitpunkt (CreatedAt) beim Start der App feststeht.
_ = WarningSettings.Instance;

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
