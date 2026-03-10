using System;
using System.Collections.Generic;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class InventoryTransaction
{
    public int TransactionId { get; set; }

    public int MaterialId { get; set; }

    public int? ProjectId { get; set; }

    public int? WarehouseKeeperId { get; set; }

    public int Quantity { get; set; }

    public string Type { get; set; } = null!;

    public DateTime? Date { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual Project? Project { get; set; }

    public virtual User? WarehouseKeeper { get; set; }
}
