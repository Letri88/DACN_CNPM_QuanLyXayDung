using System;
using System.Collections.Generic;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class User
{
    public int UserId { get; set; }

    public int? RoleId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string Password { get; set; } = null!;

    public string? Status { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<Stage> Stages { get; set; } = new List<Stage>();
}
