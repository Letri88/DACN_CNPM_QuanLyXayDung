using System;
using System.Collections.Generic;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class Material
{
    public int MaterialId { get; set; }

    public string MaterialName { get; set; } = null!;

    public string? Unit { get; set; }

    public int? MinStockLevel { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<MaterialUsage> MaterialUsages { get; set; } = new List<MaterialUsage>();
    public int TotalImported => InventoryTransactions?.Sum(x => x.Quantity) ?? 0;

    public int TotalUsed => MaterialUsages?.Sum(x => x.QuantityUsage) ?? 0;

    public int CurrentStock => TotalImported - TotalUsed;
}
