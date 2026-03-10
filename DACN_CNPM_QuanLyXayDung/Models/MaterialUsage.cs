using System;
using System.Collections.Generic;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class MaterialUsage
{
    public int UsageId { get; set; }

    public int ProjectId { get; set; }

    public int MaterialId { get; set; }

    public int QuantityUsage { get; set; }

    public DateTime? Date { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}
