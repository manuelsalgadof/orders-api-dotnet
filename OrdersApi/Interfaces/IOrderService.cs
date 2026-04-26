using OrdersApi.DTOs;

namespace OrdersApi.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateAsync(CreateOrderDto dto);
        Task<PagedResultDto<OrderListItemDto>> GetPagedAsync(int page, int pageSize);
    }
}
