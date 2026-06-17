namespace OrdersApi.DTOs
{
    public class OrderDetailDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ExternalReference { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
        public List<OrderStatusHistoryItemDto> StatusHistory { get; set; } = new();
    }
}
