# Umsetzungsplan – WeatherStation (Lernprojekt Design Patterns)

Zielgruppe: Auszubildende Fachinformatik, 2. Lehrjahr.
Ziel: Observer (Schwerpunkt), Builder, Factory Method, Singleton an einem
realistischen, nicht-trivialen Beispiel zeigen.

## Leitregeln für ALLE Dateien

- **Einfache Namen**: englische Standardwörter (`Station`, `Sensor`, `Warning`,
  `reading`, `limit`), keine Abkürzungen (`temp` → `temperature`, `obs` → `observer`).
- **Kommentare auf Deutsch**, erklärend (WARUM), für Lernende. Jede Pattern-Stelle
  bekommt einen Kopfkommentar der Form:
  ```csharp
  // ============================================================
  // PATTERN: Observer – Rolle: Subject (das beobachtete Objekt)
  // ...kurze Erklärung in 2-5 Zeilen...
  // ============================================================
  ```
- Keine cleveren Tricks (kein LINQ-Golf, keine Ausdrucks-Akrobatik). Lieber eine Zeile mehr.
- `Nullable` an, `ImplicitUsings` an, file-scoped namespaces, .NET 10 / C# 14.
- Einheiten: Temperatur °C, Luftfeuchte %, Luftdruck hPa, Wind km/h.
- Zeit: Messwerte tragen ihre eigene (simulierte) Uhrzeit `DateTime Time`.
  Regeln verwenden NUR `reading.Time`, nie `DateTime.Now` → deterministisch testbar.

## Projekte

| Projekt | Inhalt |
|---|---|
| `src/WeatherStation.Core` | Gesamte Pattern-Logik, keine UI-Abhängigkeit |
| `src/WeatherStation.ConsoleApp` | Konsolen-Oberfläche (nur Observer + Menü) |
| `src/WeatherStation.Web` | Blazor Server + Lumeo 5.12.2 (Huppenkothen-Design) |
| `tests/WeatherStation.Tests` | xUnit-Tests für Core |

Klasse heißt `Station` (nicht `WeatherStation`, sonst Konflikt mit dem Namespace).

---

## Core – verbindliche öffentliche API

### Namespace `WeatherStation.Core.Observer`

```csharp
// PATTERN: Observer – Rolle: Subject. Wiederverwendbare Basisklasse.
public abstract class Subject<T> : IObservable<T>
{
    protected Subject(bool replayLastValue = false);

    public IDisposable Subscribe(IObserver<T> observer);   // null -> ArgumentNullException
    public int ObserverCount { get; }
    public IReadOnlyList<string> GetObserverNames();        // INamedObserver.Name sonst Typname
    public bool IsCompleted { get; }

    // Wird ausgelöst, wenn ein Observer in OnNext eine Exception wirft.
    // Die anderen Observer werden trotzdem benachrichtigt (Fehlerisolation).
    public event Action<ObserverError>? ObserverFailed;

    protected void Notify(T value);          // nach Complete: ignoriert
    protected void NotifyCompleted();        // ruft OnCompleted, entfernt alle Observer, danach keine Meldungen mehr
    protected void NotifyError(Exception error); // ruft OnError, danach wie Completed
}
```
Pflicht-Details:
- Thread-sicher: `lock` + Copy-on-Write-Array. `Notify` iteriert über eine
  Momentaufnahme (Snapshot) OHNE Lock → Observer dürfen sich während einer
  Benachrichtigung selbst abmelden oder neue anmelden, ohne Exception.
- `Subscribe` gibt ein `Subscription`-Objekt (private nested class, `IDisposable`)
  zurück. `Dispose()` ist idempotent (mehrfach aufrufbar).
- Subscribe nach Completed: ruft sofort `observer.OnCompleted()` und gibt ein
  leeres Token zurück.
- `replayLastValue: true`: Neuer Observer erhält sofort den letzten gemeldeten Wert
  (falls vorhanden).
- Derselbe Observer darf mehrfach angemeldet werden (wie Rx) – jedes Token entfernt genau eine Anmeldung.

