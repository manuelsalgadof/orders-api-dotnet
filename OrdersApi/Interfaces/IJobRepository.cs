using OrdersApi.Entities;

namespace OrdersApi.Interfaces
{
    public interface IJobRepository
    {
        Task<Job> CreateAsync(Job job);
        Task<Job?> GetByIdAsync(Guid id);
        Task UpdateAsync(Job job);
        Task<int> ProcessOrdersAsync();
    }
}
