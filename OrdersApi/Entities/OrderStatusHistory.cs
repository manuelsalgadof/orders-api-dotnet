namespace OrdersApi.Entities;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    public string Source { get; set; } = "System";

    public virtual Order Order { get; set; } = null!;
}
