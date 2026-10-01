using WeatherStation.Core.Observer;

namespace WeatherStation.Tests;

public class SubjectTests
{
    [Fact]
    public void Subscribe_NullObserver_ThrowsArgumentNullException()
    {
        var subject = new TestSubject<int>();

        Assert.Throws<ArgumentNullException>(() => subject.Subscribe(null!));
    }

    [Fact]
    public void Unsubscribe_NullObserver_ThrowsArgumentNullException()
    {
        var subject = new TestSubject<int>();

        Assert.Throws<ArgumentNullException>(() => subject.Unsubscribe(null!));
    }

    [Fact]
    public void NotifyObservers_WithTwoObservers_BothReceiveValuesInOrder()
    {
        var subject = new TestSubject<int>();
        var first = new RecordingObserver<int>();
        var second = new RecordingObserver<int>();
        subject.Subscribe(first);
        subject.Subscribe(second);

        subject.Publish(1);
        subject.Publish(2);

        Assert.Equal([1, 2], first.Values);
        Assert.Equal([1, 2], second.Values);
    }

    [Fact]
    public void Unsubscribe_ObserverGetsNoMoreValues()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        subject.Subscribe(observer);
        subject.Publish(1);

        subject.Unsubscribe(observer);
        subject.Publish(2);

        Assert.Equal([1], observer.Values);
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void Unsubscribe_UnknownObserver_DoesNothing()
    {
        var subject = new TestSubject<int>();
        var known = new RecordingObserver<int>();
        subject.Subscribe(known);

        subject.Unsubscribe(new RecordingObserver<int>());
        subject.Publish(5);

        Assert.Equal(1, subject.ObserverCount);
        Assert.Equal([5], known.Values);
    }

    [Fact]
    public void Unsubscribe_CalledTwice_DoesNotRemoveOtherObserver()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        var other = new RecordingObserver<int>();
        subject.Subscribe(observer);
        subject.Subscribe(other);

        subject.Unsubscribe(observer);
        subject.Unsubscribe(observer);
        subject.Publish(5);

