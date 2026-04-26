namespace OrdersApi.BackgroundJobs
{
    public interface IBackgroundJobQueue
    {
        void Queue(Func<IServiceProvider, Task> workItem);
        Task<Func<IServiceProvider, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}
