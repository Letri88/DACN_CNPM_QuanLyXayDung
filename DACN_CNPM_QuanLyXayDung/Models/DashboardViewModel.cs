using System;
using System.Collections.Generic;

namespace DACN_CNPM_QuanLyXayDung.Models
{
    public class DashboardViewModel
    {
        public int ActiveProjectsCount { get; set; }
        public int DelayedProjectsCount { get; set; }
        public decimal TotalBudget { get; set; }
        public int TotalStaff { get; set; }

        public IEnumerable<Project> ProjectsOverview { get; set; } = new List<Project>();
        
        // Dùng tạm Project làm Timeline cho đơn giản hoặc có thể dùng Stage
        public IEnumerable<Project> ProjectsTimeline { get; set; } = new List<Project>();
        
        public IEnumerable<Material> LowStockMaterials { get; set; } = new List<Material>();
        public WeatherSummaryViewModel? Weather { get; set; }
    }
    public class WeatherSummaryViewModel
    {
        public string City { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public string Icon { get; set; } = string.Empty;
    }
}