        Assert.Equal(1, subject.ObserverCount);
        Assert.Empty(observer.Values);
        Assert.Equal([5], other.Values);
    }

    [Fact]
    public void Subscribe_SameObserverTwice_IsIgnored()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        subject.Subscribe(observer);
        subject.Subscribe(observer);

        subject.Publish(1);

        Assert.Equal(1, subject.ObserverCount);
        Assert.Equal([1], observer.Values);
    }

    [Fact]
    public void NotifyObservers_ObserverUnsubscribesItselfDuringNotify_NoExceptionAndOthersStillNotified()
    {
        var subject = new TestSubject<int>();
        IWeatherObserver<int>? selfRemover = null;
        int selfCalls = 0;
        selfRemover = new ActionObserver<int>("Selbstabmelder", _ =>
        {
            selfCalls++;
            subject.Unsubscribe(selfRemover!);
        });
        subject.Subscribe(selfRemover);
        var other = new RecordingObserver<int>();
        subject.Subscribe(other);

        subject.Publish(1);
        subject.Publish(2);

        Assert.Equal(1, selfCalls);
        Assert.Equal([1, 2], other.Values);
        Assert.Equal(1, subject.ObserverCount);
    }

    [Fact]
    public void NotifyObservers_ObserverSubscribesNewObserverDuringNotify_NewObserverStartsWithNextValue()
    {
        var subject = new TestSubject<int>();
        var late = new RecordingObserver<int>();
        bool added = false;
        subject.Subscribe(new ActionObserver<int>("Anmelder", _ =>
        {
            if (!added)
            {
                added = true;
                subject.Subscribe(late);
            }
        }));

        subject.Publish(1);
        subject.Publish(2);

        // Der neue Observer war beim ersten NotifyObservers noch nicht in der Momentaufnahme.
        Assert.Equal([2], late.Values);
    }

    [Fact]
    public void NotifyObservers_FaultyObserver_OthersStillNotifiedAndObserverFailedRaised()
    {
        var subject = new TestSubject<int>();
        var errors = new List<ObserverError>();
        subject.ObserverFailed += errors.Add;
        var before = new RecordingObserver<int>();
        var after = new RecordingObserver<int>();
        subject.Subscribe(before);
        subject.Subscribe(new ActionObserver<int>("Kaputt", _ => throw new InvalidOperationException("Absicht")));
        subject.Subscribe(after);

        subject.Publish(7);

        Assert.Equal([7], before.Values);
        Assert.Equal([7], after.Values);
        ObserverError error = Assert.Single(errors);
        Assert.Equal("Kaputt", error.ObserverName);
        Assert.IsType<InvalidOperationException>(error.Error);
        Assert.Equal(3, subject.ObserverCount);
    }

    [Fact]
    public void NotifyObservers_FaultyObserverWithoutErrorHandler_DoesNotThrow()
    {
        var subject = new TestSubject<int>();
        subject.Subscribe(new ActionObserver<int>("Kaputt", _ => throw new InvalidOperationException()));

        subject.Publish(1);
    }

    [Fact]
    public void GetObserverNames_ReturnsNamesInSubscribeOrder()
    {
        var subject = new TestSubject<int>();
        subject.Subscribe(new ActionObserver<int>("Bildschirm", _ => { }));
        subject.Subscribe(new RecordingObserver<int>());

        IReadOnlyList<string> names = subject.GetObserverNames();

        Assert.Equal(["Bildschirm", "Recorder"], names);
    }

    [Fact]
    public void NotifyStopped_CallsStationStoppedRemovesObserversAndIgnoresLaterValues()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        subject.Subscribe(observer);
        subject.Publish(1);

        subject.Stop();
        subject.Publish(2);

        Assert.True(subject.IsStopped);
        Assert.Equal(1, observer.StoppedCount);
        Assert.Equal([1], observer.Values);
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void NotifyStopped_CalledTwice_ObserverStoppedOnlyOnce()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        subject.Subscribe(observer);

        subject.Stop();
        subject.Stop();

        Assert.Equal(1, observer.StoppedCount);
    }

    [Fact]
    public void NotifyStopped_ObserverThrowsInStationStopped_OthersStillGetStopped()
    {
        var subject = new TestSubject<int>();
        var errors = new List<ObserverError>();
        subject.ObserverFailed += errors.Add;
        subject.Subscribe(new ActionObserver<int>("Kaputt", _ => { }, () => throw new InvalidOperationException()));
        var other = new RecordingObserver<int>();
        subject.Subscribe(other);

        subject.Stop();

        Assert.Equal(1, other.StoppedCount);
        ObserverError error = Assert.Single(errors);
        Assert.Equal("Kaputt", error.ObserverName);
    }

    [Fact]
    public void Subscribe_AfterStopped_CallsStationStoppedImmediatelyAndDoesNotAdd()
    {
        var subject = new TestSubject<int>();
        subject.Stop();
        var observer = new RecordingObserver<int>();

        subject.Subscribe(observer);
        subject.Unsubscribe(observer);

        Assert.Equal(1, observer.StoppedCount);
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void Subscribe_WithLastValue_NewObserverImmediatelyGetsLastValue()
    {
        var subject = new TestSubject<int>(sendLastValueToNewObservers: true);
        subject.Publish(1);
        subject.Publish(2);
        var observer = new RecordingObserver<int>();

        subject.Subscribe(observer);
        subject.Publish(3);

        Assert.Equal([2, 3], observer.Values);
    }

    [Fact]
    public void Subscribe_WithLastValueButNoValueYet_GetsNothing()
    {
        var subject = new TestSubject<int>(sendLastValueToNewObservers: true);
        var observer = new RecordingObserver<int>();

        subject.Subscribe(observer);

        Assert.Empty(observer.Values);
    }

    [Fact]
    public void Subscribe_WithoutLastValue_NewObserverGetsNoOldValue()
    {
        var subject = new TestSubject<int>();
        subject.Publish(1);
        var observer = new RecordingObserver<int>();

        subject.Subscribe(observer);

        Assert.Empty(observer.Values);
    }

    [Fact]
    public void Subscribe_DuplicateObserver_DoesNotGetLastValueAgain()
    {
        var subject = new TestSubject<int>(sendLastValueToNewObservers: true);
        subject.Publish(1);
        var observer = new RecordingObserver<int>();
        subject.Subscribe(observer);

        subject.Subscribe(observer);

        Assert.Equal([1], observer.Values);
    }

    [Fact]
    public void Subscribe_LastValueObserverThrows_ReportedAsObserverFailedAndStillSubscribed()
    {
        var subject = new TestSubject<int>(sendLastValueToNewObservers: true);
        var errors = new List<ObserverError>();
        subject.ObserverFailed += errors.Add;
        subject.Publish(1);

        subject.Subscribe(new ActionObserver<int>("Kaputt", _ => throw new InvalidOperationException()));

        Assert.Single(errors);
        Assert.Equal(1, subject.ObserverCount);
    }

    [Fact]
    public void ConcurrentSubscribeNotifyUnsubscribe_DoesNotThrowAndEndsWithNoExtraObservers()
    {
        var subject = new TestSubject<int>();
        var permanent = new RecordingObserver<int>();
        subject.Subscribe(permanent);
        int failures = 0;
        subject.ObserverFailed += _ => Interlocked.Increment(ref failures);

        Parallel.For(0, 2000, index =>
        {
            var observer = new ActionObserver<int>("Test " + index, _ => { });
            subject.Subscribe(observer);
            subject.Publish(index);
            subject.Unsubscribe(observer);
            subject.Unsubscribe(observer);
        });

        Assert.Equal(1, subject.ObserverCount);
        Assert.Equal(2000, permanent.Values.Count);
        Assert.Equal(0, failures);
    }

    [Fact]
    public async Task ConcurrentPublishAndSubscribeWithLastValue_EveryObserverSeesValuesInOrderWithoutDuplicates()
    {
        var subject = new TestSubject<int>(sendLastValueToNewObservers: true);
        var observers = new List<RecordingObserver<int>>();

        // Ein Thread meldet 0, 1, 2 ... während gleichzeitig neue Observer dazukommen.
        Task publisher = Task.Run(() =>
        {
            for (int value = 0; value < 5000; value++)
            {
                subject.Publish(value);
            }
        });

        while (!publisher.IsCompleted)
        {
            var observer = new RecordingObserver<int>();
            subject.Subscribe(observer);
            observers.Add(observer);
        }
        await publisher;

        // Der "letzte Wert" darf nie NACH einem neueren Wert ankommen.
        foreach (RecordingObserver<int> observer in observers)
        {
            List<int> values = observer.Values;
            for (int index = 1; index < values.Count; index++)
            {
                Assert.True(values[index] > values[index - 1],
                    $"Falsche Reihenfolge: {values[index - 1]} vor {values[index]}");
            }
        }
    }

    [Fact]
    public async Task ConcurrentPublishAndStop_NoUpdateAfterStationStopped()
    {
        for (int round = 0; round < 50; round++)
        {
            var subject = new TestSubject<int>();
            var observer = new SequenceObserver();
            subject.Subscribe(observer);

            Task publisher = Task.Run(() =>
            {
                for (int value = 0; value < 1000; value++)
                {
                    subject.Publish(value);
                }
            });
            subject.Stop();
            await publisher;

            Assert.Equal("stopped", observer.Events[^1]);
            Assert.Equal(1, observer.Events.Count(e => e == "stopped"));
        }
    }

    [Fact]
    public void NotifyObservers_ObserverStopsSubjectDuringNotify_LaterObserversGetNoUpdateAfterStop()
    {
        var subject = new TestSubject<int>();
        subject.Subscribe(new ActionObserver<int>("Stopper", _ => subject.Stop()));
        var later = new SequenceObserver();
        subject.Subscribe(later);

        subject.Publish(1);

        Assert.Equal(["stopped"], later.Events);
    }

    // Schreibt Update und StationStopped in EINE Liste, damit man die Reihenfolge prüfen kann.
    private sealed class SequenceObserver : IWeatherObserver<int>
    {
        private readonly object _lock = new();
        private readonly List<string> _events = new();

        public string Name => "Reihenfolge";

        public List<string> Events
        {
            get
            {
                lock (_lock)
                {
                    return new List<string>(_events);
                }
            }
        }

        public void Update(int value)
        {
            lock (_lock)
            {
                _events.Add(value.ToString());
            }
        }

        public void StationStopped()
        {
            lock (_lock)
            {
                _events.Add("stopped");
            }
        }
    }
}
