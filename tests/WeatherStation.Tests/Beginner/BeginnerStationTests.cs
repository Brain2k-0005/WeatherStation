using WeatherStation.Beginner.Observers;
using BStation = WeatherStation.Beginner.Station;
using BReading = WeatherStation.Beginner.WeatherReading;
using IBObserver = WeatherStation.Beginner.IWeatherObserver;

namespace WeatherStation.Tests.Beginner;

public class BeginnerStationTests
{
    private sealed class RecordingObserver : IBObserver
    {
        public List<BReading> Received { get; } = new();
        public void Update(BReading reading) => Received.Add(reading);
    }

    private sealed class SelfUnsubscribingObserver : IBObserver
    {
        private readonly BStation _station;
        public int Calls { get; private set; }
        public SelfUnsubscribingObserver(BStation station) => _station = station;
        public void Update(BReading reading)
        {
            Calls++;
            _station.Unsubscribe(this);
        }
    }

    private static BReading Reading(double temp = 10, double wind = 10) => new("12:00", temp, wind);

    [Fact]
    public void SetReading_NotifiesSubscribedObserver()
    {
        var station = new BStation();
        var observer = new RecordingObserver();
        station.Subscribe(observer);
        var reading = Reading(5, 20);

        station.SetReading(reading);

        Assert.Single(observer.Received);
        Assert.Same(reading, observer.Received[0]);
        Assert.Same(reading, station.Current);
    }

    [Fact]
    public void Unsubscribe_StopsUpdates()
    {
        var station = new BStation();
        var observer = new RecordingObserver();
        station.Subscribe(observer);
        station.SetReading(Reading());

        station.Unsubscribe(observer);
        station.SetReading(Reading());

        Assert.Single(observer.Received);
    }

    [Fact]
    public void Subscribe_Duplicate_IsIgnored()
    {
        var station = new BStation();
        var observer = new RecordingObserver();
        station.Subscribe(observer);
        station.Subscribe(observer);

        station.SetReading(Reading());

        Assert.Single(observer.Received);
        Assert.Equal(1, station.ObserverCount);
    }

    [Fact]
    public void Subscribe_Null_Throws()
    {
        var station = new BStation();
        Assert.Throws<ArgumentNullException>(() => station.Subscribe(null!));
    }

    [Fact]
    public void Unsubscribe_DuringNotification_IsSafe()
    {
        var station = new BStation();
        var selfRemoving = new SelfUnsubscribingObserver(station);
        var other = new RecordingObserver();
        station.Subscribe(selfRemoving);
        station.Subscribe(other);

        station.SetReading(Reading());
        station.SetReading(Reading());

        Assert.Equal(1, selfRemoving.Calls);
        Assert.Equal(2, other.Received.Count);
    }

    [Fact]
    public void ObserverCount_TracksSubscribeAndUnsubscribe()
    {
        var station = new BStation();
        var a = new RecordingObserver();
        var b = new RecordingObserver();
        Assert.Equal(0, station.ObserverCount);

        station.Subscribe(a);
        station.Subscribe(b);
        Assert.Equal(2, station.ObserverCount);

        station.Unsubscribe(a);
        Assert.Equal(1, station.ObserverCount);
    }

    [Theory]
    [InlineData(0.1, 0)]
    [InlineData(0.0, 1)]
    [InlineData(-5.0, 1)]
    public void FrostWarner_CountsAtBoundary(double temperature, int expected)
    {
        var warner = new FrostWarner();
        warner.Update(Reading(temp: temperature));
        Assert.Equal(expected, warner.WarningCount);
    }

    [Theory]
    [InlineData(74.9, 0)]
    [InlineData(75.0, 1)]
    [InlineData(120.0, 1)]
    public void StormWarner_CountsAtBoundary(double wind, int expected)
    {
        var warner = new StormWarner();
        warner.Update(Reading(wind: wind));
        Assert.Equal(expected, warner.WarningCount);
    }

    [Fact]
    public void HighestTemperature_RemembersMaximum()
    {
        var highest = new HighestTemperature();
        Assert.Null(highest.Highest);

        highest.Update(Reading(temp: -3));
        Assert.Equal(-3, highest.Highest);
        highest.Update(Reading(temp: 15));
        highest.Update(Reading(temp: 7));

        Assert.Equal(15, highest.Highest);
    }
}
