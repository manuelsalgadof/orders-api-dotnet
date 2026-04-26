namespace OrdersApi.DTOs
{
    public class CreateOrderDto
    {
        public int CustomerId { get; set; }
        public string ExternalReference { get; set; } = string.Empty;
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }
}
