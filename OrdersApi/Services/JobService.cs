using OrdersApi.BackgroundJobs;
using OrdersApi.DTOs;
using OrdersApi.Entities;
using OrdersApi.Interfaces;

namespace OrdersApi.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _repository;
        private readonly IBackgroundJobQueue _queue;

        public JobService(IJobRepository repository, IBackgroundJobQueue queue)
        {
            _repository = repository;
            _queue = queue;
        }

        public async Task<JobResponseDto> ReprocessOrdersAsync()
        {
            var job = new Job
            {
                Id = Guid.NewGuid(),
                Type = "ReprocessOrders",
                Status = "Running",
                CreatedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow,
                Message = "Reproceso iniciado."
            };

            await _repository.CreateAsync(job);

            _queue.Queue(async sp =>
            {
                var repo = sp.GetRequiredService<IJobRepository>();

                try
                {
                    var processedCount = await repo.ProcessOrdersAsync();

                    job.Status = "Completed";
                    job.FinishedAt = DateTime.UtcNow;
                    job.Message = $"Reproceso finalizado. Registros procesados: {processedCount}.";
                }
                catch (Exception ex)
                {
                    job.Status = "Failed";
                    job.FinishedAt = DateTime.UtcNow;
                    job.Message = $"Error: {ex.Message}";
                }

                await repo.UpdateAsync(job);
            });

            return MapToDto(job);
        }

        public async Task<JobResponseDto?> GetByIdAsync(Guid id)
        {
            var job = await _repository.GetByIdAsync(id);
            return job == null ? null : MapToDto(job);
        }

        private static JobResponseDto MapToDto(Job job)
        {
            return new JobResponseDto
            {
                Id = job.Id,
                Type = job.Type,
                Status = job.Status,
                Message = job.Message,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                FinishedAt = job.FinishedAt
            };
        }
    }
}