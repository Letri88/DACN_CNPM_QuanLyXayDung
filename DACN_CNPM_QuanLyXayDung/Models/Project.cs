using System;
using System.Collections.Generic;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class Project
{
    public int ProjectId { get; set; }

    public int? ManagerId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? Budget { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual User? Manager { get; set; }

    public virtual ICollection<MaterialUsage> MaterialUsages { get; set; } = new List<MaterialUsage>();

    public virtual ICollection<Stage> Stages { get; set; } = new List<Stage>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
