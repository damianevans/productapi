namespace ClientWorker.Infrastructure.Resilience;

public interface ICircuitBreaker<T>
{
    Task<T> ExecuteAsync(Func<Task<T>> operation);
}