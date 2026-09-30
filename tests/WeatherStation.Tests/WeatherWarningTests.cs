using WeatherStation.Core.Models;

namespace WeatherStation.Tests;

public class WeatherWarningTests
{
    private static readonly WeatherReading Reading = TestData.Reading(temperature: -1.5, pressure: 1000.25, windSpeed: 80, minutes: 30);

    [Fact]
    public void Frost_HasWarningLevelAndGermanFormattedText()
    {
        WeatherWarning warning = WeatherWarning.Frost(Reading);

        Assert.Equal(WarningType.Frost, warning.Type);
        Assert.Equal(WarningLevel.Warning, warning.Level);
        Assert.Equal("Frostwarnung", warning.Title);
        Assert.Equal("Es hat -1,5 °C. Glättegefahr!", warning.Message);
        Assert.Equal(Reading.Time, warning.Time);
    }

    [Fact]
    public void Heat_HasWarningLevel()
    {
        WeatherWarning warning = WeatherWarning.Heat(TestData.Reading(temperature: 31.04));

        Assert.Equal(WarningType.Heat, warning.Type);
        Assert.Equal(WarningLevel.Warning, warning.Level);
        Assert.Contains("31,0 °C", warning.Message);
    }

    [Fact]
    public void Storm_HasDangerLevel()
    {
        WeatherWarning warning = WeatherWarning.Storm(Reading);

        Assert.Equal(WarningType.Storm, warning.Type);
        Assert.Equal(WarningLevel.Danger, warning.Level);
        Assert.Contains("80,0 km/h", warning.Message);
    }

    [Fact]
    public void TemperatureRiseAndFall_AreInfoWithTemperatureChangeType()
    {
        WeatherWarning rise = WeatherWarning.TemperatureRise(Reading, 3.2);
        WeatherWarning fall = WeatherWarning.TemperatureFall(Reading, -3.2);

        Assert.Equal(WarningType.TemperatureChange, rise.Type);
        Assert.Equal(WarningType.TemperatureChange, fall.Type);
        Assert.Equal(WarningLevel.Info, rise.Level);
        Assert.Equal(WarningLevel.Info, fall.Level);
        Assert.Contains("3,2", rise.Message);
        Assert.Contains("3,2", fall.Message);
        Assert.NotEqual(rise.Title, fall.Title);
    }

    [Fact]
    public void PressureDrop_HasWarningLevelAndContainsDropAndHours()
    {
        WeatherWarning warning = WeatherWarning.PressureDrop(Reading, 4.5, 2.5);

        Assert.Equal(WarningType.PressureDrop, warning.Type);
        Assert.Equal(WarningLevel.Warning, warning.Level);
        Assert.Contains("4,5 hPa", warning.Message);
        Assert.Contains("2,5 Stunden", warning.Message);
    }

    [Fact]
    public void AllClear_HasInfoLevelAndUsesReasonAsMessage()
    {
        WeatherWarning warning = WeatherWarning.AllClear(Reading, "Alles gut.");

        Assert.Equal(WarningType.AllClear, warning.Type);
        Assert.Equal(WarningLevel.Info, warning.Level);
        Assert.Equal("Entwarnung", warning.Title);
        Assert.Equal("Alles gut.", warning.Message);
    }

    [Theory]
    [InlineData(WarningLevel.Info, "Hinweis")]
    [InlineData(WarningLevel.Warning, "Warnung")]
    [InlineData(WarningLevel.Danger, "Gefahr")]
    public void ToGerman_ReturnsGermanText(WarningLevel level, string expected)
    {
        Assert.Equal(expected, level.ToGerman());
    }

    [Fact]
    public void WarningLevel_IsOrderedByImportance()
    {
        Assert.True(WarningLevel.Danger > WarningLevel.Warning);
        Assert.True(WarningLevel.Warning > WarningLevel.Info);
    }
}
