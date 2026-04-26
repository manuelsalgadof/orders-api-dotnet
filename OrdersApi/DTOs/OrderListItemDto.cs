namespace OrdersApi.DTOs
{
    public class OrderListItemDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ExternalReference { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
