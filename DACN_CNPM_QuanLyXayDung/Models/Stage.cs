using System;
using System.Collections.Generic;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class Stage
{
    public int StageId { get; set; }

    public int ProjectId { get; set; }

    public string StageName { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
