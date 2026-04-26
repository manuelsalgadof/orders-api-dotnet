using System;
using System.Collections.Generic;

namespace OrdersApi.Entities;

public partial class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Product { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public virtual Order Order { get; set; } = null!;
}
