using WeatherStation.Core.Observer;

namespace WeatherStation.Tests;

public class ActionObserverTests
{
    [Fact]
    public void OnNext_CallsAction()
    {
        int received = 0;
        var observer = new ActionObserver<int>("Test", value => received = value);

        observer.OnNext(42);

        Assert.Equal(42, received);
        Assert.Equal("Test", observer.Name);
    }

    [Fact]
    public void OnErrorAndOnCompleted_CallOptionalActions()
    {
        Exception? receivedError = null;
        bool completed = false;
        var observer = new ActionObserver<int>("Test", _ => { }, error => receivedError = error, () => completed = true);
        var error = new Exception("x");

        observer.OnError(error);
        observer.OnCompleted();

        Assert.Same(error, receivedError);
        Assert.True(completed);
    }

    [Fact]
    public void OnErrorAndOnCompleted_WithoutActions_DoNothing()
    {
        var observer = new ActionObserver<int>("Test", _ => { });

        observer.OnError(new Exception());
        observer.OnCompleted();
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new ActionObserver<int>(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ActionObserver<int>("x", null!));
    }

    [Fact]
    public void SubscribeExtension_UsesNameAndReceivesValues()
    {
        var subject = new TestSubject<int>();
        var received = new List<int>();

        subject.Subscribe("Bildschirm", received.Add);
        subject.Publish(3);

        Assert.Equal([3], received);
        Assert.Equal(["Bildschirm"], subject.GetObserverNames());
    }
}
