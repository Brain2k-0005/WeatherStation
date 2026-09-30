using WeatherStation.Core.Building;
using WeatherStation.Core.Scenarios;

namespace WeatherStation.Web.Services;

// Hintergrunddienst, der die Wetterstation "leben" lässt:
// Alle 1,5 Sekunden holt er beim Sensor den nächsten Messwert und meldet ihn der Station.
// Die Station verteilt den Wert dann per Observer-Muster an alle Beobachter.
//
// Wichtig für das Verständnis: ExecuteAsync läuft in einem eigenen Thread des Servers,
// NICHT im UI-Thread. Deshalb müssen Komponenten Meldungen mit InvokeAsync(StateHasChanged)
// in ihren eigenen Kontext zurückholen.
public sealed class SimulationRunner : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan SimulatedStep = TimeSpan.FromMinutes(10);

    private readonly WeatherSystem _system;
    private readonly ILogger<SimulationRunner> _logger;

    // ------------------------------------------------------------
    // FÜR FORTGESCHRITTENE – beim ersten Lesen überspringen.
    // Thread-Sicherheit: Sperrobjekt für Sensor, Szenario und Pause-Zustand.
    // ------------------------------------------------------------
    // Ein Sperrobjekt schützt _sensor, _scenario und _isRunning.
    // Grund: Der Hintergrund-Thread liest sie, während ein Browser-Thread sie ändert.
    private readonly object _lock = new();
    private ISensor _sensor;
    private WeatherScenario _scenario;
    private bool _isRunning = true;

    public SimulationRunner(WeatherSystem system, ILogger<SimulationRunner> logger)
    {
        _system = system;
        _logger = logger;

        // Simulierte Uhr: heute 06:00 Uhr. Rein für die Anzeige, die Regeln nutzen nur reading.Time.
        DateTime startTime = DateTime.Today.AddHours(6);
        _scenario = ScenarioCatalog.Find("storm");
        _sensor = _scenario.Start(startTime);
    }

    // Wird ausgelöst, wenn sich Szenario oder Pause-Zustand ändern.
    // Seiten, die das anzeigen, melden sich hier an (und in Dispose wieder ab!).
    public event Action? StateChanged;

    public bool IsRunning
    {
        get { lock (_lock) { return _isRunning; } }
    }

    public WeatherScenario CurrentScenario
    {
        get { lock (_lock) { return _scenario; } }
    }

    public void Pause()
    {
        lock (_lock) { _isRunning = false; }
        StateChanged?.Invoke();
    }

    public void Resume()
    {
        lock (_lock) { _isRunning = true; }
        StateChanged?.Invoke();
    }

    // PATTERN: Factory Method - scenario.Start(...) entscheidet, WELCHER Sensor entsteht.
    // Hier muss niemand wissen, ob es ein Sturm- oder ein Frost-Sensor ist.
    public void ChangeScenario(string key)
    {
        WeatherScenario newScenario = ScenarioCatalog.Find(key);

        lock (_lock)
        {
            // Der neue Sensor startet 10 Minuten NACH dem letzten Messwert.
            // So springt die Zeit nie zurück (Diagramm und Regeln bleiben stimmig).
            DateTime startTime = _system.Station.LastReading is null
                ? DateTime.Today.AddHours(6)
                : _system.Station.LastReading.Time.Add(SimulatedStep);

            _scenario = newScenario;
            _sensor = newScenario.Start(startTime);
        }

        StateChanged?.Invoke();
    }

    // ------------------------------------------------------------
    // FÜR FORTGESCHRITTENE – beim ersten Lesen überspringen.
    // BackgroundService: die Schleife mit PeriodicTimer, Abbruch per CancellationToken
    // (stoppingToken) und Fehlerbehandlung, damit der Dienst nie abstürzt.
    // ------------------------------------------------------------
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval);

        while (await WaitForNextTickAsync(timer, stoppingToken))
        {
            try
            {
                lock (_lock)
                {
                    if (_isRunning)
                    {
                        // Lesen UND Melden im selben Lock: Ein Szenariowechsel kann
                        // nicht dazwischenfunken, die simulierte Zeit bleibt monoton.
                        _system.Station.Report(_sensor.ReadNext());
                    }
                }
            }
            catch (Exception exception)
            {
                // Eine fehlerhafte Messung darf den Hintergrunddienst nicht beenden.
                _logger.LogError(exception, "Fehler in der Simulationsschleife");
            }
        }
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Die Anwendung fährt herunter - das ist der normale Ausgang der Schleife.
            return false;
        }
    }
}