```csharp
public interface INamedObserver { string Name { get; } }
public sealed record ObserverError(string ObserverName, Exception Error);

// Observer aus Lambdas – spart für jede Kleinigkeit eine eigene Klasse.
public sealed class ActionObserver<T> : IObserver<T>, INamedObserver
{
    public ActionObserver(string name, Action<T> onNext,
                          Action<Exception>? onError = null, Action? onCompleted = null);
    public string Name { get; }
}

public static class ObservableExtensions
{
    // Kurzform: station.Subscribe("Bildschirm", reading => ...)
    public static IDisposable Subscribe<T>(this IObservable<T> source, string name, Action<T> onNext);
    // Filter-"Operator" wie in Rx: warnings.Where(w => w.Level == WarningLevel.Danger)
    public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> filter);
}
```
`Where` gibt eine private Klasse `FilteredObservable<T>` zurück: Beim Subscribe
meldet sie einen inneren Observer an der Quelle an, der nur passende Werte
weiterreicht (OnError/OnCompleted immer weiterreichen). Der Name des inneren
Observers ist der Name des äußeren (falls `INamedObserver`) + " (gefiltert)".

### Namespace `WeatherStation.Core.Models`

```csharp
public sealed record WeatherReading(DateTime Time, double Temperature, double Humidity,
                                    double Pressure, double WindSpeed);

public enum WarningLevel { Info = 0, Warning = 1, Danger = 2 }
public enum WarningType { TemperatureChange, Frost, Heat, Storm, PressureDrop, AllClear }

public sealed record WeatherWarning(DateTime Time, WarningType Type, WarningLevel Level,
                                    string Title, string Message)
{
    // PATTERN: statische Fabrikmethoden (≠ GoF Factory Method, siehe README)
    public static WeatherWarning TemperatureRise(WeatherReading reading, double change);  // Info
    public static WeatherWarning TemperatureFall(WeatherReading reading, double change);  // Info
    public static WeatherWarning Frost(WeatherReading reading);            // Warning, Type Frost
    public static WeatherWarning Heat(WeatherReading reading);             // Warning, Type Heat
    public static WeatherWarning Storm(WeatherReading reading);            // Danger,  Type Storm
    public static WeatherWarning PressureDrop(WeatherReading reading, double drop, double hours); // Warning
    public static WeatherWarning AllClear(WeatherReading reading, string reason); // Info, Type AllClear
}

public static class WarningLevelExtensions
{
    public static string ToGerman(this WarningLevel level); // "Hinweis", "Warnung", "Gefahr"
}
```
Titel/Texte deutsch, z. B. Title "Frostwarnung", Message "Es hat -1,5 °C. Glättegefahr!".
Zahlen mit `CultureInfo.GetCultureInfo("de-DE")` formatieren, 1 Nachkommastelle.

### Namespace `WeatherStation.Core.Settings`

```csharp
// PATTERN: Singleton (klassisch, thread-sicher über Lazy<T>)
public sealed class WarningSettings
{
    public static WarningSettings Instance { get; }   // Lazy<WarningSettings>
    private WarningSettings();

    public Guid InstanceId { get; }           // beweist in der UI: immer dieselbe Instanz
    public DateTime CreatedAt { get; }

    public double TemperatureChangeLimit { get; } = 3.0;   // °C seit letzter Meldung
    public double FrostLimit { get; } = 0.0;               // Warnung bei <= 0 °C
    public double FrostClearLimit { get; } = 1.0;          // Entwarnung bei >= 1 °C (Hysterese)
    public double HeatLimit { get; } = 30.0;               // Warnung bei >= 30 °C
    public double HeatClearLimit { get; } = 28.0;          // Entwarnung bei <= 28 °C
    public double StormLimit { get; } = 75.0;              // Warnung bei >= 75 km/h
    public double StormClearLimit { get; } = 60.0;         // Entwarnung bei <= 60 km/h
    public double PressureDropLimit { get; } = 3.0;        // hPa ...
    public TimeSpan PressureDropWindow { get; } = TimeSpan.FromHours(3); // ... in 3 Stunden
}
```
Kommentar muss erklären: Regeln bekommen Werte per Konstruktor (nicht direkt
`Instance`), damit sie testbar bleiben – nur der Builder liest das Singleton.

### Namespace `WeatherStation.Core.Rules`

