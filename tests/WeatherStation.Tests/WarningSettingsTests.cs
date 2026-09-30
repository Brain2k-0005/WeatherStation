using WeatherStation.Core.Settings;

namespace WeatherStation.Tests;

public class WarningSettingsTests
{
    [Fact]
    public void Instance_CalledTwice_ReturnsSameObject()
    {
        Assert.Same(WarningSettings.Instance, WarningSettings.Instance);
    }

    [Fact]
    public void Instance_FromParallelTasks_AlwaysSameInstanceAndId()
    {
        var instances = new WarningSettings[200];

        Parallel.For(0, instances.Length, index => instances[index] = WarningSettings.Instance);

        Assert.All(instances, instance => Assert.Same(WarningSettings.Instance, instance));
        Assert.All(instances, instance => Assert.Equal(WarningSettings.Instance.InstanceId, instance.InstanceId));
    }

    [Fact]
    public void Instance_HasDefaultValuesFromPlan()
    {
        WarningSettings settings = WarningSettings.Instance;

        Assert.Equal(3.0, settings.TemperatureChangeLimit);
        Assert.Equal(0.0, settings.FrostLimit);
        Assert.Equal(1.0, settings.FrostClearLimit);
        Assert.Equal(30.0, settings.HeatLimit);
        Assert.Equal(28.0, settings.HeatClearLimit);
        Assert.Equal(75.0, settings.StormLimit);
        Assert.Equal(60.0, settings.StormClearLimit);
        Assert.Equal(3.0, settings.PressureDropLimit);
        Assert.Equal(TimeSpan.FromHours(3), settings.PressureDropWindow);
        Assert.NotEqual(Guid.Empty, settings.InstanceId);
        Assert.NotEqual(default, settings.CreatedAt);
    }

    [Fact]
    public void Constructor_IsPrivate()
    {
        var publicConstructors = typeof(WarningSettings).GetConstructors();

        Assert.Empty(publicConstructors);
    }
}
