namespace OrdersApi.DTOs
{
    public class OrderItemResponseDto
    {
        public int Id { get; set; }
        public string Product { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
