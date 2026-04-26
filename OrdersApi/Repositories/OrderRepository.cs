using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Entities;
using OrdersApi.Interfaces;

namespace OrdersApi.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<bool> CustomerExistsAsync(int customerId)
        {
            return await _context.Customers.AnyAsync(x => x.Id == customerId);
        }

        public async Task<int> CountAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<List<Order>> GetPagedAsync(int page, int pageSize)
        {
            return await _context.Orders
                .Include(x => x.Customer)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}