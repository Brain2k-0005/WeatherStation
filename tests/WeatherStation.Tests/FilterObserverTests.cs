using WeatherStation.Core.Observer;

namespace WeatherStation.Tests;

public class FilterObserverTests
{
    private static FilterObserver<int> Filter(IWeatherObserver<int> target, Func<int, bool> filter) => new(target, filter);

    [Fact]
    public void Update_OnlyMatchingValuesAreForwarded()
    {
        var subject = new TestSubject<int>();
        var received = new List<int>();

        subject.Subscribe(Filter(new ActionObserver<int>("Gerade", received.Add), number => number % 2 == 0));
        for (int number = 1; number <= 6; number++)
        {
            subject.Publish(number);
        }

        Assert.Equal([2, 4, 6], received);
    }

    [Fact]
    public void Unsubscribe_StopsForwarding()
    {
        var subject = new TestSubject<int>();
        var received = new List<int>();
        var filter = Filter(new ActionObserver<int>("Alle", received.Add), number => true);
        subject.Subscribe(filter);
        subject.Publish(1);

        subject.Unsubscribe(filter);
        subject.Publish(2);

        Assert.Equal([1], received);
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void StationStopped_IsAlwaysForwarded()
    {
        var subject = new TestSubject<int>();
        var target = new RecordingObserver<int>();
        subject.Subscribe(Filter(target, number => false));

        subject.Stop();

        Assert.Equal(1, target.StoppedCount);
    }

    [Fact]
    public void Name_IsTargetNamePlusFiltered()
    {
        var subject = new TestSubject<int>();

        subject.Subscribe(Filter(new ActionObserver<int>("Gefahr-Anzeige", _ => { }), number => true));

        Assert.Equal(["Gefahr-Anzeige (gefiltert)"], subject.GetObserverNames());
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        var target = new RecordingObserver<int>();

        Assert.Throws<ArgumentNullException>(() => new FilterObserver<int>(null!, number => true));
        Assert.Throws<ArgumentNullException>(() => new FilterObserver<int>(target, null!));
    }

    [Fact]
    public void Chained_AppliesBothFilters()
    {
        var subject = new TestSubject<int>();
        var received = new List<int>();
        var inner = Filter(new ActionObserver<int>("Bereich", received.Add), number => number < 6);

        subject.Subscribe(Filter(inner, number => number > 2));
        for (int number = 1; number <= 8; number++)
        {
            subject.Publish(number);
        }

        Assert.Equal([3, 4, 5], received);
    }

    [Fact]
    public void SubjectWithLastValue_FilterAppliesToTheSentLastValue()
    {
        var subject = new TestSubject<int>(sendLastValueToNewObservers: true);
        subject.Publish(1);
        var received = new List<int>();

        subject.Subscribe(Filter(new ActionObserver<int>("Gross", received.Add), number => number > 5));

        Assert.Empty(received);
    }
}
