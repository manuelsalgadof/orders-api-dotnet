using OrdersApi.DTOs;

namespace OrdersApi.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateAsync(CreateOrderDto dto);
        Task<PagedResultDto<OrderListItemDto>> GetPagedAsync(int page, int pageSize);
        Task<OrderDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<string> ExportCsvAsync(CancellationToken cancellationToken = default);
    }
}
