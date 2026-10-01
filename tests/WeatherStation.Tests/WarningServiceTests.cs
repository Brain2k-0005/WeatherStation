using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;
using WeatherStation.Core.Rules;
using WeatherStation.Core.Services;

namespace WeatherStation.Tests;

public class WarningServiceTests
{
    [Fact]
    public void Update_ChecksAllRulesAndNotifiesEveryWarning()
    {
        var service = new WarningService([new FrostRule(0, 1), new StormRule(75, 60)]);
        var observer = new RecordingObserver<WeatherWarning>();
        service.Subscribe(observer);

        service.Update(TestData.Reading(temperature: -2, windSpeed: 90));

        Assert.Equal([WarningType.Frost, WarningType.Storm], observer.Values.Select(w => w.Type).ToList());
    }

    [Fact]
    public void Update_NoRuleTriggers_NotifiesNothing()
    {
        var service = new WarningService([new FrostRule(0, 1)]);
        var observer = new RecordingObserver<WeatherWarning>();
        service.Subscribe(observer);

        service.Update(TestData.Reading(temperature: 15));

        Assert.Empty(observer.Values);
    }

    [Fact]
    public void Constructor_EmptyRuleList_IsAllowed()
    {
        var service = new WarningService([]);

        service.Update(TestData.Reading());

        Assert.Empty(service.RuleNames);
    }

    [Fact]
    public void RuleNames_ListsRuleNamesInOrder()
    {
        var service = new WarningService([new FrostRule(0, 1), new HeatRule(30, 28)]);

        Assert.Equal(["Frost", "Hitze"], service.RuleNames);
        Assert.Equal("Warn-Dienst", service.Name);
    }

    [Fact]
    public void Chain_StationToServiceToObserver_DeliversWarnings()
    {
        var station = new Station("Test");
        var service = new WarningService([new FrostRule(0, 1)]);
        var observer = new RecordingObserver<WeatherWarning>();
        station.Subscribe(service);
        service.Subscribe(observer);

        station.SetReading(TestData.Reading(temperature: -3));

        Assert.Single(observer.Values);
    }

    [Fact]
    public void StationStopped_StopsServiceObservers()
    {
        var service = new WarningService([]);
        var observer = new RecordingObserver<WeatherWarning>();
        service.Subscribe(observer);

        service.StationStopped();

        Assert.Equal(1, observer.StoppedCount);
        Assert.True(service.IsStopped);
    }

    [Fact]
    public void StationStop_StopsWholeChain()
    {
        var station = new Station("Test");
        var service = new WarningService([]);
        var observer = new RecordingObserver<WeatherWarning>();
        station.Subscribe(service);
        service.Subscribe(observer);

        station.Stop();

        Assert.Equal(1, observer.StoppedCount);
    }

    [Fact]
    public void Update_AfterStationStopped_NoWarningsDelivered()
    {
        var service = new WarningService([new FrostRule(0, 1)]);
        var observer = new RecordingObserver<WeatherWarning>();
        service.Subscribe(observer);
        service.StationStopped();

        service.Update(TestData.Reading(temperature: -5));

        Assert.Empty(observer.Values);
    }

    [Fact]
    public void Constructor_NullRules_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WarningService(null!));
    }

    [Fact]
    public void Update_ConcurrentCalls_DoNotThrow()
    {
        var service = new WarningService([new TemperatureChangeRule(3), new PressureDropRule(3, TimeSpan.FromHours(3))]);

        Parallel.For(0, 500, index =>
            service.Update(TestData.Reading(temperature: index % 20, minutes: index)));
    }
}
