using Moq;
using OrdersApi.DTOs;
using OrdersApi.Entities;
using OrdersApi.Exceptions;
using OrdersApi.Interfaces;
using OrdersApi.Services;

namespace OrdersApi.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository>      _repositoryMock;
        private readonly Mock<IPasswordHasherService> _hasherMock;
        private readonly UserService                _service;

        public UserServiceTests()
        {
            _repositoryMock = new Mock<IUserRepository>();
            _hasherMock     = new Mock<IPasswordHasherService>();
            _service        = new UserService(_repositoryMock.Object, _hasherMock.Object);
        }

        // ─── CreateAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ValidData_ReturnsUserDto()
        {
            var dto = new CreateUserDto
            {
                Name     = "Ana García",
                Email    = "ana@test.com",
                Password = "Segura123!"
            };

            _hasherMock
                .Setup(h => h.Hash(dto.Password))
                .Returns("hashed-value");

            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => { u.Id = 1; return u; });

            var result = await _service.CreateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(1,             result.Id);
            Assert.Equal("Ana García",  result.Name);
            Assert.Equal("ana@test.com", result.Email);
            Assert.Equal("Admin",        result.Role);
            Assert.Equal("Active",       result.Status);

            _hasherMock.Verify(h => h.Hash(dto.Password), Times.Once);
            _repositoryMock.Verify(r => r.CreateAsync(It.Is<User>(u =>
                u.Name         == "Ana García"  &&
                u.Email        == "ana@test.com" &&
                u.PasswordHash == "hashed-value" &&
                u.Role         == "Admin"         &&
                u.Status       == "Active"
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NeverExposesPasswordHash()
        {
            _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("secret-hash");
            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => { u.Id = 1; return u; });

            var result = await _service.CreateAsync(new CreateUserDto
            {
                Name     = "Test",
                Email    = "test@test.com",
                Password = "Password1!"
            });

            var resultType = result.GetType();
            var hashProp   = resultType.GetProperty("PasswordHash");
            Assert.Null(hashProp);
        }

        // ─── ValidateCredentialsAsync ─────────────────────────────────────────

        [Fact]
        public async Task ValidateCredentialsAsync_ValidCredentials_ReturnsUser()
        {
            var user = new User
            {
                Id           = 1,
                Email        = "admin@test.com",
                PasswordHash = "stored-hash",
                Role         = "Admin",
                Status       = "Active"
            };

            _repositoryMock
                .Setup(r => r.GetByEmailAsync("admin@test.com"))
                .ReturnsAsync(user);

            _hasherMock
                .Setup(h => h.Verify("password123", "stored-hash"))
                .Returns(true);

            var result = await _service.ValidateCredentialsAsync("admin@test.com", "password123");

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task ValidateCredentialsAsync_UserNotFound_ReturnsNull()
        {
            _repositoryMock
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var result = await _service.ValidateCredentialsAsync("noexiste@test.com", "password123");

            Assert.Null(result);
            _hasherMock.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ValidateCredentialsAsync_WrongPassword_ReturnsNull()
        {
            var user = new User
            {
                Id           = 1,
                Email        = "admin@test.com",
                PasswordHash = "stored-hash",
                Status       = "Active"
            };

            _repositoryMock
                .Setup(r => r.GetByEmailAsync("admin@test.com"))
                .ReturnsAsync(user);

            _hasherMock
                .Setup(h => h.Verify("wrong", "stored-hash"))
                .Returns(false);

            var result = await _service.ValidateCredentialsAsync("admin@test.com", "wrong");

            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateCredentialsAsync_InactiveUser_ReturnsNull()
        {
            var user = new User
            {
                Id           = 1,
                Email        = "admin@test.com",
                PasswordHash = "stored-hash",
                Status       = "Inactive"
            };

            _repositoryMock
                .Setup(r => r.GetByEmailAsync("admin@test.com"))
                .ReturnsAsync(user);

            var result = await _service.ValidateCredentialsAsync("admin@test.com", "cualquier");

            Assert.Null(result);
            _hasherMock.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // ─── DeleteAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_LastActiveAdmin_ThrowsInvalidOperationException()
        {
            var admin = new User { Id = 1, Role = "Admin", Status = "Active" };

            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(admin);
            _repositoryMock.Setup(r => r.CountActiveAdminsAsync()).ReturnsAsync(1);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeleteAsync(1, requestingUserId: 99)
            );

            Assert.Contains("único administrador", ex.Message);
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_AdminWithOtherAdmins_Succeeds()
        {
            var admin = new User { Id = 1, Role = "Admin", Status = "Active" };

            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(admin);
            _repositoryMock.Setup(r => r.CountActiveAdminsAsync()).ReturnsAsync(2);
            _repositoryMock.Setup(r => r.DeleteAsync(admin)).Returns(Task.CompletedTask);

            await _service.DeleteAsync(1, requestingUserId: 99);

            _repositoryMock.Verify(r => r.DeleteAsync(admin), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_UserNotFound_ThrowsArgumentException()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.DeleteAsync(99, requestingUserId: 1)
            );
        }

        // ─── SeedAdminIfNoneExistsAsync ───────────────────────────────────────

        [Fact]
        public async Task SeedAdmin_WhenUsersExist_DoesNothing()
        {
            _repositoryMock.Setup(r => r.HasAnyUserAsync()).ReturnsAsync(true);

            await _service.SeedAdminIfNoneExistsAsync(new Mock<Microsoft.Extensions.Configuration.IConfiguration>().Object);

            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task SeedAdmin_WhenNoUsersAndConfigPresent_CreatesAdmin()
        {
            _repositoryMock.Setup(r => r.HasAnyUserAsync()).ReturnsAsync(false);
            _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                           .ReturnsAsync((User u) => u);
            _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");

            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            configMock.Setup(c => c["AdminSeed:Name"])    .Returns("Admin");
            configMock.Setup(c => c["AdminSeed:Email"])   .Returns("admin@seed.com");
            configMock.Setup(c => c["AdminSeed:Password"]).Returns("Admin123!");

            await _service.SeedAdminIfNoneExistsAsync(configMock.Object);

            _repositoryMock.Verify(r => r.CreateAsync(It.Is<User>(u =>
                u.Email  == "admin@seed.com" &&
                u.Role   == "Admin"          &&
                u.Status == "Active"
            )), Times.Once);
        }
    }
}
