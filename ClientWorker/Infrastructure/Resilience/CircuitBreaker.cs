namespace ClientWorker.Infrastructure.Resilience;
using System.Collections.Concurrent;
public class CircuitBreaker<T> : ICircuitBreaker<T>
{
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private object _lock = new object(); //used to lock access to the CircuitBreaker state
    private readonly int _failureThreshold;     
    private readonly TimeSpan _failureWindow;
    private readonly TimeSpan _openDuration;
    private readonly ConcurrentQueue<DateTime> _failureTimestamps = new();
    private DateTime _lastFailureTime = DateTime.MinValue;
    private int _halfOpenAttempts;
    private readonly ILogger _logger;

    public CircuitBreaker(int failureThreshold, TimeSpan failureWindow, TimeSpan openDuration, ILogger logger)
    {
        _failureThreshold = failureThreshold;
        _failureWindow = failureWindow;
        _openDuration = openDuration;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync(Func<Task<T>> operation)
    {
        CircuitBreakerState current;

        lock (_lock)
        {
            current = _state;

            if (current == CircuitBreakerState.Open)
            {
                if (DateTime.UtcNow > _lastFailureTime + _openDuration)
                {
                    TransitionToHalfOpenLocked();
                    current = CircuitBreakerState.HalfOpen;
                }
                else
                {
                    throw new CircuitOpenException("Circuit is OPEN - failing fast");
                }
            }
        }

        try
        {
            // Await the actual async work here
            T result = await operation().ConfigureAwait(false);
            OnSuccess();
            return result;
        }
        catch (Exception)
        {
            OnFailure();
            throw; // rethrow or handle fallback if you add it later
        }
    }

    private void TransitionToHalfOpenLocked()
    {
        _state = CircuitBreakerState.HalfOpen;
        _halfOpenAttempts = 0;
        _logger?.LogWarning("Circuit transitioned to HALF-OPEN state.");
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            ClearOldFailuresLocked();
            if (_state == CircuitBreakerState.HalfOpen)
            {
                TransitionToClosedLocked();
            }
            while (_failureTimestamps.TryDequeue(out _)) { }
        }
    }

    private void OnFailure()
    {
        lock (_lock)
        {
            DateTime now = DateTime.UtcNow;
            _failureTimestamps.Enqueue(now);
            _lastFailureTime = now;

            ClearOldFailuresLocked();

            if (_failureTimestamps.Count >= _failureThreshold)
            {
                if (_state == CircuitBreakerState.Closed || _state == CircuitBreakerState.HalfOpen)
                {
                    TransitionToOpenLocked();
                }
            }

            if (_state == CircuitBreakerState.HalfOpen)
            {
                TransitionToOpenLocked();
            }
        }
    }

    private void TransitionToOpenLocked()
    {
        _state = CircuitBreakerState.Open;
        _lastFailureTime = DateTime.UtcNow;
        _logger?.LogError($"Circuit OPENED at {DateTime.UtcNow:HH:mm:ss}");
    }

    private void ClearOldFailuresLocked()
    {
        DateTime threshold = DateTime.UtcNow - _failureWindow;
        while (_failureTimestamps.TryPeek(out DateTime oldest) && oldest < threshold)
        {
            _failureTimestamps.TryDequeue(out _);
        }
    }

    private void TransitionToClosedLocked()
    {
        _state = CircuitBreakerState.Closed;
        while (_failureTimestamps.TryDequeue(out _)) { }
        _logger?.LogInformation($"Circuit CLOSED at {DateTime.UtcNow:HH:mm:ss}");
    }

    public CircuitBreakerState CurrentState
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    public class CircuitOpenException : Exception
    {
        public CircuitOpenException(string message) : base(message) { }
    }

}