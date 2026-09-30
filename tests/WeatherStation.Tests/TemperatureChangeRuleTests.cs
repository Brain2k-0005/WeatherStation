using WeatherStation.Core.Models;
using WeatherStation.Core.Rules;

namespace WeatherStation.Tests;

public class TemperatureChangeRuleTests
{
    [Fact]
    public void Check_FirstReading_IsOnlyReferenceAndReportsNothing()
    {
        var rule = new TemperatureChangeRule(3.0);

        Assert.Empty(rule.Check(TestData.Reading(temperature: 10)));
    }

    [Fact]
    public void Check_RiseOfExactlyLimit_ReportsRise()
    {
        var rule = new TemperatureChangeRule(3.0);
        rule.Check(TestData.Reading(temperature: 10));

        WeatherWarning warning = Assert.Single(rule.Check(TestData.Reading(temperature: 13, minutes: 10)));

        Assert.Equal(WarningType.TemperatureChange, warning.Type);
        Assert.Equal("Temperaturanstieg", warning.Title);
    }

    [Fact]
    public void Check_FallOfMoreThanLimit_ReportsFall()
    {
        var rule = new TemperatureChangeRule(3.0);
        rule.Check(TestData.Reading(temperature: 10));

        WeatherWarning warning = Assert.Single(rule.Check(TestData.Reading(temperature: 6.5, minutes: 10)));

        Assert.Equal("Temperatursturz", warning.Title);
    }

    [Fact]
    public void Check_SmallChangesBelowLimit_ReportNothing()
    {
        var rule = new TemperatureChangeRule(3.0);
        rule.Check(TestData.Reading(temperature: 10));

        Assert.Empty(rule.Check(TestData.Reading(temperature: 12.9, minutes: 10)));
        Assert.Empty(rule.Check(TestData.Reading(temperature: 7.1, minutes: 20)));
    }

    [Fact]
    public void Check_SlowSteadyRise_ReportsWhenDistanceToLastReportReachesLimit()
    {
        var rule = new TemperatureChangeRule(3.0);
        rule.Check(TestData.Reading(temperature: 10));

        Assert.Empty(rule.Check(TestData.Reading(temperature: 11, minutes: 10)));
        Assert.Empty(rule.Check(TestData.Reading(temperature: 12, minutes: 20)));
        Assert.Single(rule.Check(TestData.Reading(temperature: 13, minutes: 30)));
    }

    [Fact]
    public void Check_AfterReport_ReferenceIsResetToReportedValue()
    {
        var rule = new TemperatureChangeRule(3.0);
        rule.Check(TestData.Reading(temperature: 10));
        rule.Check(TestData.Reading(temperature: 14, minutes: 10));

        // 14 -> 16 sind nur 2 Grad zum neuen Bezugswert, obwohl 6 Grad zum Start.
        Assert.Empty(rule.Check(TestData.Reading(temperature: 16, minutes: 20)));
        Assert.Single(rule.Check(TestData.Reading(temperature: 17, minutes: 30)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveLimit_Throws(double limit)
    {
        Assert.Throws<ArgumentException>(() => new TemperatureChangeRule(limit));
    }

    [Fact]
    public void Name_IsGerman()
    {
        Assert.Equal("Temperaturänderung", new TemperatureChangeRule(3).Name);
    }
}
