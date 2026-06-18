using Microsoft.EntityFrameworkCore;
using OrdersApi.DTOs;
using OrdersApi.Entities;
using OrdersApi.Interfaces;
using Microsoft.Data.SqlClient;
using OrdersApi.Exceptions;
using System.Text;
using System.Globalization;

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

        public async Task<OrderDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _repository.GetByIdAsync(id, cancellationToken);
            if (order is null) return null;

            return new OrderDetailDto
            {
                Id                = order.Id,
                CustomerId        = order.CustomerId,
                CustomerName      = order.Customer?.Name ?? string.Empty,
                ExternalReference = order.ExternalReference,
                Total             = order.Total,
                Status            = order.Status,
                CreatedAt         = order.CreatedAt,
                Items             = order.OrderItems.Select(i => new OrderItemResponseDto
                {
                    Id       = i.Id,
                    Product  = i.Product,
                    Quantity = i.Quantity,
                    Price    = i.Price
                }).ToList(),
                StatusHistory     = order.StatusHistory.Select(h => new OrderStatusHistoryItemDto
                {
                    FromStatus = h.FromStatus,
                    ToStatus   = h.ToStatus,
                    ChangedAt  = h.ChangedAt,
                    ChangedBy  = h.ChangedBy,
                    Source     = h.Source
                }).ToList()
            };
        }

        public async Task<string> ExportCsvAsync(CancellationToken cancellationToken = default)
        {
            const int MaxExportRecords = 5000;
            var orders = await _repository.GetAllAsync(MaxExportRecords, cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine("Id,CustomerId,CustomerName,ExternalReference,Total,Status,CreatedAt");

            foreach (var o in orders)
            {
                sb.AppendLine(string.Join(",",
                    o.Id,
                    o.CustomerId,
                    EscapeCsv(o.Customer?.Name ?? string.Empty),
                    EscapeCsv(o.ExternalReference),
                    o.Total.ToString("F2", CultureInfo.InvariantCulture),
                    EscapeCsv(o.Status),
                    o.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                ));
            }

            return sb.ToString();
        }

        private static string EscapeCsv(string value)
        {
            // CSV formula injection guard — prefix apostrophe if first char is dangerous
            if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@')
                value = "'" + value;

            // RFC 4180 escaping — after sanitization so apostrophe is also quoted if needed
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
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