```csharp
public interface IWarningRule
{
    string Name { get; }                                         // deutsch, z. B. "Frost"
    IReadOnlyList<WeatherWarning> Check(WeatherReading reading); // leer = nichts zu melden
}
public sealed class TemperatureChangeRule(double limit) : IWarningRule
public sealed class FrostRule(double limit, double clearLimit) : IWarningRule
public sealed class HeatRule(double limit, double clearLimit) : IWarningRule
public sealed class StormRule(double limit, double clearLimit) : IWarningRule
public sealed class PressureDropRule(double limit, TimeSpan window) : IWarningRule
```
Verhalten:
- **TemperatureChange**: erster Messwert = Bezugswert, keine Meldung. Danach
  Differenz zum *zuletzt gemeldeten* Bezugswert; wenn |Differenz| >= limit →
  Rise/Fall-Meldung und Bezugswert = aktueller Wert. (Kein Spam bei kleinen Schwankungen.)
- **Frost/Heat/Storm (Hysterese)**: Zustand `isActive`. Nicht aktiv und Grenze
  erreicht → Warnung, aktiv. Aktiv und Entwarnungsgrenze erreicht → `AllClear`,
  nicht aktiv. Dazwischen: nichts. Konstruktor prüft: bei Frost `clearLimit > limit`,
  bei Heat/Storm `clearLimit < limit`, sonst `ArgumentException`.
- **PressureDrop**: merkt sich Messwerte im Zeitfenster (Queue; ältere als
  `reading.Time - window` entfernen). drop = höchster Druck im Fenster − aktueller Druck.
  Nicht aktiv und drop >= limit → Warnung (hours = Zeitspanne zwischen Maximum und jetzt,
  1 Nachkommastelle), aktiv. Aktiv und drop < limit / 2 → still zurücksetzen (keine Meldung).

### Namespace `WeatherStation.Core.Services`

```csharp
// PATTERN: Observer – ist GLEICHZEITIG Observer (von Messwerten) und Subject (für Warnungen)
public sealed class WarningService : Subject<WeatherWarning>, IObserver<WeatherReading>, INamedObserver
{
    public WarningService(IEnumerable<IWarningRule> rules);   // leere Liste erlaubt
    public string Name => "Warn-Dienst";
    public IReadOnlyList<string> RuleNames { get; }
    public void OnNext(WeatherReading reading);   // alle Regeln prüfen, jede Warnung Notify()
    public void OnError(Exception error);         // NotifyError weiterreichen
    public void OnCompleted();                    // NotifyCompleted weiterreichen
}

// PATTERN: Observer – Rolle: Subject. Die Wetterstation meldet Messwerte.
public sealed class Station : Subject<WeatherReading>
{
    public Station(string name);   // replayLastValue: true; leerer Name -> ArgumentException
    public string Name { get; }
    public WeatherReading? LastReading { get; }
    public int ReadingCount { get; }
    public void Report(WeatherReading reading);   // prüft Plausibilität, dann Notify
    public void Stop();                           // NotifyCompleted
}
```
Plausibilität (sonst `ArgumentOutOfRangeException`, Messwert wird NICHT verteilt):
Temperatur −60..60, Luftfeuchte 0..100, Druck 850..1100, Wind 0..300.
Report nach Stop → `InvalidOperationException`.

### Namespace `WeatherStation.Core.Observers` (fertige Observer)

```csharp
public sealed class WarningHistory : IObserver<WeatherWarning>, INamedObserver
{   // Name "Warn-Verlauf"; thread-sicher; max. 500 Einträge; neueste zuerst
    public IReadOnlyList<WeatherWarning> GetAll();
    public int Count { get; }
    public void Clear();
}
public sealed class ReadingHistory(int maxCount = 144) : IObserver<WeatherReading>, INamedObserver
{   // Name "Messwert-Verlauf"; thread-sicher; älteste zuerst (für Diagramme)
    public IReadOnlyList<WeatherReading> GetAll();
    public void Clear();
}
public sealed class WeatherStatistics : IObserver<WeatherReading>, INamedObserver
{   // Name "Statistik"; thread-sicher
    public int Count { get; }
    public double? MinTemperature { get; }
    public double? MaxTemperature { get; }
    public double? AverageTemperature { get; }
    public double? MaxWindSpeed { get; }
    public void Reset();
}
```

### Namespace `WeatherStation.Core.Building`

