using DACN_CNPM_QuanLyXayDung.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
            var activeProjectsCount = await _context.Projects
                .CountAsync(p => p.Status == "Active" || p.Status == "In Progress");

            var delayedProjectsCount = await _context.Projects
                .CountAsync(p => p.EndDate < DateOnly.FromDateTime(DateTime.Today)
                && p.Status != "Completed" && p.Status != "Done");

            var totalBudget = await _context.Projects.SumAsync(p => p.Budget ?? 0);

            var totalStaff = await _context.Users
                .CountAsync(u => u.Status == "Active");


            var projectsOverview = await _context.Projects
                .Include(p => p.Manager)
                .Include(p => p.Stages)
                    .ThenInclude(s => s.Tasks)
                .Take(5)
                .ToListAsync();


            var projectsTimeline = await _context.Projects
                .Include(p => p.Stages)
                .Take(3)
                .ToListAsync();


            var materials = await _context.Materials
                .Include(m => m.InventoryTransactions)
                .Include(m => m.MaterialUsages)
                .ToListAsync();

            var lowStockMaterials = materials
                .Where(m =>
                {
                    var totalImport = m.InventoryTransactions.Sum(x => x.Quantity);
                    var totalUsed = m.MaterialUsages.Sum(x => x.QuantityUsage);
                    var currentStock = totalImport - totalUsed;

                    return currentStock <= (m.MinStockLevel ?? 0);
                })
                .Take(3)
                .ToList();


            var model = new DashboardViewModel
            {
                ActiveProjectsCount = activeProjectsCount,
                DelayedProjectsCount = delayedProjectsCount,
                TotalBudget = totalBudget,
                TotalStaff = totalStaff,
                ProjectsOverview = projectsOverview,
                ProjectsTimeline = projectsTimeline,
                LowStockMaterials = lowStockMaterials
            };

            return View(model);
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
