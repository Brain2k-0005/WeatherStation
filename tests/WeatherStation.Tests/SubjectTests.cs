using WeatherStation.Core.Observer;

namespace WeatherStation.Tests;

public class SubjectTests
{
    [Fact]
    public void Subscribe_NullObserver_ThrowsArgumentNullException()
    {
        var subject = new TestSubject<int>();

        Assert.Throws<ArgumentNullException>(() => subject.Subscribe((IObserver<int>)null!));
    }

    [Fact]
    public void Notify_WithTwoObservers_BothReceiveValuesInOrder()
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
    public void Subscribe_ThenDispose_ObserverGetsNoMoreValues()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        IDisposable token = subject.Subscribe(observer);
        subject.Publish(1);

        token.Dispose();
        subject.Publish(2);

        Assert.Equal([1], observer.Values);
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotRemoveOtherSubscription()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        var other = new RecordingObserver<int>();
        IDisposable token = subject.Subscribe(observer);
        subject.Subscribe(other);

        token.Dispose();
        token.Dispose();
        subject.Publish(5);

        Assert.Equal(1, subject.ObserverCount);
        Assert.Empty(observer.Values);
        Assert.Equal([5], other.Values);
    }

    [Fact]
    public void Subscribe_SameObserverTwice_ReceivesTwiceAndEachTokenRemovesOne()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        IDisposable first = subject.Subscribe(observer);
        IDisposable second = subject.Subscribe(observer);

        subject.Publish(1);
        Assert.Equal([1, 1], observer.Values);

        first.Dispose();
        Assert.Equal(1, subject.ObserverCount);
        subject.Publish(2);
        Assert.Equal([1, 1, 2], observer.Values);

        second.Dispose();
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void Notify_ObserverUnsubscribesItselfDuringNotify_NoExceptionAndOthersStillNotified()
    {
        var subject = new TestSubject<int>();
        IDisposable? token = null;
        int selfCalls = 0;
        token = subject.Subscribe("Selbstabmelder", _ =>
        {
            selfCalls++;
            token!.Dispose();
        });
        var other = new RecordingObserver<int>();
        subject.Subscribe(other);

        subject.Publish(1);
        subject.Publish(2);

        Assert.Equal(1, selfCalls);
        Assert.Equal([1, 2], other.Values);
        Assert.Equal(1, subject.ObserverCount);
    }

    [Fact]
    public void Notify_ObserverSubscribesNewObserverDuringNotify_NewObserverStartsWithNextValue()
    {
        var subject = new TestSubject<int>();
        var late = new RecordingObserver<int>();
        bool added = false;
        subject.Subscribe("Anmelder", _ =>
        {
            if (!added)
            {
                added = true;
                subject.Subscribe(late);
            }
        });

        subject.Publish(1);
        subject.Publish(2);

        // Der neue Observer war beim ersten Notify noch nicht in der Momentaufnahme.
        Assert.Equal([2], late.Values);
    }

    [Fact]
    public void Notify_FaultyObserver_OthersStillNotifiedAndObserverFailedRaised()
    {
        var subject = new TestSubject<int>();
        var errors = new List<ObserverError>();
        subject.ObserverFailed += errors.Add;
        var before = new RecordingObserver<int>();
        var after = new RecordingObserver<int>();
        subject.Subscribe(before);
        subject.Subscribe("Kaputt", _ => throw new InvalidOperationException("Absicht"));
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
    public void Notify_FaultyUnnamedObserver_ReportsTypeName()
    {
        var subject = new TestSubject<int>();
        var errors = new List<ObserverError>();
        subject.ObserverFailed += errors.Add;
        subject.Subscribe(new ThrowingObserver());

        subject.Publish(1);

        Assert.Equal(nameof(ThrowingObserver), Assert.Single(errors).ObserverName);
    }

    [Fact]
    public void Notify_FaultyObserverWithoutErrorHandler_DoesNotThrow()
    {
        var subject = new TestSubject<int>();
        subject.Subscribe("Kaputt", _ => throw new InvalidOperationException());

        subject.Publish(1);
    }

    [Fact]
    public void GetObserverNames_ReturnsNamesOrTypeNames()
    {
        var subject = new TestSubject<int>();
        subject.Subscribe("Bildschirm", _ => { });
        subject.Subscribe(new RecordingObserver<int>());

        IReadOnlyList<string> names = subject.GetObserverNames();

        Assert.Equal(["Bildschirm", "RecordingObserver`1"], names);
    }

    [Fact]
    public void NotifyCompleted_CallsOnCompletedRemovesObserversAndIgnoresLaterValues()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        subject.Subscribe(observer);
        subject.Publish(1);

        subject.Complete();
        subject.Publish(2);

        Assert.True(subject.IsCompleted);
        Assert.Equal(1, observer.CompletedCount);
        Assert.Equal([1], observer.Values);
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void NotifyCompleted_CalledTwice_ObserverCompletedOnlyOnce()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        subject.Subscribe(observer);

        subject.Complete();
        subject.Complete();

        Assert.Equal(1, observer.CompletedCount);
    }

    [Fact]
    public void NotifyError_CallsOnErrorAndCompletesSubject()
    {
        var subject = new TestSubject<int>();
        var observer = new RecordingObserver<int>();
        subject.Subscribe(observer);
        var error = new InvalidOperationException("Fehler");

        subject.Fail(error);
        subject.Publish(1);

        Assert.Same(error, Assert.Single(observer.Errors));
        Assert.Equal(0, observer.CompletedCount);
        Assert.Empty(observer.Values);
        Assert.True(subject.IsCompleted);
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void NotifyError_ObserverThrowsInOnError_OthersStillGetError()
    {
        var subject = new TestSubject<int>();
        var errors = new List<ObserverError>();
        subject.ObserverFailed += errors.Add;
        subject.Subscribe(new ActionObserver<int>("Kaputt", _ => { }, _ => throw new InvalidOperationException()));
        var other = new RecordingObserver<int>();
        subject.Subscribe(other);

        subject.Fail(new Exception("x"));

        Assert.Single(other.Errors);
        Assert.Single(errors);
    }

    [Fact]
    public void Subscribe_AfterCompleted_CallsOnCompletedImmediatelyAndReturnsHarmlessToken()
    {
        var subject = new TestSubject<int>();
        subject.Complete();
        var observer = new RecordingObserver<int>();

        IDisposable token = subject.Subscribe(observer);
        token.Dispose();
        token.Dispose();

        Assert.Equal(1, observer.CompletedCount);
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void Subscribe_WithReplay_NewObserverImmediatelyGetsLastValue()
    {
        var subject = new TestSubject<int>(replayLastValue: true);
        subject.Publish(1);
        subject.Publish(2);
        var observer = new RecordingObserver<int>();

        subject.Subscribe(observer);
        subject.Publish(3);

        Assert.Equal([2, 3], observer.Values);
    }

    [Fact]
    public void Subscribe_WithReplayButNoValueYet_GetsNothing()
    {
        var subject = new TestSubject<int>(replayLastValue: true);
        var observer = new RecordingObserver<int>();

        subject.Subscribe(observer);

        Assert.Empty(observer.Values);
    }

    [Fact]
    public void Subscribe_WithoutReplay_NewObserverGetsNoOldValue()
    {
        var subject = new TestSubject<int>();
        subject.Publish(1);
        var observer = new RecordingObserver<int>();

        subject.Subscribe(observer);

        Assert.Empty(observer.Values);
    }

    [Fact]
    public void Subscribe_ReplayObserverThrows_ReportedAsObserverFailedAndStillSubscribed()
    {
        var subject = new TestSubject<int>(replayLastValue: true);
        var errors = new List<ObserverError>();
        subject.ObserverFailed += errors.Add;
        subject.Publish(1);

        subject.Subscribe("Kaputt", _ => throw new InvalidOperationException());

        Assert.Single(errors);
        Assert.Equal(1, subject.ObserverCount);
    }

    [Fact]
    public void ConcurrentSubscribeNotifyDispose_DoesNotThrowAndEndsWithNoObservers()
    {
        var subject = new TestSubject<int>();
        var permanent = new RecordingObserver<int>();
        subject.Subscribe(permanent);
        int failures = 0;
        subject.ObserverFailed += _ => Interlocked.Increment(ref failures);

        Parallel.For(0, 2000, index =>
        {
            IDisposable token = subject.Subscribe("Test " + index, _ => { });
            subject.Publish(index);
            token.Dispose();
            token.Dispose();
        });

        Assert.Equal(1, subject.ObserverCount);
        Assert.Equal(2000, permanent.Values.Count);
        Assert.Equal(0, failures);
    }

    [Fact]
    public async Task ConcurrentPublishAndSubscribeWithReplay_EveryObserverSeesValuesInOrderWithoutDuplicates()
    {
        var subject = new TestSubject<int>(replayLastValue: true);
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

        // Der Replay-Wert darf nie NACH einem neueren Wert ankommen.
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
    public async Task ConcurrentPublishAndComplete_NoOnNextAfterOnCompleted()
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
            subject.Complete();
            await publisher;

            Assert.Equal("completed", observer.Events[^1]);
            Assert.Equal(1, observer.Events.Count(e => e == "completed"));
        }
    }

    [Fact]
    public void Notify_ObserverCompletesSubjectDuringNotify_LaterObserversGetNoValueAfterCompleted()
    {
        var subject = new TestSubject<int>();
        subject.Subscribe("Stopper", _ => subject.Complete());
        var later = new SequenceObserver();
        subject.Subscribe(later);

        subject.Publish(1);

        Assert.Equal(["completed"], later.Events);
    }

    // Schreibt OnNext und OnCompleted in EINE Liste, damit man die Reihenfolge prüfen kann.
    private sealed class SequenceObserver : IObserver<int>
    {
        private readonly object _lock = new();
        private readonly List<string> _events = new();

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

        public void OnNext(int value)
        {
            lock (_lock)
            {
                _events.Add(value.ToString());
            }
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
            lock (_lock)
            {
                _events.Add("completed");
            }
        }
    }

    private sealed class ThrowingObserver : IObserver<int>
    {
        public void OnNext(int value) => throw new InvalidOperationException();

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
