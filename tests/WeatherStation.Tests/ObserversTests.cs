using WeatherStation.Core.Models;
using WeatherStation.Core.Observers;

namespace WeatherStation.Tests;

public class ObserversTests
{
    private static WeatherWarning Warning(int minutes)
    {
        return WeatherWarning.AllClear(TestData.Reading(minutes: minutes), "Grund " + minutes);
    }

    // ---------- WarningHistory ----------

    [Fact]
    public void WarningHistory_NewestFirst()
    {
        var history = new WarningHistory();

        history.OnNext(Warning(0));
        history.OnNext(Warning(10));
        history.OnNext(Warning(20));

        Assert.Equal(3, history.Count);
        Assert.Equal([20, 10, 0], history.GetAll().Select(w => (int)(w.Time - TestData.Start).TotalMinutes).ToList());
        Assert.Equal("Warn-Verlauf", history.Name);
    }

    [Fact]
    public void WarningHistory_KeepsAtMost500Entries_DroppingOldest()
    {
        var history = new WarningHistory();

        for (int index = 0; index < 510; index++)
        {
            history.OnNext(Warning(index));
        }

        IReadOnlyList<WeatherWarning> all = history.GetAll();
        Assert.Equal(500, history.Count);
        Assert.Equal(TestData.Start.AddMinutes(509), all[0].Time);
        Assert.Equal(TestData.Start.AddMinutes(10), all[^1].Time);
    }

    [Fact]
    public void WarningHistory_Clear_EmptiesList()
    {
        var history = new WarningHistory();
        history.OnNext(Warning(0));

        history.Clear();

        Assert.Equal(0, history.Count);
        Assert.Empty(history.GetAll());
    }

    [Fact]
    public void WarningHistory_GetAll_ReturnsSnapshotUnaffectedByLaterWarnings()
    {
        var history = new WarningHistory();
        history.OnNext(Warning(0));
        IReadOnlyList<WeatherWarning> snapshot = history.GetAll();

        history.OnNext(Warning(10));

        Assert.Single(snapshot);
    }

    [Fact]
    public void WarningHistory_ParallelWrites_AreNotLost()
    {
        var history = new WarningHistory();

        Parallel.For(0, 400, index => history.OnNext(Warning(index)));

        Assert.Equal(400, history.Count);
    }

    // ---------- ReadingHistory ----------

    [Fact]
    public void ReadingHistory_OldestFirst()
    {
        var history = new ReadingHistory();

        history.OnNext(TestData.Reading(temperature: 1, minutes: 0));
        history.OnNext(TestData.Reading(temperature: 2, minutes: 10));

        Assert.Equal([1.0, 2.0], history.GetAll().Select(r => r.Temperature).ToList());
        Assert.Equal("Messwert-Verlauf", history.Name);
    }

    [Fact]
    public void ReadingHistory_RespectsMaxCount_DroppingOldest()
    {
        var history = new ReadingHistory(3);

        for (int index = 1; index <= 5; index++)
        {
            history.OnNext(TestData.Reading(temperature: index, minutes: index));
        }

        Assert.Equal([3.0, 4.0, 5.0], history.GetAll().Select(r => r.Temperature).ToList());
    }

    [Fact]
    public void ReadingHistory_DefaultMaxCountIs144()
    {
        var history = new ReadingHistory();

        for (int index = 0; index < 200; index++)
        {
            history.OnNext(TestData.Reading(minutes: index));
        }

        Assert.Equal(144, history.GetAll().Count);
    }

    [Fact]
    public void ReadingHistory_Clear_EmptiesList()
    {
        var history = new ReadingHistory();
        history.OnNext(TestData.Reading());

        history.Clear();

        Assert.Empty(history.GetAll());
    }

    [Fact]
    public void ReadingHistory_InvalidMaxCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadingHistory(0));
    }

    [Fact]
    public void ReadingHistory_ParallelWrites_RespectLimit()
    {
        var history = new ReadingHistory(50);

        Parallel.For(0, 1000, index => history.OnNext(TestData.Reading(minutes: index)));

        Assert.Equal(50, history.GetAll().Count);
    }

    // ---------- WeatherStatistics ----------

    [Fact]
    public void WeatherStatistics_Empty_ReturnsNullValues()
    {
        var statistics = new WeatherStatistics();

        Assert.Equal(0, statistics.Count);
        Assert.Null(statistics.MinTemperature);
        Assert.Null(statistics.MaxTemperature);
        Assert.Null(statistics.AverageTemperature);
        Assert.Null(statistics.MaxWindSpeed);
        Assert.Equal("Statistik", statistics.Name);
    }

    [Fact]
    public void WeatherStatistics_CalculatesMinMaxAverageAndMaxWind()
    {
        var statistics = new WeatherStatistics();

        statistics.OnNext(TestData.Reading(temperature: 10, windSpeed: 5));
        statistics.OnNext(TestData.Reading(temperature: -4, windSpeed: 40));
        statistics.OnNext(TestData.Reading(temperature: 18, windSpeed: 20));

        Assert.Equal(3, statistics.Count);
        Assert.Equal(-4, statistics.MinTemperature);
        Assert.Equal(18, statistics.MaxTemperature);
        Assert.Equal(8.0, statistics.AverageTemperature);
        Assert.Equal(40, statistics.MaxWindSpeed);
    }

    [Fact]
    public void WeatherStatistics_FirstReadingSetsAllValues_EvenIfNegative()
    {
        var statistics = new WeatherStatistics();

        statistics.OnNext(TestData.Reading(temperature: -10, windSpeed: 0));

        Assert.Equal(-10, statistics.MinTemperature);
        Assert.Equal(-10, statistics.MaxTemperature);
        Assert.Equal(0, statistics.MaxWindSpeed);
    }

    [Fact]
    public void WeatherStatistics_Reset_ClearsEverythingAndRestartsCleanly()
    {
        var statistics = new WeatherStatistics();
        statistics.OnNext(TestData.Reading(temperature: 30, windSpeed: 90));

        statistics.Reset();

        Assert.Equal(0, statistics.Count);
        Assert.Null(statistics.MinTemperature);
        Assert.Null(statistics.AverageTemperature);

        statistics.OnNext(TestData.Reading(temperature: 5, windSpeed: 1));
        Assert.Equal(5, statistics.MaxTemperature);
        Assert.Equal(1, statistics.MaxWindSpeed);
    }

    [Fact]
    public void WeatherStatistics_ParallelWrites_CountIsExact()
    {
        var statistics = new WeatherStatistics();

        Parallel.For(0, 1000, index => statistics.OnNext(TestData.Reading(temperature: 10)));

        Assert.Equal(1000, statistics.Count);
        Assert.Equal(10.0, statistics.AverageTemperature);
    }
}
