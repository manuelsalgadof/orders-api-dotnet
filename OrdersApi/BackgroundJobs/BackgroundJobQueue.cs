using System.Collections.Concurrent;

namespace OrdersApi.BackgroundJobs
{
    public class BackgroundJobQueue : IBackgroundJobQueue
    {
        private readonly ConcurrentQueue<Func<IServiceProvider, Task>> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);

        public void Queue(Func<IServiceProvider, Task> workItem)
        {
            _queue.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<Func<IServiceProvider, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _queue.TryDequeue(out var workItem);
            return workItem!;
        }
    }
}