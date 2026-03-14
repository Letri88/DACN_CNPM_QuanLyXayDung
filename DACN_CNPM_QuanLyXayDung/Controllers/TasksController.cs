using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;

namespace DACN_CNPM_QuanLyXayDung.Controllers
{
    public class TasksController : Controller
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public TasksController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        // GET: Tasks
        public async Task<IActionResult> Index()
        {
            var heThongQlvongDoiDuAnTaiNguyenContext = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Stage);
            return View(await heThongQlvongDoiDuAnTaiNguyenContext.ToListAsync());
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Stage)
                .FirstOrDefaultAsync(m => m.TaskId == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // GET: Tasks/Create
        public IActionResult Create(int? stageId = null)
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            ViewData["StageId"] = new SelectList(_context.Stages, "StageId", "StageName", stageId);
            ViewBag.ReturnStageId = stageId;
            return View();
        }

        // POST: Tasks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TaskId,ProjectId,StageId,TaskName,Description,StartDate,EndDate,Status")] Models.Task task)
        {
            ModelState.Remove(nameof(task.Project));
            ModelState.Remove(nameof(task.Stage));
            if (ModelState.IsValid)
            {
                _context.Add(task);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Stages", new { id = task.StageId });
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", task.ProjectId);
            ViewData["StageId"] = new SelectList(_context.Stages, "StageId", "StageName", task.StageId);
            return View(task);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", task.ProjectId);
            ViewData["StageId"] = new SelectList(_context.Stages, "StageId", "StageName", task.StageId);
            return View(task);
        }

        // POST: Tasks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TaskId,ProjectId,StageId,TaskName,Description,StartDate,EndDate,Status")] Models.Task task)
        {
            if (id != task.TaskId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(task.Project));
            ModelState.Remove(nameof(task.Stage));
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(task);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(task.TaskId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Stages", new { id = task.StageId });
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", task.ProjectId);
            ViewData["StageId"] = new SelectList(_context.Stages, "StageId", "StageName", task.StageId);
            return View(task);
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Stage)
                .FirstOrDefaultAsync(m => m.TaskId == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
            }

            await _context.SaveChangesAsync();
            if (task != null) {
                return RedirectToAction("Details", "Stages", new { id = task.StageId });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.TaskId == id);
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
