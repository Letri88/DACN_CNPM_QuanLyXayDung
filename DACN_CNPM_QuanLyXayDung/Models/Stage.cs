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

    public int? AssignedUserId { get; set; }

    public string? Status { get; set; }

    public decimal? Budget { get; set; }

    public bool BudgetLocked { get; set; } = false;

    public int? PercentComplete => Tasks != null && Tasks.Any() ? (int)Math.Round((double)Tasks.Count(t => t.Status == "Done" || t.Status == "Completed") / Tasks.Count * 100) : 0;

    public virtual User? AssignedUser { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
