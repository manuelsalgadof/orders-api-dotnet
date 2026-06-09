using OrdersApi.Entities;

namespace OrdersApi.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<int> CountAsync();
        Task<List<User>> GetPagedAsync(int page, int pageSize);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task DeleteAsync(User user);
        Task<bool> EmailExistsAsync(string email);
        Task<int> CountActiveAdminsAsync();
        Task<bool> HasAnyUserAsync();
    }
}
