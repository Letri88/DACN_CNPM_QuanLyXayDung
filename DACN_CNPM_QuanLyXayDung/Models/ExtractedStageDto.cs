using System;

namespace DACN_CNPM_QuanLyXayDung.Models
{
    public class ExtractedStageDto
    {
        public string StageName { get; set; } = null!;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public decimal? Budget { get; set; }
        public string? AssignedUserId { get; set; }
        public int? ProjectId { get; set; }
    }
}
