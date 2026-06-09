using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Entities;
using OrdersApi.Interfaces;

namespace OrdersApi.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<int> CountAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<List<User>> GetPagedAsync(int page, int pageSize)
        {
            return await _context.Users
                .AsNoTracking()
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<int> CountActiveAdminsAsync()
        {
            return await _context.Users
                .CountAsync(u => u.Role == "Admin" && u.Status == "Active");
        }

        public async Task<bool> HasAnyUserAsync()
        {
            return await _context.Users.AnyAsync();
        }
    }
}
