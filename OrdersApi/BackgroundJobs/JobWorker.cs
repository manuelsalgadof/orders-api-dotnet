using OrdersApi.Interfaces;

namespace OrdersApi.BackgroundJobs
{
    public class JobWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBackgroundJobQueue _queue;

        public JobWorker(IServiceProvider serviceProvider, IBackgroundJobQueue queue)
        {
            _serviceProvider = serviceProvider;
            _queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _queue.DequeueAsync(stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                await workItem(scope.ServiceProvider);
            }
        }
    }
}