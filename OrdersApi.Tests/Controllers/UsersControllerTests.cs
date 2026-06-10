using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersApi.Controllers;
using OrdersApi.Interfaces;
using System.Security.Claims;

namespace OrdersApi.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock = new();

        private UsersController CreateController(string? nameIdentifierClaim)
        {
            var claims = new List<Claim> { new(ClaimTypes.Role, "Admin") };

            if (nameIdentifierClaim is not null)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifierClaim));

            var principal   = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
            var httpContext = new DefaultHttpContext { User = principal };

            var controller = new UsersController(_userServiceMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        [Fact]
        public async Task Delete_WhenMissingNameIdentifier_ReturnsUnauthorized()
        {
            var controller = CreateController(nameIdentifierClaim: null);

            var result = await controller.Delete(1);

            Assert.IsType<UnauthorizedObjectResult>(result);
            _userServiceMock.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenMalformedNameIdentifier_ReturnsUnauthorized()
        {
            var controller = CreateController(nameIdentifierClaim: "not-an-int");

            var result = await controller.Delete(1);

            Assert.IsType<UnauthorizedObjectResult>(result);
            _userServiceMock.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenNameIdentifierIsZero_ReturnsUnauthorized()
        {
            var controller = CreateController(nameIdentifierClaim: "0");

            var result = await controller.Delete(1);

            Assert.IsType<UnauthorizedObjectResult>(result);
            _userServiceMock.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenDeletingSelf_ReturnsConflict()
        {
            _userServiceMock
                .Setup(s => s.DeleteAsync(5, 5))
                .ThrowsAsync(new InvalidOperationException("No se puede eliminar el propio usuario."));

            var controller = CreateController(nameIdentifierClaim: "5");

            var result = await controller.Delete(5);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Delete_WhenValidRequest_ReturnsNoContent()
        {
            _userServiceMock
                .Setup(s => s.DeleteAsync(2, 5))
                .Returns(Task.CompletedTask);

            var controller = CreateController(nameIdentifierClaim: "5");

            var result = await controller.Delete(2);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
