namespace WeatherStation.Web.Services;

// Alle Code-Beispiele der Pattern-Lernseiten (/muster/...).
// Sie stehen hier als Text, damit das Markup der Seiten lesbar bleibt. Die Beispiele sind
// gekürzte Kopien aus dem echten Code (Dateiname steht jeweils in der Überschrift) und werden
// NICHT ausgeführt. Raw-String-Literale ("""): Anführungszeichen und Klammern brauchen keine Maskierung.
public static class PatternSnippets
{
    // ---------------------------------------------------------------
    // Observer
    // ---------------------------------------------------------------

    public const string ObserverWithout = """
        // AUSGEDACHTES Gegenbeispiel: So steht es NICHT im Projekt (FileLogger gibt es dort nicht).
        // So NICHT: Die Station kennt alle Empfänger und ruft sie direkt auf.
        public class Station
        {
            private readonly ScreenDisplay _display = new();
            private readonly FrostWarner _frostWarner = new();
            private readonly FileLogger _logger = new("wetter.log");

            public void SetReading(WeatherReading reading)
            {
                _display.Show(reading);
                _frostWarner.Check(reading);
                _logger.Write(reading);
            }
        }
        """;

    public const string ObserverContract = """
        // IWeatherObserver.cs - der Vertrag (Stufe 1)
        public interface IWeatherObserver
        {
            void Update(WeatherReading reading);
        }
        """;

    public const string ObserverSubject = """
        // Station.cs - das Subject (Stufe 1)
        public class Station
        {
            private readonly List<IWeatherObserver> _observers = new();

            public void Subscribe(IWeatherObserver observer)
            {
                if (_observers.Contains(observer)) return;   // doppelt anmelden bringt nichts
                _observers.Add(observer);
            }

            public void Unsubscribe(IWeatherObserver observer) => _observers.Remove(observer);

            public void SetReading(WeatherReading reading)
            {
                Current = reading;
                NotifyObservers(reading);
            }

            private void NotifyObservers(WeatherReading reading)
            {
                // ToList() = Kopie, damit sich ein Observer während der Schleife abmelden darf.
                foreach (IWeatherObserver observer in _observers.ToList())
                {
                    observer.Update(reading);
                }
            }
        }
        """;

    public const string ObserverUse = """
        // Program.cs - anmelden
        var station = new Station();
        station.Subscribe(new ScreenDisplay());
        station.Subscribe(new FrostWarner());
        station.Subscribe(new StormWarner());

        // Der SMS-Warner von morgen: eine neue Klasse + EINE Zeile.
        // An der Station ändert sich nichts.
        station.Subscribe(new SmsWarner());
        """;

    public const string ObserverBlazor = """
        // Warnings.razor - so macht es jede Blazor-Seite
        private IWeatherObserver<WeatherWarning>? _observer;   // im FELD merken!

        protected override void OnInitialized()
        {
            _observer = new ActionObserver<WeatherWarning>("Warnungen-Tabelle", warning => Refresh());
            Weather.Warnings.Subscribe(_observer);
        }

        public void Dispose()
        {
            _disposed = true;
            Weather.Warnings.Unsubscribe(_observer!);   // ohne diese Zeile: Speicherleck
        }
        """;

    // ---------------------------------------------------------------
    // Builder
    // ---------------------------------------------------------------

    public const string BuilderWithout = """
        // AUSGEDACHTES Gegenbeispiel: Diesen Konstruktor gibt es im Projekt NICHT.
        // So NICHT: ein langer Konstruktor ...
        var system = new WeatherSystem(
            "Wetterstation Berufsschule",
            true, true, true, true, false,        // welches true war welche Regel?
            144,
            3.0, 0.0, 1.0, 30.0, 28.0);          // welcher Grenzwert war welcher?

        // ... oder die Teile von Hand zusammenstecken - in JEDEM Programm:
        var station = new Station("Wetterstation Berufsschule");
        var warningService = new WarningService(rules);
        var readingHistory = new ReadingHistory(144);
        var statistics = new WeatherStatistics();

        station.Subscribe(warningService);
        station.Subscribe(readingHistory);
        // station.Subscribe(statistics);   <- vergessen! Die Statistik bleibt leer, niemand merkt es.
        """;

    public const string BuilderUse = """
        // Program.cs (Web) - liest sich wie eine Bestellung
        builder.Services.AddSingleton(_ => new WeatherStationBuilder()
            .SetName("Wetterstation Berufsschule")
            .AddAllWarnings()          // Frost, Hitze, Sturm, Luftdruck, Temperatursprung
            .KeepReadings(144)         // 144 Messwerte x 10 Minuten = 24 Stunden
            .Build());
        """;

    public const string BuilderBuild = """
        // WeatherStationBuilder.Build() - die Verkabelung steht EINMAL hier
        var station = new Station(_name);
        var warningService = new WarningService(rules);
        var warningHistory = new WarningHistory();
        var readingHistory = new ReadingHistory(_readingCount);
        var statistics = new WeatherStatistics();

        // Wer hört wem zu?
        station.Subscribe(warningService);
        station.Subscribe(readingHistory);
        station.Subscribe(statistics);
        warningService.Subscribe(warningHistory);

        return new WeatherSystem(station, warningService, warningHistory, readingHistory, statistics);
        """;

