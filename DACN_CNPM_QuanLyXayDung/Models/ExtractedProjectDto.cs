using System;

namespace DACN_CNPM_QuanLyXayDung.Models;

public class ExtractedProjectDto
{
    public string? ProjectName { get; set; }
    public string? Description { get; set; }
    public string? ManagerId { get; set; }
    public decimal? Budget { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
