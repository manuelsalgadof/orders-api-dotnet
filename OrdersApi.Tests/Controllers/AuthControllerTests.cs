using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using OrdersApi.Controllers;
using OrdersApi.DTOs;
using OrdersApi.Entities;
using OrdersApi.Interfaces;

namespace OrdersApi.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IUserService>   _userServiceMock;
        private readonly AuthController       _controller;

        public AuthControllerTests()
        {
            _configMock      = new Mock<IConfiguration>();
            _userServiceMock = new Mock<IUserService>();

            _configMock.Setup(c => c["Jwt:Key"])     .Returns("super-secret-key-for-testing-32chars!");
            _configMock.Setup(c => c["Jwt:Issuer"])  .Returns("TestIssuer");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

            _controller = new AuthController(_configMock.Object, _userServiceMock.Object);
        }

        [Fact]
        public async Task Login_ValidCredentials_Returns200WithToken()
        {
            var user = new User
            {
                Id    = 1,
                Email = "admin@test.com",
                Role  = "Admin"
            };

            _userServiceMock
                .Setup(s => s.ValidateCredentialsAsync("admin@test.com", "Segura123!"))
                .ReturnsAsync(user);

            var result = await _controller.Login(new LoginRequestDto
            {
                Email    = "admin@test.com",
                Password = "Segura123!"
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponseDto>(ok.Value);
            Assert.NotEmpty(response.Token);
        }

        [Fact]
        public async Task Login_InvalidCredentials_Returns401WithGenericMessage()
        {
            _userServiceMock
                .Setup(s => s.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var result = await _controller.Login(new LoginRequestDto
            {
                Email    = "nobody@test.com",
                Password = "WrongPassword1!"
            });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var body = unauthorized.Value!;
            var message = body.GetType().GetProperty("message")?.GetValue(body)?.ToString();
            Assert.Equal("Credenciales inválidas.", message);
        }

        [Fact]
        public async Task Login_UserNotFound_ReturnsSameMessageAsWrongPassword()
        {
            _userServiceMock
                .Setup(s => s.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var resultNotFound = await _controller.Login(new LoginRequestDto
            {
                Email    = "ghost@test.com",
                Password = "SomePassword1!"
            });

            var resultWrongPwd = await _controller.Login(new LoginRequestDto
            {
                Email    = "real@test.com",
                Password = "WrongPwd123!"
            });

            var r1 = Assert.IsType<UnauthorizedObjectResult>(resultNotFound);
            var r2 = Assert.IsType<UnauthorizedObjectResult>(resultWrongPwd);

            var msg1 = r1.Value!.GetType().GetProperty("message")?.GetValue(r1.Value)?.ToString();
            var msg2 = r2.Value!.GetType().GetProperty("message")?.GetValue(r2.Value)?.ToString();

            Assert.Equal(msg1, msg2);
        }
    }
}