```csharp
// PATTERN: Builder
public sealed class WeatherStationBuilder
{
    public WeatherStationBuilder SetName(string name);
    public WeatherStationBuilder AddTemperatureChangeWarning();   // Werte aus WarningSettings.Instance
    public WeatherStationBuilder AddFrostWarning();
    public WeatherStationBuilder AddHeatWarning();
    public WeatherStationBuilder AddStormWarning();
    public WeatherStationBuilder AddPressureDropWarning();
    public WeatherStationBuilder AddAllWarnings();                // alle fünf
    public WeatherStationBuilder AddRule(Func<IWarningRule> createRule); // eigene Regel, pro Build() neu erzeugt (Review-Fix: kein geteilter Zustand)
    public WeatherStationBuilder KeepReadings(int count);         // Größe ReadingHistory, Standard 144
    public WeatherSystem Build();
}

// Das fertige, verkabelte Produkt des Builders.
public sealed class WeatherSystem
{
    public Station Station { get; }
    public WarningService Warnings { get; }
    public WarningHistory WarningHistory { get; }
    public ReadingHistory ReadingHistory { get; }
    public WeatherStatistics Statistics { get; }
}
```
`Build()`: kein Name → `InvalidOperationException("... SetName ...")`; keine Regel →
`InvalidOperationException`. Doppelte Add-Aufrufe derselben Standardregel nur einmal
hinzufügen. Verkabelung: Station → WarningService, Station → ReadingHistory,
Station → Statistics, WarningService → WarningHistory. Build() mehrfach
aufrufbar, liefert jeweils ein neues, unabhängiges System.

### Namespace `WeatherStation.Core.Scenarios`

```csharp
public interface ISensor
{
    // Liefert den nächsten Messwert; jeder Aufruf = 10 simulierte Minuten später.
    WeatherReading ReadNext();
}

// PATTERN: Factory Method (GoF). Die Unterklasse entscheidet, WELCHER Sensor entsteht.
public abstract class WeatherScenario
{
    public abstract string Key { get; }          // "storm", "heat", "frost", "random"
    public abstract string Name { get; }         // "Sturmfront zieht auf"
    public abstract string Description { get; }  // 1 Satz: was passiert, welche Warnungen
    protected abstract ISensor CreateSensor(DateTime startTime);   // ← die Factory Method
    public ISensor Start(DateTime startTime);    // ruft CreateSensor, prüft auf null
}
public sealed class StormScenario : WeatherScenario
public sealed class HeatWaveScenario : WeatherScenario
public sealed class FrostNightScenario : WeatherScenario
public sealed class RandomScenario(int? seed = null) : WeatherScenario

public static class ScenarioCatalog
{
    public static IReadOnlyList<WeatherScenario> All { get; }   // Reihenfolge: storm, frost, heat, random
    public static WeatherScenario Find(string key);             // unbekannt -> ArgumentException
}
```
Sensoren (`internal sealed class StormSensor` usw. in `Scenarios/Sensors/`): deterministisch
(Ausnahme Random, mit Seed reproduzierbar), Schrittweite 10 min, Zyklus wiederholt sich
endlos (z. B. 72 Schritte = 12 h), kleines seed-basiertes Rauschen erlaubt (fester Seed).
Jeder Zyklus MUSS zuverlässig auslösen (mit den Standardwerten aus WarningSettings):
- **Sturm**: Druck fällt ~1015 → ~998 (PressureDrop), Wind steigt auf ~90 km/h (Storm),
  Temperatur fällt ~18 → ~11 °C (TemperatureFall), danach Beruhigung (Storm AllClear),
  Druck steigt wieder.
- **Frostnacht**: Temperatur ~6 → ~−4 °C über Nacht (TemperatureFall, Frost), morgens
  wieder ~8 °C (Frost AllClear, TemperatureRise).
- **Hitzewelle**: ~24 → ~34 °C am Nachmittag (TemperatureRise, Heat), abends ~22 °C
  (Heat AllClear).
- **Zufall**: Random-Walk in plausiblen Grenzen.
Alle Werte müssen die Plausibilitätsprüfung der Station bestehen.

---

## ConsoleApp

