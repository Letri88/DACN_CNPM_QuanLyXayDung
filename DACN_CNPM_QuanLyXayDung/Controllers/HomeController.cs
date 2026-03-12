using DACN_CNPM_QuanLyXayDung.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace DACN_CNPM_QuanLyXayDung.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public HomeController(ILogger<HomeController> logger, HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var activeProjectsCount = await _context.Projects.CountAsync(p => p.Status == "Active" || p.Status == "In Progress");
            var delayedProjectsCount = await _context.Projects.CountAsync(p => p.EndDate < DateOnly.FromDateTime(DateTime.Today) && p.Status != "Completed" && p.Status != "Done");
            var totalBudget = await _context.Projects.SumAsync(p => p.Budget ?? 0);
            var totalStaff = await _context.Users.CountAsync(u => u.Status == "Active");

            var projectsOverview = await _context.Projects
                .Include(p => p.Manager)
                .Include(p => p.Tasks)
                .Take(5)
                .ToListAsync();

            var projectsTimeline = await _context.Projects
                .Include(p => p.Stages)
                .Take(3)
                .ToListAsync();

            var lowStockMaterials = await _context.Materials
                .Where(m => m.MinStockLevel.HasValue && m.MinStockLevel > 0)
                .Take(3)
                .ToListAsync();
            WeatherSummaryViewModel? weather = null;
            try
            {
                using var client = new HttpClient();
                var url = "https://api.openweathermap.org/data/2.5/weather?q=Da%20Nang,VN&appid=5ceaa34b4ed8c590fdf3b97b04a6ddce&units=metric&lang=vi";
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var main = root.GetProperty("main");
                    var weatherArr = root.GetProperty("weather");
                    var wind = root.GetProperty("wind");

                    var firstWeather = weatherArr[0];
                    var iconCode = firstWeather.GetProperty("icon").GetString() ?? "01d";
                    var mappedIcon = iconCode switch
                    {
                        "01d" => "wb_sunny",
                        "01n" => "clear_night",
                        "02d" => "partly_cloudy_day",
                        "02n" => "partly_cloudy_night",
                        "03d" or "03n" or "04d" or "04n" => "cloud",
                        "09d" or "09n" or "10d" or "10n" => "rainy",
                        "11d" or "11n" => "thunderstorm",
                        "13d" or "13n" => "ac_unit",
                        "50d" or "50n" => "fog",
                        _ => "wb_sunny"
                    };

                    weather = new WeatherSummaryViewModel
                    {
                        City = root.GetProperty("name").GetString() ?? "Đà Nẵng",
                        Temperature = main.GetProperty("temp").GetDouble(),
                        Description = firstWeather.GetProperty("description").GetString() ?? string.Empty,
                        Humidity = main.GetProperty("humidity").GetInt32(),
                        WindSpeed = wind.TryGetProperty("speed", out var s) ? s.GetDouble() : 0,
                        Icon = mappedIcon
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gọi API thời tiết OpenWeather");
            }

            var model = new DashboardViewModel
            {
                ActiveProjectsCount = activeProjectsCount,
                DelayedProjectsCount = delayedProjectsCount,
                TotalBudget = totalBudget,
                TotalStaff = totalStaff,
                ProjectsOverview = projectsOverview,
                ProjectsTimeline = projectsTimeline,
                LowStockMaterials = lowStockMaterials,
                Weather = weather
            };


            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
