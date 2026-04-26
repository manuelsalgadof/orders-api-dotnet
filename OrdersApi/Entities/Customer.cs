using System;
using System.Collections.Generic;

namespace OrdersApi.Entities;

public partial class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
