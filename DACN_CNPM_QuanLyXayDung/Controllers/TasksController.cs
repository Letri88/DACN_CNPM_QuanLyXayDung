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
            Stage? stage = null;
            if (stageId.HasValue)
            {
                stage = _context.Stages
                    .Include(s => s.Project)
                    .FirstOrDefault(s => s.StageId == stageId.Value);
            }

            if (stage != null)
            {
                // Tạo task gắn chặt với stage này và project tương ứng
                var model = new Models.Task
                {
                    ProjectId = stage.ProjectId,
                    StageId = stage.StageId
                };

                ViewBag.FixedStage = true;
                ViewBag.ProjectName = stage.Project.ProjectName;
                ViewBag.StageName = stage.StageName;
                ViewBag.ReturnStageId = stageId;

                return View(model);
            }

            // Trường hợp tạo task chung (không từ giai đoạn cụ thể)
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            ViewData["StageId"] = new SelectList(Enumerable.Empty<Stage>(), "StageId", "StageName");
            ViewBag.FixedStage = false;
            ViewBag.ReturnStageId = null;
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

            if (task.StartDate.HasValue && task.EndDate.HasValue && task.EndDate < task.StartDate)
            {
                ModelState.AddModelError(nameof(task.EndDate), "Ngày kết thúc công việc không được nhỏ hơn ngày bắt đầu.");
            }

            if (_context.Tasks.Any(t => t.StageId == task.StageId && t.TaskName.Trim().ToLower() == task.TaskName.Trim().ToLower()))
            {
                ModelState.AddModelError(nameof(task.TaskName), "Tên công việc đã tồn tại trong giai đoạn này.");
            }

            if (task.StageId != 0)
            {
                var stage = await _context.Stages.Include(s => s.Project).FirstOrDefaultAsync(s => s.StageId == task.StageId);
                if (stage != null)
                {
                    // Đảm bảo task luôn thuộc đúng dự án của giai đoạn
                    task.ProjectId = stage.ProjectId;

                    if (stage.StartDate.HasValue && task.StartDate.HasValue && task.StartDate < stage.StartDate)
                    {
                        ModelState.AddModelError(nameof(task.StartDate), "Ngày bắt đầu công việc không được nhỏ hơn ngày bắt đầu giai đoạn.");
                    }
                    if (stage.EndDate.HasValue && task.EndDate.HasValue && task.EndDate > stage.EndDate)
                    {
                        ModelState.AddModelError(nameof(task.EndDate), "Ngày kết thúc công việc không được vượt quá ngày kết thúc giai đoạn.");
                    }

                    var project = stage.Project;
                    if (project != null)
                    {
                        if (project.StartDate.HasValue && task.StartDate.HasValue && task.StartDate < project.StartDate)
                        {
                            ModelState.AddModelError(nameof(task.StartDate), "Ngày bắt đầu công việc không được nhỏ hơn ngày bắt đầu dự án.");
                        }
                        if (project.EndDate.HasValue && task.EndDate.HasValue && task.EndDate > project.EndDate)
                        {
                            ModelState.AddModelError(nameof(task.EndDate), "Ngày kết thúc công việc không được vượt quá ngày kết thúc dự án.");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(task);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Stages", new { id = task.StageId });
            }

            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", task.ProjectId);
            ViewData["StageId"] = new SelectList(_context.Stages.Where(s => s.ProjectId == task.ProjectId), "StageId", "StageName", task.StageId);
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

            if (task.StartDate.HasValue && task.EndDate.HasValue && task.EndDate < task.StartDate)
            {
                ModelState.AddModelError(nameof(task.EndDate), "Ngày kết thúc công việc không được nhỏ hơn ngày bắt đầu.");
            }

            if (_context.Tasks.Any(t => t.TaskId != id && t.StageId == task.StageId && t.TaskName.Trim().ToLower() == task.TaskName.Trim().ToLower()))
            {
                ModelState.AddModelError(nameof(task.TaskName), "Tên công việc đã tồn tại trong giai đoạn này.");
            }

            if (task.StageId != 0)
            {
                var stage = await _context.Stages.Include(s => s.Project).FirstOrDefaultAsync(s => s.StageId == task.StageId);
                if (stage != null)
                {
                    if (task.ProjectId != stage.ProjectId)
                    {
                        ModelState.AddModelError(nameof(task.StageId), "Giai đoạn được chọn không thuộc dự án này.");
                    }

                    if (stage.StartDate.HasValue && task.StartDate.HasValue && task.StartDate < stage.StartDate)
                    {
                        ModelState.AddModelError(nameof(task.StartDate), "Ngày bắt đầu công việc không được nhỏ hơn ngày bắt đầu giai đoạn.");
                    }
                    if (stage.EndDate.HasValue && task.EndDate.HasValue && task.EndDate > stage.EndDate)
                    {
                        ModelState.AddModelError(nameof(task.EndDate), "Ngày kết thúc công việc không được vượt quá ngày kết thúc giai đoạn.");
                    }

                    var project = stage.Project;
                    if (project != null)
                    {
                        if (project.StartDate.HasValue && task.StartDate.HasValue && task.StartDate < project.StartDate)
                        {
                            ModelState.AddModelError(nameof(task.StartDate), "Ngày bắt đầu công việc không được nhỏ hơn ngày bắt đầu dự án.");
                        }
                        if (project.EndDate.HasValue && task.EndDate.HasValue && task.EndDate > project.EndDate)
                        {
                            ModelState.AddModelError(nameof(task.EndDate), "Ngày kết thúc công việc không được vượt quá ngày kết thúc dự án.");
                        }
                    }
                }
            }

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
            ViewData["StageId"] = new SelectList(_context.Stages.Where(s => s.ProjectId == task.ProjectId), "StageId", "StageName", task.StageId);
            return View(task);
        }

        [HttpGet]
        public JsonResult GetStagesByProject(int projectId)
        {
            var stages = _context.Stages
                .Where(s => s.ProjectId == projectId)
                .Select(s => new { s.StageId, s.StageName })
                .ToList();

            return Json(stages);
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
