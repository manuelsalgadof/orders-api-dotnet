using OrdersApi.Entities;

namespace OrdersApi.Interfaces
{
        public interface IOrderRepository
        {
            Task<Order> CreateAsync(Order order);
            Task<bool> CustomerExistsAsync(int customerId);

            Task<int> CountAsync();
            Task<List<Order>> GetPagedAsync(int page, int pageSize);
            Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
            Task<List<Order>> GetAllAsync(int maxRecords, CancellationToken cancellationToken = default);
    }
    
}
