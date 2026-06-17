using System;
using System.Collections.Generic;

namespace OrdersApi.Entities;

public partial class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public decimal Total { get; set; }

    public string Status { get; set; } = null!;

    public string ExternalReference { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
