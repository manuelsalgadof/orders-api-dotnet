using Microsoft.Extensions.Configuration;
using OrdersApi.DTOs;
using OrdersApi.Entities;

namespace OrdersApi.Interfaces
{
    public interface IUserService
    {
        Task<UserListItemDto> CreateAsync(CreateUserDto dto);
        Task<PagedResultDto<UserListItemDto>> GetPagedAsync(int page, int pageSize);
        Task<UserListItemDto?> GetByIdAsync(int id);
        Task<UserListItemDto> UpdateAsync(int id, UpdateUserDto dto);
        Task DeleteAsync(int id, int requestingUserId);
        Task<User?> ValidateCredentialsAsync(string email, string password);
        Task SeedAdminIfNoneExistsAsync(IConfiguration configuration, bool isProduction);
    }
}
