using WeatherStation.Core.Observer;

namespace WeatherStation.Tests;

public class WhereFilterTests
{
    [Fact]
    public void Where_OnlyMatchingValuesAreForwarded()
    {
        var subject = new TestSubject<int>();
        var received = new List<int>();

        subject.Where(number => number % 2 == 0).Subscribe("Gerade", received.Add);
        for (int number = 1; number <= 6; number++)
        {
            subject.Publish(number);
        }

        Assert.Equal([2, 4, 6], received);
    }

    [Fact]
    public void Where_Dispose_StopsForwarding()
    {
        var subject = new TestSubject<int>();
        var received = new List<int>();
        IDisposable token = subject.Where(number => true).Subscribe("Alle", received.Add);
        subject.Publish(1);

        token.Dispose();
        subject.Publish(2);

        Assert.Equal([1], received);
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void Where_CompletionAndErrorAreAlwaysForwarded()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        subject.Where(number => false).Subscribe(observer);
        subject.Complete();
        Assert.Equal(1, observer.CompletedCount);

        var subject2 = new TestSubject<int>();
        var observer2 = new RecordingObserver<int>();
        subject2.Where(number => false).Subscribe(observer2);
        var error = new Exception("x");
        subject2.Fail(error);
        Assert.Same(error, Assert.Single(observer2.Errors));
    }

    [Fact]
    public void Where_InnerObserverNameIsOuterNamePlusFiltered()
    {
        var subject = new TestSubject<int>();

        subject.Where(number => true).Subscribe("Gefahr-Anzeige", _ => { });

        Assert.Equal(["Gefahr-Anzeige (gefiltert)"], subject.GetObserverNames());
    }

    [Fact]
    public void Where_UnnamedObserver_UsesTypeName()
    {
        var subject = new TestSubject<int>();

        subject.Where(number => true).Subscribe(new RecordingObserver<int>());

        Assert.Equal(["RecordingObserver`1 (gefiltert)"], subject.GetObserverNames());
    }

    [Fact]
    public void Where_NullArguments_Throw()
    {
        var subject = new TestSubject<int>();

        Assert.Throws<ArgumentNullException>(() => subject.Where(null!));
        Assert.Throws<ArgumentNullException>(() => ((IObservable<int>)null!).Where(number => true));
    }

    [Fact]
    public void Where_Chained_AppliesBothFilters()
    {
        var subject = new TestSubject<int>();
        var received = new List<int>();

        subject.Where(number => number > 2).Where(number => number < 6).Subscribe("Bereich", received.Add);
        for (int number = 1; number <= 8; number++)
        {
            subject.Publish(number);
        }

        Assert.Equal([3, 4, 5], received);
    }

    [Fact]
    public void Where_WithReplaySubject_FilterAppliesToReplayedValue()
    {
        var subject = new TestSubject<int>(replayLastValue: true);
        subject.Publish(1);
        var received = new List<int>();

        subject.Where(number => number > 5).Subscribe("Gross", received.Add);

        Assert.Empty(received);
    }
}
