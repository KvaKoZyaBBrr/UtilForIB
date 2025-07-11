namespace Workers;

public interface IWorker
{
    Task<bool> ProcessAsync(string data, CancellationToken cancellation);
}