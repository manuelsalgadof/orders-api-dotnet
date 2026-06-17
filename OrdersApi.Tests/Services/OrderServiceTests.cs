using System;
using System.Collections.Generic;
using System.Globalization;
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

        // ─── GetByIdAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ExistingOrder_ReturnsOrderDetailDto()
        {
            var order = new Order
            {
                Id                = 5,
                CustomerId        = 1,
                ExternalReference = "REF-005",
                Total             = 200m,
                Status            = "Processed",
                CreatedAt         = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                Customer          = new Customer { Id = 1, Name = "Acme" },
                OrderItems        = new List<OrderItem>
                {
                    new() { Id = 1, Product = "Widget", Quantity = 2, Price = 100m }
                },
                StatusHistory = new List<OrderStatusHistory>
                {
                    new()
                    {
                        FromStatus = "Pending",
                        ToStatus   = "Processed",
                        ChangedAt  = new DateTime(2026, 1, 15, 11, 0, 0, DateTimeKind.Utc),
                        ChangedBy  = null,
                        Source     = "Job"
                    }
                }
            };

            _repositoryMock
                .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var result = await _service.GetByIdAsync(5);

            Assert.NotNull(result);
            Assert.Equal(5,           result.Id);
            Assert.Equal(1,           result.CustomerId);
            Assert.Equal("Acme",      result.CustomerName);
            Assert.Equal("REF-005",   result.ExternalReference);
            Assert.Equal(200m,        result.Total);
            Assert.Equal("Processed", result.Status);

            Assert.Single(result.Items);
            Assert.Equal("Widget", result.Items[0].Product);
            Assert.Equal(2,        result.Items[0].Quantity);
            Assert.Equal(100m,     result.Items[0].Price);

            Assert.Single(result.StatusHistory);
            Assert.Equal("Pending",   result.StatusHistory[0].FromStatus);
            Assert.Equal("Processed", result.StatusHistory[0].ToStatus);
            Assert.Equal("Job",       result.StatusHistory[0].Source);
            Assert.Null(result.StatusHistory[0].ChangedBy);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            var result = await _service.GetByIdAsync(99);

            Assert.Null(result);
        }

        // ─── ExportCsvAsync ────────────────────────────────────────────────────

        private static Order BuildOrder(
            int id,
            string customerName,
            string externalRef,
            decimal total,
            string status = "Pending",
            DateTime? createdAt = null) => new()
        {
            Id                = id,
            CustomerId        = 1,
            ExternalReference = externalRef,
            Total             = total,
            Status            = status,
            CreatedAt         = createdAt ?? new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            Customer          = new Customer { Id = 1, Name = customerName },
            OrderItems        = new List<OrderItem>()
        };

        [Fact]
        public async Task ExportCsvAsync_NoOrders_ReturnsOnlyHeader()
        {
            _repositoryMock.Setup(r => r.GetAllAsync(5000, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Order>());

            var csv = await _service.ExportCsvAsync();

            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Single(lines);
            Assert.Equal("Id,CustomerId,CustomerName,ExternalReference,Total,Status,CreatedAt", lines[0].Trim());
        }

        [Fact]
        public async Task ExportCsvAsync_OneOrder_ReturnsHeaderPlusOneLine()
        {
            var createdAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
            var order     = BuildOrder(1, "TestCo", "REF-001", 150.50m, "Pending", createdAt);

            _repositoryMock.Setup(r => r.GetAllAsync(5000, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Order> { order });

            var csv   = await _service.ExportCsvAsync();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(2, lines.Length);
            Assert.Equal(
                "Id,CustomerId,CustomerName,ExternalReference,Total,Status,CreatedAt",
                lines[0].Trim());
            Assert.Equal(
                "1,1,TestCo,REF-001,150.50,Pending,2026-01-15T10:00:00Z",
                lines[1].Trim());
        }

        [Fact]
        public async Task ExportCsvAsync_CustomerNameWithComma_IsQuoted()
        {
            var order = BuildOrder(1, "Acme, Inc.", "REF-002", 100m);

            _repositoryMock.Setup(r => r.GetAllAsync(5000, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Order> { order });

            var csv   = await _service.ExportCsvAsync();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // CustomerName con coma debe estar encerrado en comillas dobles
            Assert.Contains("\"Acme, Inc.\"", lines[1]);
        }

        [Fact]
        public async Task ExportCsvAsync_ExternalRefWithDoubleQuote_IsDoubleEscaped()
        {
            var order = BuildOrder(1, "Normal", "REF\"001", 100m);

            _repositoryMock.Setup(r => r.GetAllAsync(5000, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Order> { order });

            var csv   = await _service.ExportCsvAsync();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // RFC 4180: la comilla doble se escapa duplicando → ""REF""001""
            Assert.Contains("\"REF\"\"001\"", lines[1]);
        }

        [Fact]
        public async Task ExportCsvAsync_FieldWithNewline_IsQuoted()
        {
            var order = BuildOrder(1, "Line1\nLine2", "REF-003", 100m);

            _repositoryMock.Setup(r => r.GetAllAsync(5000, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Order> { order });

            var csv = await _service.ExportCsvAsync();

            // El campo con \n debe aparecer entre comillas en el CSV resultante
            Assert.Contains("\"Line1\nLine2\"", csv);
        }

        [Fact]
        public async Task ExportCsvAsync_FieldWithCarriageReturn_IsQuoted()
        {
            var order = BuildOrder(1, "Line1\rLine2", "REF-004", 100m);

            _repositoryMock.Setup(r => r.GetAllAsync(5000, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Order> { order });

            var csv = await _service.ExportCsvAsync();

            Assert.Contains("\"Line1\rLine2\"", csv);
        }

        [Fact]
        public async Task ExportCsvAsync_TotalUsesInvariantCulture_DotDecimalSeparator()
        {
            // Total con decimales: debe usar punto, no coma
            var order = BuildOrder(1, "TestCo", "REF-005", 1234.56m);

            _repositoryMock.Setup(r => r.GetAllAsync(5000, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Order> { order });

            var csv   = await _service.ExportCsvAsync();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.Contains("1234.56", lines[1]);
            Assert.DoesNotContain("1234,56", lines[1]);
        }

        [Fact]
        public async Task ExportCsvAsync_Header_ExactColumnOrder()
        {
            _repositoryMock.Setup(r => r.GetAllAsync(5000, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Order>());

            var csv   = await _service.ExportCsvAsync();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(
                "Id,CustomerId,CustomerName,ExternalReference,Total,Status,CreatedAt",
                lines[0].Trim());
        }
    }
}