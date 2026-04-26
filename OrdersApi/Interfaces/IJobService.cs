using OrdersApi.DTOs;

namespace OrdersApi.Interfaces
{
    public interface IJobService
    {
        Task<JobResponseDto> ReprocessOrdersAsync();
        Task<JobResponseDto?> GetByIdAsync(Guid id);
    }
}
