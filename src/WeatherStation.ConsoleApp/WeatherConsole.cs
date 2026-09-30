using System.Globalization;
using WeatherStation.ConsoleApp.Observers;
using WeatherStation.Core.Building;
using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;
using WeatherStation.Core.Scenarios;
using WeatherStation.Core.Settings;

namespace WeatherStation.ConsoleApp;

// Menü und Simulationsschleife der Konsolen-Oberfläche.
public sealed class WeatherConsole
{
    private const int StepDelayMilliseconds = 700;

    // Ohne Tastatur (umgeleitete Eingabe) läuft die Simulation eine feste Zahl Schritte,
    // damit man das Programm automatisch testen kann.
    private const int StepsWithoutKeyboard = 80;

    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public void Run()
    {
        while (true)
        {
            WeatherScenario? scenario = AskForScenario();
            if (scenario == null)
            {
                Console.WriteLine();
                Console.WriteLine("Auf Wiedersehen!");
                return;
            }

            RunSimulation(scenario);
        }
    }

    // ---------------------------------------------------------------
    // Menü
    // ---------------------------------------------------------------

    // Gibt null zurück, wenn der Benutzer beenden möchte (oder die Eingabe zu Ende ist).
    private WeatherScenario? AskForScenario()
    {
        while (true)
        {
            PrintHeader();
            PrintLimits();

            Console.WriteLine();
            Console.WriteLine("  Bitte ein Szenario wählen:");
            Console.WriteLine();

            IReadOnlyList<WeatherScenario> scenarios = ScenarioCatalog.All;
            for (int i = 0; i < scenarios.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"   {i + 1}) {scenarios[i].Name}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($" – {scenarios[i].Description}");
                Console.ResetColor();
            }

            Console.WriteLine("   0) Beenden");
            Console.WriteLine();
            Console.Write("  Ihre Wahl: ");

            string? input = Console.ReadLine();
            if (input == null)
            {
                return null; // Eingabe zu Ende (z. B. umgeleitet)
            }

            input = input.Trim();
            if (input == "0")
            {
                return null;
            }

            if (int.TryParse(input, out int number) && number >= 1 && number <= scenarios.Count)
            {
                return scenarios[number - 1];
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Ungültige Eingabe, bitte eine Zahl aus der Liste eingeben.");
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("  ╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("  ║          WETTERSTATION – Design Patterns             ║");
        Console.WriteLine("  ║   Observer · Builder · Factory Method · Singleton    ║");
        Console.WriteLine("  ╚══════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    // ============================================================
    // PATTERN: Singleton – Benutzer der Instanz
    // WarningSettings.Instance liefert überall dasselbe Objekt.
    // Die InstanceId beweist es: sie ist bei jedem Aufruf gleich.
    // ============================================================
    private static void PrintLimits()
    {
        WarningSettings settings = WarningSettings.Instance;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine();
        Console.WriteLine($"  Grenzwerte (Singleton, Instanz {settings.InstanceId.ToString()[..8]}):");
        Console.WriteLine(string.Format(German,
            "   Frost <= {0:0.0} °C | Hitze >= {1:0.0} °C | Sturm >= {2:0} km/h",
            settings.FrostLimit, settings.HeatLimit, settings.StormLimit));
        Console.WriteLine(string.Format(German,
            "   Temperatursprung >= {0:0.0} °C | Druckabfall >= {1:0.0} hPa in {2:0} h",
            settings.TemperatureChangeLimit, settings.PressureDropLimit, settings.PressureDropWindow.TotalHours));
        Console.ResetColor();
    }

    // ---------------------------------------------------------------
    // Simulation
    // ---------------------------------------------------------------

    private void RunSimulation(WeatherScenario scenario)
    {
        // ============================================================
        // PATTERN: Builder
        // Der Builder baut Station, Regeln, Dienste und Verlauf Schritt für Schritt
        // zusammen und verkabelt sie. Jeder Start bekommt ein frisches System.
        // ============================================================
        WeatherSystem system = new WeatherStationBuilder()
            .SetName("Wetterstation Berufsschule")
            .AddAllWarnings()
            .Build();

        // ============================================================
        // PATTERN: Factory Method
        // scenario.Start(...) ruft intern CreateSensor(...) auf. Welche Sensor-Klasse
        // entsteht, entscheidet die Unterklasse (Sturm, Frost, Hitze, Zufall).
        // Diese Klasse kennt nur das Interface ISensor.
        // ============================================================
        DateTime startTime = DateTime.Today.AddHours(6);
        ISensor sensor = scenario.Start(startTime);

        // ============================================================
        // PATTERN: Observer – hier werden die Observer angemeldet
        // Subscribe gibt ein IDisposable zurück: Dispose() = abmelden.
        // ============================================================
        var readingPrinter = new ReadingPrinter();
        IDisposable? readingSubscription = system.Station.Subscribe(readingPrinter);

        var warningPrinter = new WarningPrinter();
        IDisposable warningSubscription = system.Warnings.Subscribe(warningPrinter);

        // Lambda-Observer mit Filter: nur Warnungen der Stufe "Gefahr" kommen an.
        IDisposable dangerSubscription = system.Warnings
            .Where(warning => warning.Level == WarningLevel.Danger)
            .Subscribe("Gefahren-Banner", PrintDangerBanner);

        bool keyboardAvailable = IsKeyboardAvailable();
        PrintSimulationStart(scenario, keyboardAvailable);

        bool paused = false;
        int step = 0;

        try
        {
            while (true)
            {
                if (keyboardAvailable)
                {
                    // Nicht blockierend: nur lesen, wenn wirklich eine Taste wartet.
                    if (TryReadKey(out ConsoleKey key))
                    {
                        if (key == ConsoleKey.Q)
                        {
                            break;
                        }

                        if (key == ConsoleKey.P)
                        {
                            paused = !paused;
                            PrintInfo(paused ? "-- Pause (P zum Fortsetzen) --" : "-- Weiter --");
                        }
                        else if (key == ConsoleKey.S)
                        {
                            PrintStatistics(system);
                        }
                        else if (key == ConsoleKey.U)
                        {
                            readingSubscription = ToggleReadingPrinter(system, readingPrinter, readingSubscription);
                        }
                    }
                }
                else if (step >= StepsWithoutKeyboard)
                {
                    break;
                }

                if (!paused)
                {
                    WeatherReading reading = sensor.ReadNext();
                    system.Station.Report(reading); // verteilt an alle angemeldeten Observer
                    step++;
                    Thread.Sleep(keyboardAvailable ? StepDelayMilliseconds : 0);
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
        }
        finally
        {
            // Station stoppen: alle Observer bekommen OnCompleted und melden sich ab.
            Console.WriteLine();
            system.Station.Stop();

            // Zur Sicherheit alle Anmeldungen freigeben (Dispose ist mehrfach aufrufbar).
            readingSubscription?.Dispose();
            warningSubscription.Dispose();
            dangerSubscription.Dispose();
        }

        if (keyboardAvailable)
        {
            DiscardPendingKeys();
        }

        PrintStatistics(system);
        Console.WriteLine();
    }

    private static void PrintSimulationStart(WeatherScenario scenario, bool keyboardAvailable)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  Simulation: {scenario.Name}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        if (keyboardAvailable)
        {
            Console.WriteLine("  Tasten:  [P] Pause/Weiter   [S] Statistik   [U] Messwert-Anzeige ab-/anmelden   [Q] Zurück zum Menü");
        }
        else
        {
            Console.WriteLine($"  Keine Tastatur erkannt – es laufen {StepsWithoutKeyboard} Schritte, dann geht es zurück zum Menü.");
        }

        Console.WriteLine("  Zeit   | Temperatur |  Feuchte |      Druck |      Wind");
        Console.WriteLine("  -------+------------+----------+------------+----------");
        Console.ResetColor();
    }

    // Demonstriert IDisposable: Abmelden = Dispose(), Anmelden = neues Subscribe.
    private static IDisposable? ToggleReadingPrinter(WeatherSystem system, ReadingPrinter printer, IDisposable? subscription)
    {
        if (subscription != null)
        {
            subscription.Dispose();
            PrintInfo("-- Messwert-Anzeige abgemeldet (Dispose) --");
            return null;
        }

        // Die Station hat "replayLastValue": Nach dem Subscribe kommt sofort der letzte Messwert noch einmal.
        PrintInfo("-- Messwert-Anzeige wieder angemeldet (Subscribe, zeigt sofort den letzten Messwert) --");
        return system.Station.Subscribe(printer);
    }

    // Wird vom Lambda-Observer aufgerufen (nur bei Stufe "Gefahr").
    private static void PrintDangerBanner(WeatherWarning warning)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.DarkRed;
        Console.WriteLine();
        Console.WriteLine("  !!! GEFAHR !!!  " + warning.Title);
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintStatistics(WeatherSystem system)
    {
        var statistics = system.Statistics;

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine();
        Console.WriteLine("  ---- Statistik ----");
        Console.WriteLine($"  Messwerte:        {statistics.Count}");
        if (statistics.Count > 0)
        {
            Console.WriteLine(string.Format(German, "  Temperatur min:   {0:0.0} °C", statistics.MinTemperature));
            Console.WriteLine(string.Format(German, "  Temperatur max:   {0:0.0} °C", statistics.MaxTemperature));
            Console.WriteLine(string.Format(German, "  Temperatur Ø:     {0:0.0} °C", statistics.AverageTemperature));
            Console.WriteLine(string.Format(German, "  Wind max:         {0:0} km/h", statistics.MaxWindSpeed));
        }
        Console.WriteLine($"  Warnungen:        {system.WarningHistory.Count}");
        Console.WriteLine("  -------------------");
        Console.ResetColor();
    }

    private static void PrintInfo(string text)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("  " + text);
        Console.ResetColor();
    }

    // ---------------------------------------------------------------
    // Tastatur
    // ---------------------------------------------------------------

    // ------------------------------------------------------------
    // FÜR FORTGESCHRITTENE – beim ersten Lesen überspringen.
    // Tastatur ohne Blockieren abfragen (KeyAvailable), damit die Simulation weiterläuft.
    // Die Hilfsmethoden bis zum Dateiende gehören dazu.
    // ------------------------------------------------------------
    // Console.KeyAvailable wirft eine Exception, wenn die Eingabe umgeleitet ist.
    // Darum prüfen wir das einmal vor der Schleife.
    private static bool IsKeyboardAvailable()
    {
        if (Console.IsInputRedirected)
        {
            return false;
        }

        try
        {
            _ = Console.KeyAvailable;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    // Tasten, die während der Simulation noch gedrückt wurden, verwerfen –
    // sonst landen sie als Eingabe im Menü (z. B. ein doppeltes "q").
    private static void DiscardPendingKeys()
    {
        while (Console.KeyAvailable)
        {
            Console.ReadKey(intercept: true);
        }
    }

    private static bool TryReadKey(out ConsoleKey key)
    {
        key = ConsoleKey.NoName;
        if (!Console.KeyAvailable)
        {
            return false;
        }

        key = Console.ReadKey(intercept: true).Key;
        return true;
    }
}
