using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;
using Microsoft.AspNetCore.Authorization;

namespace DACN_CNPM_QuanLyXayDung.Controllers
{
    [Authorize(Roles = "Admin, Project Manager, Quản trị viên, Quản lý dự án")]
    public class ProjectsController : Controller
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public ProjectsController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var heThongQlvongDoiDuAnTaiNguyenContext = _context.Projects
                .Include(p => p.Manager)
                    .ThenInclude(u => u.Role)
                .Include(p => p.Stages)
                    .ThenInclude(s => s.Tasks);
            return View(await heThongQlvongDoiDuAnTaiNguyenContext.ToListAsync());
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Manager)
                    .ThenInclude(u => u.Role)
                .Include(p => p.MaterialUsages)
                    .ThenInclude(mu => mu.Material)
                .Include(p => p.Stages)
                    .ThenInclude(s => s.Tasks)
                .FirstOrDefaultAsync(m => m.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            ViewData["ManagerId"] = GetUsersWithRoles();
            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,ManagerId,ProjectName,Description,Budget,StartDate,EndDate,Status")] Project project)
        {
            ModelState.Remove(nameof(project.Manager));
            ModelState.Remove(nameof(project.InventoryTransactions));
            ModelState.Remove(nameof(project.MaterialUsages));
            ModelState.Remove(nameof(project.Stages));
            ModelState.Remove(nameof(project.Tasks));

            if (project.StartDate.HasValue && project.EndDate.HasValue && project.EndDate < project.StartDate)
            {
                ModelState.AddModelError(nameof(project.EndDate), "Ngày kết thúc dự án không được nhỏ hơn ngày bắt đầu.");
            }

            if (_context.Projects.Any(p => p.ProjectName.Trim().ToLower() == project.ProjectName.Trim().ToLower()))
            {
                ModelState.AddModelError(nameof(project.ProjectName), "Tên dự án đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ManagerId"] = GetUsersWithRoles(project.ManagerId);
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            ViewData["ManagerId"] = GetUsersWithRoles(project.ManagerId);
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectId,ManagerId,ProjectName,Description,Budget,StartDate,EndDate,Status")] Project project)
        {
            if (id != project.ProjectId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(project.Manager));
            ModelState.Remove(nameof(project.InventoryTransactions));
            ModelState.Remove(nameof(project.MaterialUsages));
            ModelState.Remove(nameof(project.Stages));
            ModelState.Remove(nameof(project.Tasks));

            if (project.StartDate.HasValue && project.EndDate.HasValue && project.EndDate < project.StartDate)
            {
                ModelState.AddModelError(nameof(project.EndDate), "Ngày kết thúc dự án không được nhỏ hơn ngày bắt đầu.");
            }

            if (_context.Projects.Any(p => p.ProjectId != id && p.ProjectName.Trim().ToLower() == project.ProjectName.Trim().ToLower()))
            {
                ModelState.AddModelError(nameof(project.ProjectName), "Tên dự án đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.ProjectId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ManagerId"] = GetUsersWithRoles(project.ManagerId);
            return View(project);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(m => m.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Stages)
                    .ThenInclude(s => s.Tasks)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project != null)
            {
                // Xóa toàn bộ task thuộc các stage của dự án
                var allTasks = project.Stages.SelectMany(s => s.Tasks).ToList();
                if (allTasks.Any())
                {
                    _context.Tasks.RemoveRange(allTasks);
                }

                // Xóa các stage của dự án
                if (project.Stages.Any())
                {
                    _context.Stages.RemoveRange(project.Stages);
                }

                // (Tuỳ chọn) Nếu bạn muốn, có thể xóa luôn Tasks gắn thẳng với Project mà không qua Stage
                var projectLevelTasks = _context.Tasks.Where(t => t.ProjectId == id).ToList();
                if (projectLevelTasks.Any())
                {
                    _context.Tasks.RemoveRange(projectLevelTasks);
                }

                // Xóa các giao dịch kho liên quan đến dự án
                var inventoryTransactions = _context.InventoryTransactions.Where(it => it.ProjectId == id).ToList();
                if (inventoryTransactions.Any())
                {
                    _context.InventoryTransactions.RemoveRange(inventoryTransactions);
                }
                
                // Xóa các giao dịch sử dụng vật liệu liên quan đến dự án
                var materialUsages = _context.MaterialUsages.Where(mu => mu.ProjectId == id).ToList();
                if (materialUsages.Any())
                {
                    _context.MaterialUsages.RemoveRange(materialUsages);
                }

                // Cuối cùng xóa chính dự án
                _context.Projects.Remove(project);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }

        private string TranslateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return "";
            return roleName.Trim().ToLower() switch {
                "admin" => "Quản trị viên",
                "project manager" => "Quản lý dự án",
                "engineer" => "Kỹ sư",
                "warehouse keeper" => "Thủ kho",
                _ => roleName
            };
        }

        private SelectList GetUsersWithRoles(int? selectedId = null)
        {
            var users = _context.Users.Include(u => u.Role).ToList().Select(u => new {
                UserId = u.UserId,
                DisplayName = $"{u.FullName} - {TranslateRole(u.Role?.RoleName)}"
            });
            return new SelectList(users, "UserId", "DisplayName", selectedId);
        }
    }
}
