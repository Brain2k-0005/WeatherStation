using WeatherStation.Core.Models;
using WeatherStation.Core.Rules;

namespace WeatherStation.Tests;

// Frost, Hitze und Sturm arbeiten alle mit Hysterese und werden deshalb gemeinsam getestet.
public class HysteresisRuleTests
{
    // ---------- Frost (Warnung <= 0, Entwarnung >= 1) ----------

    [Fact]
    public void Frost_AtLimit_WarnsOnce()
    {
        var rule = new FrostRule(0.0, 1.0);

        WeatherWarning warning = Assert.Single(rule.Check(TestData.Reading(temperature: 0.0)));
        Assert.Equal(WarningType.Frost, warning.Type);
        Assert.Empty(rule.Check(TestData.Reading(temperature: -3, minutes: 10)));
    }

    [Fact]
    public void Frost_AboveLimit_ReportsNothing()
    {
        var rule = new FrostRule(0.0, 1.0);

        Assert.Empty(rule.Check(TestData.Reading(temperature: 0.1)));
    }

    [Fact]
    public void Frost_InBetweenLimits_StaysActiveWithoutMessage()
    {
        var rule = new FrostRule(0.0, 1.0);
        rule.Check(TestData.Reading(temperature: -1));

        Assert.Empty(rule.Check(TestData.Reading(temperature: 0.5, minutes: 10)));
        // Immer noch aktiv: erneutes Absinken meldet NICHT noch einmal.
        Assert.Empty(rule.Check(TestData.Reading(temperature: -0.5, minutes: 20)));
    }

    [Fact]
    public void Frost_AtClearLimit_ReportsAllClearAndCanWarnAgain()
    {
        var rule = new FrostRule(0.0, 1.0);
        rule.Check(TestData.Reading(temperature: -1));

        WeatherWarning clear = Assert.Single(rule.Check(TestData.Reading(temperature: 1.0, minutes: 10)));
        Assert.Equal(WarningType.AllClear, clear.Type);
        Assert.Equal(WarningLevel.Info, clear.Level);

        Assert.Empty(rule.Check(TestData.Reading(temperature: 5, minutes: 20)));
        WeatherWarning again = Assert.Single(rule.Check(TestData.Reading(temperature: -2, minutes: 30)));
        Assert.Equal(WarningType.Frost, again.Type);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 0.0)]
    public void Frost_ClearLimitNotAboveLimit_Throws(double limit, double clearLimit)
    {
        Assert.Throws<ArgumentException>(() => new FrostRule(limit, clearLimit));
    }

    // ---------- Hitze (Warnung >= 30, Entwarnung <= 28) ----------

    [Fact]
    public void Heat_AtLimit_WarnsOnce()
    {
        var rule = new HeatRule(30.0, 28.0);

        WeatherWarning warning = Assert.Single(rule.Check(TestData.Reading(temperature: 30.0)));
        Assert.Equal(WarningType.Heat, warning.Type);
        Assert.Empty(rule.Check(TestData.Reading(temperature: 33, minutes: 10)));
    }

    [Fact]
    public void Heat_BelowLimit_ReportsNothing()
    {
        var rule = new HeatRule(30.0, 28.0);

        Assert.Empty(rule.Check(TestData.Reading(temperature: 29.9)));
    }

    [Fact]
    public void Heat_InBetweenLimits_StaysActive()
    {
        var rule = new HeatRule(30.0, 28.0);
        rule.Check(TestData.Reading(temperature: 31));

        Assert.Empty(rule.Check(TestData.Reading(temperature: 29, minutes: 10)));
        Assert.Empty(rule.Check(TestData.Reading(temperature: 30.5, minutes: 20)));
    }

    [Fact]
    public void Heat_AtClearLimit_ReportsAllClearAndCanWarnAgain()
    {
        var rule = new HeatRule(30.0, 28.0);
        rule.Check(TestData.Reading(temperature: 31));

        WeatherWarning clear = Assert.Single(rule.Check(TestData.Reading(temperature: 28.0, minutes: 10)));
        Assert.Equal(WarningType.AllClear, clear.Type);

        Assert.Single(rule.Check(TestData.Reading(temperature: 32, minutes: 20)));
    }

    [Theory]
    [InlineData(30.0, 30.0)]
    [InlineData(30.0, 31.0)]
    public void Heat_ClearLimitNotBelowLimit_Throws(double limit, double clearLimit)
    {
        Assert.Throws<ArgumentException>(() => new HeatRule(limit, clearLimit));
    }

    // ---------- Sturm (Warnung >= 75, Entwarnung <= 60) ----------

    [Fact]
    public void Storm_AtLimit_WarnsWithDangerLevelOnce()
    {
        var rule = new StormRule(75.0, 60.0);

        WeatherWarning warning = Assert.Single(rule.Check(TestData.Reading(windSpeed: 75.0)));
        Assert.Equal(WarningType.Storm, warning.Type);
        Assert.Equal(WarningLevel.Danger, warning.Level);
        Assert.Empty(rule.Check(TestData.Reading(windSpeed: 90, minutes: 10)));
    }

    [Fact]
    public void Storm_BelowLimit_ReportsNothing()
    {
        var rule = new StormRule(75.0, 60.0);

        Assert.Empty(rule.Check(TestData.Reading(windSpeed: 74.9)));
    }

    [Fact]
    public void Storm_InBetweenLimits_StaysActive()
    {
        var rule = new StormRule(75.0, 60.0);
        rule.Check(TestData.Reading(windSpeed: 80));

        Assert.Empty(rule.Check(TestData.Reading(windSpeed: 65, minutes: 10)));
        Assert.Empty(rule.Check(TestData.Reading(windSpeed: 78, minutes: 20)));
    }

    [Fact]
    public void Storm_AtClearLimit_ReportsAllClearAndCanWarnAgain()
    {
        var rule = new StormRule(75.0, 60.0);
        rule.Check(TestData.Reading(windSpeed: 80));

        WeatherWarning clear = Assert.Single(rule.Check(TestData.Reading(windSpeed: 60.0, minutes: 10)));
        Assert.Equal(WarningType.AllClear, clear.Type);

        Assert.Single(rule.Check(TestData.Reading(windSpeed: 100, minutes: 20)));
    }

    [Theory]
    [InlineData(75.0, 75.0)]
    [InlineData(75.0, 80.0)]
    public void Storm_ClearLimitNotBelowLimit_Throws(double limit, double clearLimit)
    {
        Assert.Throws<ArgumentException>(() => new StormRule(limit, clearLimit));
    }

    [Fact]
    public void Names_AreGerman()
    {
        Assert.Equal("Frost", new FrostRule(0, 1).Name);
        Assert.Equal("Hitze", new HeatRule(30, 28).Name);
        Assert.Equal("Sturm", new StormRule(75, 60).Name);
    }
}
