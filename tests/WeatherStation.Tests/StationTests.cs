using WeatherStation.Core.Models;
using WeatherStation.Core.Observer;
using WeatherStation.Core.Services;

namespace WeatherStation.Tests;

public class StationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => new Station(name));
    }

    [Fact]
    public void Report_ValidReading_NotifiesObserverAndUpdatesState()
    {
        var station = new Station("Schule");
        var observer = new RecordingObserver<WeatherReading>();
        station.Subscribe(observer);
        WeatherReading reading = TestData.Reading();

        station.Report(reading);

        Assert.Equal([reading], observer.Values);
        Assert.Equal(reading, station.LastReading);
        Assert.Equal(1, station.ReadingCount);
        Assert.Equal("Schule", station.Name);
    }

    [Fact]
    public void NewStation_HasNoLastReadingAndZeroCount()
    {
        var station = new Station("Schule");

        Assert.Null(station.LastReading);
        Assert.Equal(0, station.ReadingCount);
    }

    [Fact]
    public void Subscribe_AfterReading_ReplaysLastReading()
    {
        var station = new Station("Schule");
        WeatherReading reading = TestData.Reading();
        station.Report(reading);
        var observer = new RecordingObserver<WeatherReading>();

        station.Subscribe(observer);

        Assert.Equal([reading], observer.Values);
    }

    [Theory]
    [InlineData(-60.1, 50, 1013, 10)]
    [InlineData(60.1, 50, 1013, 10)]
    [InlineData(15, -0.1, 1013, 10)]
    [InlineData(15, 100.1, 1013, 10)]
    [InlineData(15, 50, 849.9, 10)]
    [InlineData(15, 50, 1100.1, 10)]
    [InlineData(15, 50, 1013, -0.1)]
    [InlineData(15, 50, 1013, 300.1)]
    [InlineData(double.NaN, 50, 1013, 10)]
    public void Report_ImplausibleValue_ThrowsAndIsNotDistributed(double temperature, double humidity, double pressure, double wind)
    {
        var station = new Station("Schule");
        var observer = new RecordingObserver<WeatherReading>();
        station.Subscribe(observer);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            station.Report(TestData.Reading(temperature, humidity, pressure, wind)));

        Assert.Empty(observer.Values);
        Assert.Equal(0, station.ReadingCount);
        Assert.Null(station.LastReading);
    }

    [Theory]
    [InlineData(-60, 0, 850, 0)]
    [InlineData(60, 100, 1100, 300)]
    public void Report_ValuesOnTheBoundary_AreAccepted(double temperature, double humidity, double pressure, double wind)
    {
        var station = new Station("Schule");

        station.Report(TestData.Reading(temperature, humidity, pressure, wind));

        Assert.Equal(1, station.ReadingCount);
    }

    [Fact]
    public void Stop_CompletesObserversAndSubsequentReportThrows()
    {
        var station = new Station("Schule");
        var observer = new RecordingObserver<WeatherReading>();
        station.Subscribe(observer);

        station.Stop();

        Assert.Equal(1, observer.CompletedCount);
        Assert.True(station.IsCompleted);
        Assert.Throws<InvalidOperationException>(() => station.Report(TestData.Reading()));
    }

    [Fact]
    public void Stop_CalledTwice_DoesNotThrow()
    {
        var station = new Station("Schule");

        station.Stop();
        station.Stop();
    }

    [Fact]
    public void ReadingCount_CountsAllReports()
    {
        var station = new Station("Schule");

        for (int index = 0; index < 5; index++)
        {
            station.Report(TestData.Reading(minutes: index * 10));
        }

        Assert.Equal(5, station.ReadingCount);
    }
}
