using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using OrdersApi.DTOs;
using OrdersApi.Entities;
using OrdersApi.Interfaces;
using OrdersApi.Services;

namespace OrdersApi.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _repositoryMock;
        private readonly OrderService _service;


        public OrderServiceTests()
        {
            _repositoryMock = new Mock<IOrderRepository>();
            _service = new OrderService(_repositoryMock.Object);
        }

        [Fact]
        public async Task CreateAsync_WhenOrderIsValid_ShouldCreateOrder()
        {
            // Arrange
            var dto = new CreateOrderDto
            {
                CustomerId = 1,
                ExternalReference = "ORDER-TEST-001",
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        Product = "Mouse",
                        Quantity = 2,
                        Price = 10000
                    },
                    new CreateOrderItemDto
                    {
                        Product = "Teclado",
                        Quantity = 1,
                        Price = 25000
                    }
                }
            };

            _repositoryMock
                .Setup(x => x.CustomerExistsAsync(dto.CustomerId))
                .ReturnsAsync(true);

            _repositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order order) =>
                {
                    order.Id = 1;
                    return order;
                });

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(45000, result.Total);
            Assert.Equal("Pending", result.Status);
            Assert.Equal("ORDER-TEST-001", result.ExternalReference);

            _repositoryMock.Verify(x => x.CreateAsync(It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenCustomerDoesNotExist_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new CreateOrderDto
            {
                CustomerId = 99,
                ExternalReference = "ORDER-TEST-002",
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        Product = "Mouse",
                        Quantity = 1,
                        Price = 10000
                    }
                }
            };

            _repositoryMock
                .Setup(x => x.CustomerExistsAsync(dto.CustomerId))
                .ReturnsAsync(false);

            // Act + Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(dto)
            );

            Assert.Equal("El cliente no existe.", exception.Message);

            _repositoryMock.Verify(x => x.CreateAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenItemsAreEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new CreateOrderDto
            {
                CustomerId = 1,
                ExternalReference = "ORDER-TEST-003",
                Items = new List<CreateOrderItemDto>()
            };

            // Act + Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(dto)
            );

            Assert.Equal("El pedido debe tener al menos un producto.", exception.Message);

            _repositoryMock.Verify(x => x.CreateAsync(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenExternalReferenceIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new CreateOrderDto
            {
                CustomerId = 1,
                ExternalReference = "",
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        Product = "Mouse",
                        Quantity = 1,
                        Price = 10000
                    }
                }
            };

            // Act + Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(dto)
            );

            Assert.Equal("La referencia externa es obligatoria.", exception.Message);

            _repositoryMock.Verify(x => x.CreateAsync(It.IsAny<Order>()), Times.Never);
        }
    }
}