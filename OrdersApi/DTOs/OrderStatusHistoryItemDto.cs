namespace OrdersApi.DTOs
{
    public class OrderStatusHistoryItemDto
    {
        public string? FromStatus { get; set; }
        public string ToStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? ChangedBy { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
