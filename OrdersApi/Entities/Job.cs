using System;
using System.Collections.Generic;

namespace OrdersApi.Entities;

public partial class Job
{
    public Guid Id { get; set; }

    public string Type { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }
}
