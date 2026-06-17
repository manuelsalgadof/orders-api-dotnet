using Microsoft.Extensions.Logging;
using Moq;
using OrdersApi.DTOs;
using OrdersApi.Entities;
using OrdersApi.Interfaces;
using OrdersApi.Services;

namespace OrdersApi.Tests.Services
{
    public class ValidateRoleTests
    {
        private readonly Mock<IUserRepository>        _repositoryMock = new();
        private readonly Mock<IPasswordHasherService> _hasherMock     = new();
        private readonly Mock<ILogger<UserService>>   _loggerMock     = new();

        private UserService CreateService() =>
            new(_repositoryMock.Object, _hasherMock.Object, _loggerMock.Object);

        // Setup mínimo para que Hash no explote en los tests que llegan a CreateAsync
        private void SetupHasher() =>
            _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");

        // Setup para tests que llegan al repositorio (roles válidos)
        private void SetupRepository() =>
            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => { u.Id = 1; return u; });

        private static CreateUserDto BuildDto(string? role) => new()
        {
            Name     = "Test User",
            Email    = "test@test.com",
            Password = "Password1!",
            Role     = role ?? string.Empty   // DTO no acepta null — simulamos vacío para el caso whitespace
        };

        // ─── Roles válidos ─────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_RoleAdmin_Accepted()
        {
            SetupHasher();
            SetupRepository();

            var dto    = BuildDto("Admin");
            var result = await CreateService().CreateAsync(dto);

            Assert.Equal("Admin", result.Role);
        }

        [Fact]
        public async Task CreateAsync_RoleOperator_Accepted()
        {
            SetupHasher();
            SetupRepository();

            var dto    = BuildDto("Operator");
            var result = await CreateService().CreateAsync(dto);

            Assert.Equal("Operator", result.Role);
        }

        [Fact]
        public async Task CreateAsync_RoleViewer_Accepted()
        {
            SetupHasher();
            SetupRepository();

            var dto    = BuildDto("Viewer");
            var result = await CreateService().CreateAsync(dto);

            Assert.Equal("Viewer", result.Role);
        }

        [Fact]
        public async Task CreateAsync_RoleCaseInsensitive_LowercaseAdmin_Accepted()
        {
            SetupHasher();
            SetupRepository();

            // ValidateRole usa StringComparer.OrdinalIgnoreCase — "admin" debe ser aceptado
            var dto = new CreateUserDto
            {
                Name     = "Test User",
                Email    = "test@test.com",
                Password = "Password1!",
                Role     = "admin"
            };

            var result = await CreateService().CreateAsync(dto);

            // El valor retornado es el valor original que pasó la validación
            Assert.Equal("admin", result.Role);
        }

        // ─── Rol vacío/whitespace → retorna "Viewer" (mínimo privilegio) ────────

        [Fact]
        public async Task CreateAsync_RoleEmpty_DefaultsToViewer()
        {
            SetupHasher();
            SetupRepository();

            var dto = new CreateUserDto
            {
                Name     = "Test User",
                Email    = "test@test.com",
                Password = "Password1!",
                Role     = ""
            };

            var result = await CreateService().CreateAsync(dto);

            Assert.Equal("Viewer", result.Role);
        }

        [Fact]
        public async Task CreateAsync_RoleWhitespace_DefaultsToViewer()
        {
            SetupHasher();
            SetupRepository();

            var dto = new CreateUserDto
            {
                Name     = "Test User",
                Email    = "test@test.com",
                Password = "Password1!",
                Role     = "   "
            };

            var result = await CreateService().CreateAsync(dto);

            Assert.Equal("Viewer", result.Role);
        }

        // ─── Roles inválidos → ArgumentException ──────────────────────────────

        [Fact]
        public async Task CreateAsync_RoleInvalid_SuperAdmin_ThrowsArgumentException()
        {
            SetupHasher();

            var dto = new CreateUserDto
            {
                Name     = "Test User",
                Email    = "test@test.com",
                Password = "Password1!",
                Role     = "SuperAdmin"
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => CreateService().CreateAsync(dto)
            );

            Assert.Contains("Rol inválido", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_RoleInvalid_ArbitraryValue_ThrowsArgumentException()
        {
            SetupHasher();

            var dto = new CreateUserDto
            {
                Name     = "Test User",
                Email    = "test@test.com",
                Password = "Password1!",
                Role     = "Manager"
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => CreateService().CreateAsync(dto)
            );

            Assert.Contains("Rol inválido", ex.Message);
            Assert.Contains("Admin", ex.Message);
            Assert.Contains("Operator", ex.Message);
            Assert.Contains("Viewer", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_RoleInvalid_DoesNotCallRepository()
        {
            SetupHasher();

            var dto = new CreateUserDto
            {
                Name     = "Test User",
                Email    = "test@test.com",
                Password = "Password1!",
                Role     = "SuperAdmin"
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => CreateService().CreateAsync(dto)
            );

            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
