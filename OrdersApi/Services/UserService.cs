using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrdersApi.DTOs;
using OrdersApi.Entities;
using OrdersApi.Exceptions;
using OrdersApi.Interfaces;

namespace OrdersApi.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IPasswordHasherService _hasher;

        public UserService(IUserRepository repository, IPasswordHasherService hasher)
        {
            _repository = repository;
            _hasher     = hasher;
        }

        public async Task<UserListItemDto> CreateAsync(CreateUserDto dto)
        {
            var user = new User
            {
                Name         = dto.Name.Trim(),
                Email        = dto.Email.Trim().ToLowerInvariant(),
                PasswordHash = _hasher.Hash(dto.Password),
                Role         = "Admin",
                Status       = "Active",
                CreatedAt    = DateTime.UtcNow
            };

            try
            {
                var created = await _repository.CreateAsync(user);
                return MapToDto(created);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx &&
                                               (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                throw new DuplicateUserException("El email ya está registrado.");
            }
        }

        public async Task<PagedResultDto<UserListItemDto>> GetPagedAsync(int page, int pageSize)
        {
            if (page <= 0)    page     = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var total = await _repository.CountAsync();
            var users = await _repository.GetPagedAsync(page, pageSize);

            return new PagedResultDto<UserListItemDto>
            {
                Page         = page,
                PageSize     = pageSize,
                TotalRecords = total,
                TotalPages   = (int)Math.Ceiling(total / (double)pageSize),
                Items        = users.Select(MapToDto).ToList()
            };
        }

        public async Task<UserListItemDto?> GetByIdAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);
            return user is null ? null : MapToDto(user);
        }

        public async Task<UserListItemDto> UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _repository.GetByIdAsync(id)
                ?? throw new ArgumentException("Usuario no encontrado.");

            // AsNoTracking was used — re-attach for update
            if (dto.Name is not null)
                user.Name = dto.Name.Trim();

            if (dto.Email is not null)
            {
                var normalized = dto.Email.Trim().ToLowerInvariant();
                if (normalized != user.Email && await _repository.EmailExistsAsync(normalized))
                    throw new DuplicateUserException("El email ya está registrado.");
                user.Email = normalized;
            }

            if (dto.Status is not null)
            {
                var allowed = new[] { "Active", "Inactive" };
                if (!allowed.Contains(dto.Status))
                    throw new ArgumentException("Estado inválido. Valores permitidos: Active, Inactive.");
                user.Status = dto.Status;
            }

            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                var updated = await _repository.UpdateAsync(user);
                return MapToDto(updated);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx &&
                                               (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                throw new DuplicateUserException("El email ya está registrado.");
            }
        }

        public async Task DeleteAsync(int id, int requestingUserId)
        {
            var user = await _repository.GetByIdAsync(id)
                ?? throw new ArgumentException("Usuario no encontrado.");

            if (user.Role == "Admin" && user.Status == "Active")
            {
                var activeAdminCount = await _repository.CountActiveAdminsAsync();
                if (activeAdminCount <= 1)
                    throw new InvalidOperationException("No se puede eliminar el único administrador activo del sistema.");
            }

            await _repository.DeleteAsync(user);
        }

        public async Task<User?> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _repository.GetByEmailAsync(email.Trim().ToLowerInvariant());

            if (user is null || user.Status != "Active")
                return null;

            return _hasher.Verify(password, user.PasswordHash) ? user : null;
        }

        public async Task SeedAdminIfNoneExistsAsync(IConfiguration configuration)
        {
            if (await _repository.HasAnyUserAsync())
                return;

            var name     = configuration["AdminSeed:Name"];
            var email    = configuration["AdminSeed:Email"];
            var password = configuration["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(name)  ||
                string.IsNullOrWhiteSpace(email)  ||
                string.IsNullOrWhiteSpace(password))
                return;

            var admin = new User
            {
                Name         = name.Trim(),
                Email        = email.Trim().ToLowerInvariant(),
                PasswordHash = _hasher.Hash(password),
                Role         = "Admin",
                Status       = "Active",
                CreatedAt    = DateTime.UtcNow
            };

            await _repository.CreateAsync(admin);
        }

        private static UserListItemDto MapToDto(User user) => new()
        {
            Id        = user.Id,
            Name      = user.Name,
            Email     = user.Email,
            Role      = user.Role,
            Status    = user.Status,
            CreatedAt = user.CreatedAt
        };
    }
}
