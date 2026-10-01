using WeatherStation.Core.Building;
using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;
using WeatherStation.Core.Scenarios;

namespace WeatherStation.Tests;

public class ScenarioTests
{
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0);

    private static WeatherSystem BuildSystem()
    {
        return new WeatherStationBuilder().SetName("Test").AddAllWarnings().KeepReadings(1000).Build();
    }

    // Lässt einen Sensor "steps" Schritte laufen und gibt alle Warnungen zurück.
    // Station.SetReading prüft dabei jeden Messwert auf Plausibilität (sonst Exception).
    private static List<WeatherWarning> Run(WeatherScenario scenario, int steps)
    {
        WeatherSystem system = BuildSystem();
        var warnings = new List<WeatherWarning>();
        system.Warnings.Subscribe(new ActionObserver<WeatherWarning>("Test", warnings.Add));
        ISensor sensor = scenario.Start(Start);

        for (int step = 0; step < steps; step++)
        {
            system.Station.SetReading(sensor.ReadNext());
        }

        Assert.Equal(steps, system.Station.ReadingCount);
        return warnings;
    }

    private static bool Has(List<WeatherWarning> warnings, WarningType type)
    {
        return warnings.Any(warning => warning.Type == type);
    }

    // ---------- Katalog ----------

    [Fact]
    public void Catalog_All_HasFourScenariosInOrder()
    {
        Assert.Equal(["storm", "frost", "heat", "random"], ScenarioCatalog.All.Select(s => s.Key).ToList());
    }

    [Fact]
    public void Catalog_All_ScenariosHaveNameAndDescription()
    {
        Assert.All(ScenarioCatalog.All, scenario =>
        {
            Assert.False(string.IsNullOrWhiteSpace(scenario.Name));
            Assert.False(string.IsNullOrWhiteSpace(scenario.Description));
        });
    }

    [Theory]
    [InlineData("storm", typeof(StormScenario))]
    [InlineData("frost", typeof(FrostNightScenario))]
    [InlineData("heat", typeof(HeatWaveScenario))]
    [InlineData("random", typeof(RandomScenario))]
    public void Catalog_Find_ReturnsMatchingScenario(string key, Type expectedType)
    {
        Assert.IsType(expectedType, ScenarioCatalog.Find(key));
    }

    [Fact]
    public void Catalog_Find_UnknownKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ScenarioCatalog.Find("tornado"));
    }

    // ---------- Factory Method ----------

    [Fact]
    public void Start_ReturnsSensorWhoseFirstReadingIsStartTime()
    {
        foreach (WeatherScenario scenario in ScenarioCatalog.All)
        {
            ISensor sensor = scenario.Start(Start);

            Assert.Equal(Start, sensor.ReadNext().Time);
        }
    }

    [Fact]
    public void Start_TwiceOnSameScenario_ReturnsIndependentSensors()
    {
        var scenario = new StormScenario();
        ISensor first = scenario.Start(Start);
        ISensor second = scenario.Start(Start);

        first.ReadNext();
        first.ReadNext();

        Assert.NotSame(first, second);
        Assert.Equal(Start, second.ReadNext().Time);
    }

    [Fact]
    public void Start_CreateSensorReturnsNull_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => new NullSensorScenario().Start(Start));
    }

    [Fact]
    public void ReadNext_EveryCall_IsTenMinutesLater()
    {
        foreach (WeatherScenario scenario in ScenarioCatalog.All)
        {
            ISensor sensor = scenario.Start(Start);
            DateTime previous = sensor.ReadNext().Time;

            for (int step = 0; step < 200; step++)
            {
                DateTime current = sensor.ReadNext().Time;
                Assert.Equal(TimeSpan.FromMinutes(10), current - previous);
                previous = current;
            }
        }
    }

    [Fact]
    public void Sensors_AreDeterministic_SameStartGivesSameValues()
    {
        foreach (WeatherScenario scenario in ScenarioCatalog.All.Where(s => s.Key != "random"))
        {
            ISensor first = scenario.Start(Start);
            ISensor second = scenario.Start(Start);

            for (int step = 0; step < 150; step++)
            {
                Assert.Equal(first.ReadNext(), second.ReadNext());
            }
        }
    }

    [Fact]
    public void RandomScenario_SameSeed_GivesSameSequence()
    {
        ISensor first = new RandomScenario(seed: 42).Start(Start);
        ISensor second = new RandomScenario(seed: 42).Start(Start);

        for (int step = 0; step < 100; step++)
        {
            Assert.Equal(first.ReadNext(), second.ReadNext());
        }
    }

    [Fact]
    public void RandomScenario_DifferentSeeds_GiveDifferentSequences()
    {
        ISensor first = new RandomScenario(seed: 1).Start(Start);
        ISensor second = new RandomScenario(seed: 2).Start(Start);
        var firstValues = new List<WeatherReading>();
        var secondValues = new List<WeatherReading>();

        for (int step = 0; step < 20; step++)
        {
            firstValues.Add(first.ReadNext());
            secondValues.Add(second.ReadNext());
        }

        Assert.NotEqual(firstValues, secondValues);
    }

    [Fact]
    public void RandomScenario_WithoutSeed_WorksAndStaysPlausible()
    {
        Run(new RandomScenario(), 500);
    }

    // ---------- Plausibilität ----------

    [Theory]
    [InlineData("storm")]
    [InlineData("frost")]
    [InlineData("heat")]
    [InlineData("random")]
    public void AllReadings_PassStationValidation_ForManyCycles(string key)
    {
        WeatherScenario scenario = key == "random" ? new RandomScenario(seed: 7) : ScenarioCatalog.Find(key);

        // 1000 Schritte = mehr als 13 Zyklen; Station.SetReading wirft bei unplausiblen Werten.
        Run(scenario, 1000);
    }

    [Fact]
    public void RandomScenario_ManySeeds_AlwaysPlausible()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            Run(new RandomScenario(seed), 800);
        }
    }

    // ---------- Warnungs-Abdeckung (mit den Standardwerten aus WarningSettings) ----------

    [Fact]
    public void StormScenario_OneCycle_TriggersPressureDropStormFallAndAllClear()
    {
        List<WeatherWarning> warnings = Run(new StormScenario(), 72);

        Assert.Contains(warnings, w => w.Type == WarningType.PressureDrop);
        Assert.Contains(warnings, w => w.Type == WarningType.Storm && w.Level == WarningLevel.Danger);
        Assert.Contains(warnings, w => w.Type == WarningType.TemperatureChange && w.Title == "Temperatursturz");
        Assert.Contains(warnings, w => w.Type == WarningType.AllClear);
        Assert.False(Has(warnings, WarningType.Frost));
    }

    [Fact]
    public void StormScenario_StormAllClearComesAfterStormWarning()
    {
        List<WeatherWarning> warnings = Run(new StormScenario(), 72);

        WeatherWarning storm = warnings.First(w => w.Type == WarningType.Storm);
        WeatherWarning clear = warnings.First(w => w.Type == WarningType.AllClear);

        Assert.True(clear.Time > storm.Time);
    }

    [Fact]
    public void FrostNightScenario_OneCycle_TriggersFallFrostAllClearAndRise()
    {
        List<WeatherWarning> warnings = Run(new FrostNightScenario(), 72);

        Assert.Contains(warnings, w => w.Type == WarningType.TemperatureChange && w.Title == "Temperatursturz");
        Assert.Contains(warnings, w => w.Type == WarningType.Frost);
        Assert.Contains(warnings, w => w.Type == WarningType.AllClear);
        Assert.Contains(warnings, w => w.Type == WarningType.TemperatureChange && w.Title == "Temperaturanstieg");
    }

    [Fact]
    public void HeatWaveScenario_OneCycle_TriggersRiseHeatAndAllClear()
    {
        List<WeatherWarning> warnings = Run(new HeatWaveScenario(), 72);

        Assert.Contains(warnings, w => w.Type == WarningType.TemperatureChange && w.Title == "Temperaturanstieg");
        Assert.Contains(warnings, w => w.Type == WarningType.Heat);
        Assert.Contains(warnings, w => w.Type == WarningType.AllClear);
    }

    [Theory]
    [InlineData("storm", WarningType.Storm)]
    [InlineData("frost", WarningType.Frost)]
    [InlineData("heat", WarningType.Heat)]
    public void Scenario_EveryCycle_TriggersItsMainWarningAgain(string key, WarningType mainType)
    {
        // Der zweite und dritte Zyklus müssen genauso warnen wie der erste.
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            List<WeatherWarning> warnings = Run(ScenarioCatalog.Find(key), 72 * cycle);

            Assert.Equal(cycle, warnings.Count(w => w.Type == mainType));
        }
    }

    [Fact]
    public void StormScenario_EveryCycle_TriggersPressureDropAgain()
    {
        for (int cycle = 1; cycle <= 3; cycle++)
        {
            List<WeatherWarning> warnings = Run(new StormScenario(), 72 * cycle);

            Assert.Equal(cycle, warnings.Count(w => w.Type == WarningType.PressureDrop));
        }
    }

    [Fact]
    public void Scenarios_ContinueAfterStationRestart_WithMonotonicTime()
    {
        // So arbeitet die Web-Anwendung: neuer Sensor startet 10 Minuten nach dem letzten Messwert.
        WeatherSystem system = BuildSystem();
        ISensor stormSensor = new StormScenario().Start(Start);
        for (int step = 0; step < 10; step++)
        {
            system.Station.SetReading(stormSensor.ReadNext());
        }

        DateTime nextStart = system.Station.LastReading!.Time.AddMinutes(10);
        ISensor frostSensor = new FrostNightScenario().Start(nextStart);

        Assert.Equal(nextStart, frostSensor.ReadNext().Time);
    }

    private sealed class NullSensorScenario : WeatherScenario
    {
        public override string Key => "null";
        public override string Name => "Null";
        public override string Description => "Liefert absichtlich keinen Sensor.";

        protected override ISensor CreateSensor(DateTime startTime) => null!;
    }
}
