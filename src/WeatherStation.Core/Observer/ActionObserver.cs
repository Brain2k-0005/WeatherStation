namespace WeatherStation.Core.Observer;

// Observer aus Lambdas – spart für jede Kleinigkeit eine eigene Klasse.
public sealed class ActionObserver<T> : IObserver<T>, INamedObserver
{
    private readonly Action<T> _onNext;
    private readonly Action<Exception>? _onError;
    private readonly Action? _onCompleted;

    public ActionObserver(string name, Action<T> onNext,
                          Action<Exception>? onError = null, Action? onCompleted = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(onNext);
        Name = name;
        _onNext = onNext;
        _onError = onError;
        _onCompleted = onCompleted;
    }

    public string Name { get; }

    public void OnNext(T value) => _onNext(value);

    public void OnError(Exception error) => _onError?.Invoke(error);

    public void OnCompleted() => _onCompleted?.Invoke();
}
