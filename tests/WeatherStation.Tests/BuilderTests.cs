using WeatherStation.Core.Building;
using WeatherStation.Core.Models;
using WeatherStation.Core.Rules;

namespace WeatherStation.Tests;

public class BuilderTests
{
    [Fact]
    public void Build_WithoutName_ThrowsAndMentionsSetName()
    {
        var builder = new WeatherStationBuilder().AddAllWarnings();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("SetName", exception.Message);
    }

    [Fact]
    public void Build_WithEmptyName_Throws()
    {
        var builder = new WeatherStationBuilder().SetName("  ").AddAllWarnings();

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithoutRules_Throws()
    {
        var builder = new WeatherStationBuilder().SetName("Schule");

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void AddAllWarnings_AddsFiveStandardRules()
    {
        WeatherSystem system = new WeatherStationBuilder().SetName("Schule").AddAllWarnings().Build();

        Assert.Equal(
            ["Temperaturänderung", "Frost", "Hitze", "Sturm", "Luftdruckabfall"],
            system.Warnings.RuleNames);
        Assert.Equal("Schule", system.Station.Name);
    }

    [Fact]
    public void Build_DuplicateAddCalls_AddStandardRuleOnlyOnce()
    {
        WeatherSystem system = new WeatherStationBuilder()
            .SetName("Schule")
            .AddFrostWarning()
            .AddFrostWarning()
            .AddAllWarnings()
            .Build();

        Assert.Equal(1, system.Warnings.RuleNames.Count(name => name == "Frost"));
        Assert.Equal(5, system.Warnings.RuleNames.Count);
    }

    [Fact]
    public void AddRule_CustomRuleIsIncluded()
    {
        WeatherSystem system = new WeatherStationBuilder()
            .SetName("Schule")
            .AddRule(() => new StormRule(50, 40))
            .Build();

        Assert.Equal(["Sturm"], system.Warnings.RuleNames);
    }

    [Fact]
    public void AddRule_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WeatherStationBuilder().AddRule(null!));
    }

    [Fact]
    public void AddRule_FactoryReturnsNull_BuildThrows()
    {
        var builder = new WeatherStationBuilder().SetName("Schule").AddRule(() => null!);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void AddRule_BuildCalledTwice_EachSystemGetsItsOwnRuleState()
    {
        int createdRules = 0;
        var builder = new WeatherStationBuilder()
            .SetName("Schule")
            .AddRule(() =>
            {
                createdRules++;
                return new StormRule(50, 40);
            });

        WeatherSystem first = builder.Build();
        WeatherSystem second = builder.Build();

        // Das erste System hat Sturm und damit eine aktive Warnung ...
        first.Station.Report(TestData.Reading(windSpeed: 80));
        // ... das zweite muss trotzdem selbst warnen (kein geteilter Zustand).
        second.Station.Report(TestData.Reading(windSpeed: 80));

        Assert.Equal(2, createdRules);
        Assert.Equal(1, first.WarningHistory.Count);
        Assert.Equal(1, second.WarningHistory.Count);
    }

    [Fact]
    public void AddRule_FactoryReturnsSameInstanceTwice_SecondBuildThrows()
    {
        var sharedRule = new StormRule(50, 40);
        var builder = new WeatherStationBuilder().SetName("Schule").AddRule(() => sharedRule);

        builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("NEUE Regel", exception.Message);
    }

    [Fact]
    public void Build_WiresStationToServiceHistoryAndStatistics()
    {
        WeatherSystem system = new WeatherStationBuilder().SetName("Schule").AddFrostWarning().Build();

        system.Station.Report(TestData.Reading(temperature: -3));

        Assert.Single(system.ReadingHistory.GetAll());
        Assert.Equal(1, system.Statistics.Count);
        Assert.Equal(1, system.WarningHistory.Count);
        Assert.Equal(WarningType.Frost, system.WarningHistory.GetAll()[0].Type);
    }

    [Fact]
    public void Build_ObserverNamesOfWiring_AreVisible()
    {
        WeatherSystem system = new WeatherStationBuilder().SetName("Schule").AddFrostWarning().Build();

        Assert.Equal(["Warn-Dienst", "Messwert-Verlauf", "Statistik"], system.Station.GetObserverNames());
        Assert.Equal(["Warn-Verlauf"], system.Warnings.GetObserverNames());
    }

    [Fact]
    public void Build_CalledTwice_ProducesIndependentSystems()
    {
        var builder = new WeatherStationBuilder().SetName("Schule").AddFrostWarning();
        WeatherSystem first = builder.Build();
        WeatherSystem second = builder.Build();

        first.Station.Report(TestData.Reading(temperature: -3));

        Assert.NotSame(first.Station, second.Station);
        Assert.NotSame(first.Warnings, second.Warnings);
        Assert.Equal(1, first.WarningHistory.Count);
        Assert.Equal(0, second.WarningHistory.Count);
        Assert.Equal(0, second.Station.ReadingCount);

        // Auch die Regel-Zustände sind getrennt: Das zweite System warnt selbst bei Frost.
        second.Station.Report(TestData.Reading(temperature: -3));
        Assert.Equal(1, second.WarningHistory.Count);
    }

    [Fact]
    public void KeepReadings_LimitsReadingHistory()
    {
        WeatherSystem system = new WeatherStationBuilder()
            .SetName("Schule")
            .AddFrostWarning()
            .KeepReadings(2)
            .Build();

        for (int index = 0; index < 5; index++)
        {
            system.Station.Report(TestData.Reading(temperature: 10 + index, minutes: index * 10));
        }

        Assert.Equal([13.0, 14.0], system.ReadingHistory.GetAll().Select(r => r.Temperature).ToList());
    }

    [Fact]
    public void KeepReadings_DefaultIs144()
    {
        WeatherSystem system = new WeatherStationBuilder().SetName("Schule").AddFrostWarning().Build();

        for (int index = 0; index < 150; index++)
        {
            system.Station.Report(TestData.Reading(minutes: index * 10));
        }

        Assert.Equal(144, system.ReadingHistory.GetAll().Count);
    }

    [Fact]
    public void KeepReadings_InvalidCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WeatherStationBuilder().KeepReadings(0));
    }

    [Fact]
    public void StationStop_CompletesWiredSystem()
    {
        WeatherSystem system = new WeatherStationBuilder().SetName("Schule").AddAllWarnings().Build();

        system.Station.Stop();

        Assert.True(system.Station.IsCompleted);
        Assert.True(system.Warnings.IsCompleted);
    }
}
