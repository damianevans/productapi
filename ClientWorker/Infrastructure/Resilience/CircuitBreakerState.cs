namespace ClientWorker.Infrastructure.Resilience;

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}