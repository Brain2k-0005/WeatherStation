using WeatherStation.Core.Models;
using WeatherStation.Core.Rules;

namespace WeatherStation.Tests;

public class PressureDropRuleTests
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(3);

    [Fact]
    public void Check_FirstReading_ReportsNothing()
    {
        var rule = new PressureDropRule(3.0, Window);

        Assert.Empty(rule.Check(TestData.Reading(pressure: 1013)));
    }

    [Fact]
    public void Check_DropOfExactlyLimitInsideWindow_Warns()
    {
        var rule = new PressureDropRule(3.0, Window);
        rule.Check(TestData.Reading(pressure: 1013, minutes: 0));

        WeatherWarning warning = Assert.Single(rule.Check(TestData.Reading(pressure: 1010, minutes: 90)));

        Assert.Equal(WarningType.PressureDrop, warning.Type);
        Assert.Equal(WarningLevel.Warning, warning.Level);
        Assert.Contains("3,0 hPa", warning.Message);
        Assert.Contains("1,5 Stunden", warning.Message);
    }

    [Fact]
    public void Check_DropBelowLimit_ReportsNothing()
    {
        var rule = new PressureDropRule(3.0, Window);
        rule.Check(TestData.Reading(pressure: 1013, minutes: 0));

        Assert.Empty(rule.Check(TestData.Reading(pressure: 1010.5, minutes: 60)));
    }

    [Fact]
    public void Check_DropSpreadOverMoreThanWindow_ReportsNothing()
    {
        var rule = new PressureDropRule(3.0, Window);
        // 1 hPa pro Stunde: in 3 Stunden nur 3 hPa, aber der erste Wert fällt irgendwann aus dem Fenster.
        rule.Check(TestData.Reading(pressure: 1013, minutes: 0));
        rule.Check(TestData.Reading(pressure: 1012.5, minutes: 60));
        rule.Check(TestData.Reading(pressure: 1012, minutes: 120));
        rule.Check(TestData.Reading(pressure: 1011.5, minutes: 180));

        // Der Wert von Minute 0 liegt genau am Fensterrand (drin). Danach nicht mehr:
        Assert.Empty(rule.Check(TestData.Reading(pressure: 1011.2, minutes: 240)));
        Assert.Empty(rule.Check(TestData.Reading(pressure: 1010.9, minutes: 300)));
    }

    [Fact]
    public void Check_OldHighValueOutsideWindow_IsIgnored()
    {
        var rule = new PressureDropRule(3.0, Window);
        rule.Check(TestData.Reading(pressure: 1020, minutes: 0));
        rule.Check(TestData.Reading(pressure: 1012, minutes: 200));

        // 1020 (Minute 0) ist bei Minute 200 schon älter als 3 Stunden -> kein Abfall im Fenster.
        Assert.Empty(rule.Check(TestData.Reading(pressure: 1011, minutes: 210)));
    }

    [Fact]
    public void Check_UsesHighestPressureInWindow()
    {
        var rule = new PressureDropRule(3.0, Window);
        rule.Check(TestData.Reading(pressure: 1010, minutes: 0));
        rule.Check(TestData.Reading(pressure: 1015, minutes: 30));

        WeatherWarning warning = Assert.Single(rule.Check(TestData.Reading(pressure: 1011, minutes: 60)));

        Assert.Contains("4,0 hPa", warning.Message);
        Assert.Contains("0,5 Stunden", warning.Message);
    }

    [Fact]
    public void Check_WhileActive_DoesNotWarnAgain()
    {
        var rule = new PressureDropRule(3.0, Window);
        rule.Check(TestData.Reading(pressure: 1013, minutes: 0));
        Assert.Single(rule.Check(TestData.Reading(pressure: 1009, minutes: 10)));

        Assert.Empty(rule.Check(TestData.Reading(pressure: 1005, minutes: 20)));
        Assert.Empty(rule.Check(TestData.Reading(pressure: 1004, minutes: 30)));
    }

    [Fact]
    public void Check_ResetsSilentlyWhenDropBelowHalfLimit_ThenWarnsAgain()
    {
        var rule = new PressureDropRule(3.0, Window);
        rule.Check(TestData.Reading(pressure: 1013, minutes: 0));
        Assert.Single(rule.Check(TestData.Reading(pressure: 1009, minutes: 10)));

        // Drop = 1013 - 1012 = 1 < 1,5 -> still zurückgesetzt, keine Meldung.
        Assert.Empty(rule.Check(TestData.Reading(pressure: 1012, minutes: 20)));
        Assert.Single(rule.Check(TestData.Reading(pressure: 1009, minutes: 30)));
    }

    [Fact]
    public void Check_DropBetweenHalfLimitAndLimit_DoesNotReset()
    {
        var rule = new PressureDropRule(3.0, Window);
        rule.Check(TestData.Reading(pressure: 1013, minutes: 0));
        Assert.Single(rule.Check(TestData.Reading(pressure: 1009, minutes: 10)));

        // Drop = 2 (>= 1,5): bleibt aktiv, deshalb keine neue Meldung bei erneutem Fallen.
        Assert.Empty(rule.Check(TestData.Reading(pressure: 1011, minutes: 20)));
        Assert.Empty(rule.Check(TestData.Reading(pressure: 1009, minutes: 30)));
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(-1, 3)]
    [InlineData(3, 0)]
    [InlineData(3, -1)]
    public void Constructor_InvalidArguments_Throw(double limit, int windowHours)
    {
        Assert.Throws<ArgumentException>(() => new PressureDropRule(limit, TimeSpan.FromHours(windowHours)));
    }

    [Fact]
    public void Name_IsGerman()
    {
        Assert.Equal("Luftdruckabfall", new PressureDropRule(3, Window).Name);
    }
}
