using Microsoft.EntityFrameworkCore;
using OrdersApi.DTOs;
using OrdersApi.Entities;
using OrdersApi.Interfaces;
using Microsoft.Data.SqlClient;
using OrdersApi.Exceptions;

namespace OrdersApi.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;

        public OrderService(IOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<OrderResponseDto> CreateAsync(CreateOrderDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new ArgumentException("El pedido debe tener al menos un producto.");

            if (string.IsNullOrWhiteSpace(dto.ExternalReference))
                throw new ArgumentException("La referencia externa es obligatoria.");

            var customerExists = await _repository.CustomerExistsAsync(dto.CustomerId);

            if (!customerExists)
                throw new ArgumentException("El cliente no existe.");

            var total = dto.Items.Sum(x => x.Quantity * x.Price);

            var order = new Order
            {
                CustomerId = dto.CustomerId,
                ExternalReference = dto.ExternalReference,
                Total = total,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                OrderItems = dto.Items.Select(x => new OrderItem
                {
                    Product = x.Product,
                    Quantity = x.Quantity,
                    Price = x.Price
                }).ToList()
            };

            try
            {
                var created = await _repository.CreateAsync(order);

                return new OrderResponseDto
                {
                    Id = created.Id,
                    CustomerId = created.CustomerId,
                    ExternalReference = created.ExternalReference,
                    Total = created.Total,
                    Status = created.Status,
                    CreatedAt = created.CreatedAt
                };
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx &&
                                              (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                throw new DuplicateOrderException("Ya existe un pedido con la misma referencia externa.");
            }
        }

        public async Task<PagedResultDto<OrderListItemDto>> GetPagedAsync(int page, int pageSize)
        {
            if (page <= 0)
                page = 1;

            if (pageSize <= 0)
                pageSize = 10;

            if (pageSize > 100)
                pageSize = 100;

            var totalRecords = await _repository.CountAsync();
            var orders = await _repository.GetPagedAsync(page, pageSize);

            var items = orders.Select(x => new OrderListItemDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer?.Name ?? string.Empty,
                ExternalReference = x.ExternalReference,
                Total = x.Total,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            }).ToList();

            return new PagedResultDto<OrderListItemDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Items = items
            };
        }
    }
}