    // ---------------------------------------------------------------
    // Factory Method
    // ---------------------------------------------------------------

    public const string FactoryWithout = """
        // AUSGEDACHTES Gegenbeispiel: So steht es NICHT im Projekt (die Sensor-Klassen sind internal).
        // So NICHT: Der Aufrufer kennt jede Sensor-Klasse.
        ISensor CreateSensor(string scenarioKey, DateTime startTime)
        {
            switch (scenarioKey)
            {
                case "storm":  return new StormSensor(startTime);
                case "frost":  return new FrostNightSensor(startTime);
                case "heat":   return new HeatWaveSensor(startTime);
                case "random": return new RandomSensor(startTime, seed: null);
                default: throw new ArgumentException($"Unbekanntes Szenario: {scenarioKey}");
            }
        }

        // Und an einer zweiten Stelle noch einmal, für das Menü:
        string GetScenarioName(string key) => key switch
        {
            "storm"  => "Sturmfront zieht auf",
            "frost"  => "Frostnacht",
            // ...
        };
        """;

    public const string FactoryCreator = """
        // WeatherScenario.cs - der Creator mit der Factory Method
        public abstract class WeatherScenario
        {
            public abstract string Key { get; }
            public abstract string Name { get; }
            public abstract string Description { get; }

            // <-- Die Factory Method: Die Unterklasse entscheidet, welcher Sensor entsteht.
            protected abstract ISensor CreateSensor(DateTime startTime);

            // Der Ablauf ist für alle Szenarien gleich.
            public ISensor Start(DateTime startTime)
            {
                ISensor? sensor = CreateSensor(startTime);
                if (sensor == null)
                {
                    throw new InvalidOperationException($"Das Szenario '{Key}' hat keinen Sensor erzeugt.");
                }
                return sensor;
            }
        }
        """;

    public const string FactoryConcrete = """
        // StormScenario.cs - ein ConcreteCreator
        public sealed class StormScenario : WeatherScenario
        {
            public override string Key => "storm";
            public override string Name => "Sturmfront zieht auf";
            public override string Description => "Der Luftdruck fällt, der Wind steigt ...";

            protected override ISensor CreateSensor(DateTime startTime)
            {
                return new StormSensor(startTime);
            }
        }

        // SimulationRunner.cs - der Aufrufer kennt nur WeatherScenario und ISensor
        WeatherScenario newScenario = ScenarioCatalog.Find(key);
        _sensor = newScenario.Start(startTime);
        """;

    // ---------------------------------------------------------------
    // Singleton
    // ---------------------------------------------------------------

    public const string SingletonWithout = """
        // AUSGEDACHTES Gegenbeispiel (den Builder-Konstruktor mit Grenzwerten gibt es nicht).
        // Weg 1 - So NICHT: Die Einstellungen wandern durch jede Methode.
        void Run(double frostLimit, double frostClearLimit, double heatLimit, double heatClearLimit,
                 double stormLimit, double stormClearLimit, double pressureLimit, TimeSpan pressureWindow)
        {
            var builder = new WeatherStationBuilder(frostLimit, frostClearLimit, heatLimit, ...);
            PrintLimits(frostLimit, frostClearLimit, heatLimit, heatClearLimit, ...);
        }

        // Weg 2 - So auch NICHT: dieselben Zahlen an mehreren Stellen
        // (stell dir WarningSettings mit öffentlichem Konstruktor vor).
        public class FrostRule
        {
            private const double Limit = 0.0;        // hier
        }
        public class ConsolePrinter
        {
            public void PrintLimits() => Console.WriteLine("Frost bei 0 °C");   // und hier noch einmal
        }
        public class SettingsPage
        {
            private readonly WarningSettings _settings = new WarningSettings();   // eigene Instanz!
        }
        """;

    public const string SingletonWith = """
        // WarningSettings.cs
        public sealed class WarningSettings
        {
            // Lazy<T>: Die Instanz entsteht beim ersten Zugriff, genau einmal, thread-sicher
            // (Standard-Modus ExecutionAndPublication). sealed: keine Unterklassen.
            private static readonly Lazy<WarningSettings> LazyInstance = new(() => new WarningSettings());

            // private: Niemand außer der Klasse selbst kann "new WarningSettings()" schreiben.
            private WarningSettings()
            {
                InstanceId = Guid.NewGuid();
                CreatedAt = DateTime.Now;
            }

            // Der EINZIGE Weg an das Objekt.
            public static WarningSettings Instance => LazyInstance.Value;

            public Guid InstanceId { get; }
            public DateTime CreatedAt { get; }

            public double FrostLimit { get; } = 0.0;
            public double FrostClearLimit { get; } = 1.0;
            // ... weitere Grenzwerte (nur lesbar!)
        }
        """;

    public const string SingletonUse = """
        // Benutzen - überall dasselbe Objekt
        WarningSettings settings = WarningSettings.Instance;

        // WeatherStationBuilder.CreateRules() - im Core liest nur der Builder das Singleton
        // (Konsole, Web-Program.cs und die Singleton-Seite lesen es zusätzlich, nur zum Anzeigen).
        // Er reicht die Werte per Konstruktor weiter. Dadurch bleibt FrostRule testbar.
        rules.Add(new FrostRule(settings.FrostLimit, settings.FrostClearLimit));
        """;
}
