using OrdersApi.Entities;

namespace OrdersApi.Interfaces
{
        public interface IOrderRepository
        {
            Task<Order> CreateAsync(Order order);
            Task<bool> CustomerExistsAsync(int customerId);

            Task<int> CountAsync();
            Task<List<Order>> GetPagedAsync(int page, int pageSize);
    }
    
}
