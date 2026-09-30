using WeatherStation.Core.Observers;
using WeatherStation.Core.Rules;
using WeatherStation.Core.Services;
using WeatherStation.Core.Settings;

namespace WeatherStation.Core.Building;

// ============================================================
// PATTERN: Builder
// Eine Wetterstation besteht aus vielen Teilen (Station, Warn-Dienst, Regeln,
// Historien, Statistik), die richtig verkabelt werden müssen. Der Builder
// sammelt Schritt für Schritt die Wünsche ("fluent": jeder Aufruf gibt den
// Builder zurück) und baut erst in Build() alles zusammen und prüft dabei,
// ob die Angaben vollständig sind.
// Nur hier wird das Singleton WarningSettings gelesen; die Regeln bekommen
// die Werte per Konstruktor.
// ============================================================
public sealed class WeatherStationBuilder
{
    private const int DefaultReadingCount = 144;

    private string? _name;
    private int _readingCount = DefaultReadingCount;
    private bool _addTemperatureChange;
    private bool _addFrost;
    private bool _addHeat;
    private bool _addStorm;
    private bool _addPressureDrop;
    // Eigene Regeln werden als "Bauanleitung" (Funktion) gespeichert, nicht als fertiges Objekt.
    // Grund: Regeln haben einen Zustand (z. B. "Warnung aktiv"). Jedes Build() soll
    // eine NEUE Regel bekommen, sonst würden sich zwei Systeme heimlich eine Regel teilen.
    private readonly List<Func<IWarningRule>> _customRuleFactories = new();

    // Alle eigenen Regeln, die schon in ein System eingebaut wurden. Damit erkennen wir den
    // Fehler "AddRule(() => eineVorhandeneRegel)", der doch wieder dieselbe Instanz liefert.
    private readonly HashSet<IWarningRule> _usedCustomRules = new(ReferenceEqualityComparer.Instance);

    public WeatherStationBuilder SetName(string name)
    {
        _name = name;
        return this;
    }

    public WeatherStationBuilder AddTemperatureChangeWarning()
    {
        _addTemperatureChange = true;
        return this;
    }

    public WeatherStationBuilder AddFrostWarning()
    {
        _addFrost = true;
        return this;
    }

    public WeatherStationBuilder AddHeatWarning()
    {
        _addHeat = true;
        return this;
    }

    public WeatherStationBuilder AddStormWarning()
    {
        _addStorm = true;
        return this;
    }

    public WeatherStationBuilder AddPressureDropWarning()
    {
        _addPressureDrop = true;
        return this;
    }

    public WeatherStationBuilder AddAllWarnings()
    {
        return AddTemperatureChangeWarning()
            .AddFrostWarning()
            .AddHeatWarning()
            .AddStormWarning()
            .AddPressureDropWarning();
    }

    // Eigene Regel hinzufügen. Aufruf: .AddRule(() => new MeineRegel())
    // Die Funktion wird bei JEDEM Build() erneut aufgerufen und muss eine neue Regel liefern.
    public WeatherStationBuilder AddRule(Func<IWarningRule> createRule)
    {
        ArgumentNullException.ThrowIfNull(createRule);
        _customRuleFactories.Add(createRule);
        return this;
    }

    public WeatherStationBuilder KeepReadings(int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Es muss mindestens 1 Messwert gespeichert werden.");
        }
        _readingCount = count;
        return this;
    }

    public WeatherSystem Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            throw new InvalidOperationException("Die Station hat keinen Namen. Bitte zuerst SetName(...) aufrufen.");
        }

        List<IWarningRule> rules = CreateRules();
        if (rules.Count == 0)
        {
            throw new InvalidOperationException("Es wurde keine Warnregel hinzugefügt. Bitte z. B. AddAllWarnings() aufrufen.");
        }

        // Jedes Build() erzeugt komplett NEUE Objekte – auch neue Regeln,
        // denn Regeln haben einen Zustand und dürfen nicht geteilt werden.
        var station = new Station(_name);
        var warningService = new WarningService(rules);
        var warningHistory = new WarningHistory();
        var readingHistory = new ReadingHistory(_readingCount);
        var statistics = new WeatherStatistics();

        // Verkabelung: Wer hört wem zu?
        station.Subscribe(warningService);
        station.Subscribe(readingHistory);
        station.Subscribe(statistics);
        warningService.Subscribe(warningHistory);

        return new WeatherSystem(station, warningService, warningHistory, readingHistory, statistics);
    }

    private List<IWarningRule> CreateRules()
    {
        WarningSettings settings = WarningSettings.Instance;
        var rules = new List<IWarningRule>();

        // Jede Standardregel höchstens einmal (Flags statt Liste), auch bei doppeltem Add-Aufruf.
        if (_addTemperatureChange)
        {
            rules.Add(new TemperatureChangeRule(settings.TemperatureChangeLimit));
        }
        if (_addFrost)
        {
            rules.Add(new FrostRule(settings.FrostLimit, settings.FrostClearLimit));
        }
        if (_addHeat)
        {
            rules.Add(new HeatRule(settings.HeatLimit, settings.HeatClearLimit));
        }
        if (_addStorm)
        {
            rules.Add(new StormRule(settings.StormLimit, settings.StormClearLimit));
        }
        if (_addPressureDrop)
        {
            rules.Add(new PressureDropRule(settings.PressureDropLimit, settings.PressureDropWindow));
        }

        foreach (Func<IWarningRule> createRule in _customRuleFactories)
        {
            IWarningRule? rule = createRule();
            if (rule == null)
            {
                throw new InvalidOperationException("Eine Funktion aus AddRule(...) hat keine Regel geliefert (null).");
            }
            if (!_usedCustomRules.Add(rule))
            {
                throw new InvalidOperationException(
                    $"Die Regel '{rule.Name}' wurde schon einmal eingebaut (Regeln dürfen nicht geteilt werden). AddRule braucht eine Funktion, die jedes Mal eine NEUE Regel erzeugt, z. B. AddRule(() => new MeineRegel()).");
            }
            rules.Add(rule);
        }
        return rules;
    }
}