- Menü beim Start: Szenario wählen (aus `ScenarioCatalog.All`), 0 = Beenden.
- System per Builder bauen (`SetName("Wetterstation Berufsschule").AddAllWarnings().Build()`).
- Eigene Observer-Klassen im Ordner `Observers/`:
  - `ReadingPrinter : IObserver<WeatherReading>` – eine Zeile pro Messwert (Uhrzeit, Werte).
  - `WarningPrinter : IObserver<WeatherWarning>` – farbig nach Level
    (Info Cyan, Warning Yellow, Danger Red + `Console.Beep()` nur unter Windows, try/catch).
  - Dazu ein Lambda-Observer über `.Where(w => w.Level == WarningLevel.Danger)`,
    der „!!! GEFAHR !!!“-Banner druckt → zeigt Filter + ActionObserver.
- Simulationsschleife: alle 700 ms ein Messwert, Taste `P` = Pause, `Q` = zurück ins Menü,
  `S` = Statistik anzeigen, `U` = Messwert-Anzeige abmelden/anmelden (zeigt Dispose!).
- Beim Verlassen: `station.Stop()` → Observer bekommen `OnCompleted` (Ausgabe sichtbar).
- `Console.OutputEncoding = UTF8`. Program.cs kurz; Logik in `WeatherConsole.cs`.

## Web (Blazor Server, Lumeo 5.12.2)

- Design: Skill `huppenkothen-design` + `lumeo` verbindlich (Shell sidebar-07, Tokens,
  Geist, hell/dunkel-Umschalter im Nutzer-Menü, startet hell). Starter-Assets aus
  aus dem internen Design-Guide (Geist-Fonts, Theme-Tokens, Override-CSS), Präfix `ws-`.
- Bewusste Vereinfachungen für ein Lernprojekt (in README nennen): Texte deutsch direkt
  im Markup (keine resx), kein Excel-Export, keine Auth, Daten im Speicher.
- DI: `WeatherSystem` als Singleton über den Builder; `SimulationRunner : BackgroundService`
  (auch als Singleton registriert) pumpt alle 1,5 s `sensor.ReadNext()` in `Station.Report`.
  Szenariowechsel: neuer Sensor startet bei `LastReading.Time + 10 min` (Zeit bleibt monoton).
  `Pause()/Resume()/IsRunning/CurrentScenario/ChangeScenario(key)`, Event `StateChanged`.
- **Jede Komponente, die abonniert, implementiert `IDisposable` und meldet sich in
  `Dispose()` ab** (Kommentar: sonst Speicherleck + Updates an tote Komponenten) und
  aktualisiert mit `InvokeAsync(StateHasChanged)` (Kommentar: Meldung kommt aus dem
  Hintergrund-Thread).
- Seiten:
  1. `/` **Übersicht**: Kennzahl-Zeile (Temperatur, Luftfeuchte, Luftdruck, Wind,
     Warnungen), Szenario-`Select` + Pause-`Switch`, Liniendiagramm Temperatur & Luftdruck
     (Lumeo.Charts, aus ReadingHistory), letzte 5 Warnungen mit Status-Pillen.
  2. `/warnungen` **Warnungen**: DataGrid (Hoverable, kein Zebra, Zähler-Fußzeile,
     Leerzustand) mit Zeit, Stufe (Pille), Art, Titel, Meldung – live.
  3. `/observer-labor` **Observer-Labor**: Liste der angemeldeten Observer von Station und
     Warn-Dienst (Namen + Anzahl, live); Knöpfe „Test-Observer anmelden/abmelden“,
     „Fehlerhaften Observer anmelden/abmelden“ (wirft Exception → ObserverFailed-Log zeigt
     Fehlerisolation), Switch „Nur Gefahr (Where-Filter)“; Protokoll (letzte 50 Einträge)
     was der Test-Observer empfängt.
  4. `/muster` **Design Patterns**: je Pattern eine Karte (Wo im Code? Warum?) +
     Singleton-Nachweis (InstanceId, CreatedAt) + alle Grenzwerte als `<Descriptions>`.
- **Benachrichtigung an den Nutzer**: Komponente `WarningToaster` im MainLayout abonniert
  `Warnings.Where(w => w.Level >= WarningLevel.Warning)` → Lumeo `ToastService`
  (Warning = Warnung, Danger = Fehler-Variante).
