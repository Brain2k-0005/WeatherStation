using WeatherStation.Core.Observer;

namespace WeatherStation.Tests;

public class ActionObserverTests
{
    [Fact]
    public void Update_CallsAction()
    {
        int received = 0;
        var observer = new ActionObserver<int>("Test", value => received = value);

        observer.Update(42);

        Assert.Equal(42, received);
        Assert.Equal("Test", observer.Name);
    }

    [Fact]
    public void StationStopped_CallsOptionalAction()
    {
        bool stopped = false;
        var observer = new ActionObserver<int>("Test", _ => { }, () => stopped = true);

        observer.StationStopped();

        Assert.True(stopped);
    }

    [Fact]
    public void StationStopped_WithoutAction_DoesNothing()
    {
        var observer = new ActionObserver<int>("Test", _ => { });

        observer.StationStopped();
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new ActionObserver<int>(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ActionObserver<int>("x", null!));
    }

    [Fact]
    public void SubscribedActionObserver_ReceivesValuesAndShowsItsName()
    {
        var subject = new TestSubject<int>();
        var received = new List<int>();

        subject.Subscribe(new ActionObserver<int>("Bildschirm", received.Add));
        subject.Publish(3);

        Assert.Equal([3], received);
        Assert.Equal(["Bildschirm"], subject.GetObserverNames());
    }
}
