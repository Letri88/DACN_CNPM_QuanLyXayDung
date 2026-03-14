using System;
using System.Collections.Generic;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class Task
{
    public int TaskId { get; set; }

    public int? ProjectId { get; set; }

    public int StageId { get; set; }

    public string TaskName { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public virtual Project? Project { get; set; }

    public virtual Stage Stage { get; set; } = null!;
}
