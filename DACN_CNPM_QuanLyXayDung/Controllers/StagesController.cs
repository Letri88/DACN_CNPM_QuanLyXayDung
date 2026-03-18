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
    public class StagesController : Controller
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public StagesController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        // GET: Stages
        public async Task<IActionResult> Index()
        {
            var heThongQlvongDoiDuAnTaiNguyenContext = _context.Stages
                .Include(s => s.Project)
                .Include(s => s.AssignedUser)
                .Include(s => s.Tasks);
            return View(await heThongQlvongDoiDuAnTaiNguyenContext.ToListAsync());
        }

        // GET: Stages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stage = await _context.Stages
                .Include(s => s.Project)
                .Include(s => s.AssignedUser)
                    .ThenInclude(u => u.Role)
                .Include(s => s.Tasks)
                .FirstOrDefaultAsync(m => m.StageId == id);
            if (stage == null)
            {
                return NotFound();
            }

            return View(stage);
        }

        // GET: Stages/Create
        public IActionResult Create()
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            ViewData["AssignedUserId"] = GetUsersWithRoles();
            return View();
        }

        // POST: Stages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StageId,ProjectId,StageName,StartDate,EndDate,Status,AssignedUserId")] Stage stage)
        {
            ModelState.Remove(nameof(stage.Project));
            ModelState.Remove(nameof(stage.Tasks));
            ModelState.Remove(nameof(stage.AssignedUser));

            if (stage.StartDate.HasValue && stage.EndDate.HasValue && stage.EndDate < stage.StartDate)
            {
                ModelState.AddModelError(nameof(stage.EndDate), "Ngày kết thúc giai đoạn không được nhỏ hơn ngày bắt đầu.");
            }

            if (_context.Stages.Any(s => s.ProjectId == stage.ProjectId && s.StageName.Trim().ToLower() == stage.StageName.Trim().ToLower()))
            {
                ModelState.AddModelError(nameof(stage.StageName), "Tên giai đoạn đã tồn tại trong dự án này.");
            }

            if (stage.ProjectId != 0)
            {
                var project = await _context.Projects.FindAsync(stage.ProjectId);
                if (project != null)
                {
                    if (project.StartDate.HasValue && stage.StartDate.HasValue && stage.StartDate < project.StartDate)
                    {
                        ModelState.AddModelError(nameof(stage.StartDate), "Ngày bắt đầu giai đoạn không được nhỏ hơn ngày bắt đầu dự án.");
                    }
                    if (project.EndDate.HasValue && stage.EndDate.HasValue && stage.EndDate > project.EndDate)
                    {
                        ModelState.AddModelError(nameof(stage.EndDate), "Ngày kết thúc giai đoạn không được vượt quá ngày kết thúc dự án.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(stage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", stage.ProjectId);
            return View(stage);
        }

        // GET: Stages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stage = await _context.Stages.FindAsync(id);
            if (stage == null)
            {
                return NotFound();
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", stage.ProjectId);
            ViewData["AssignedUserId"] = GetUsersWithRoles(stage.AssignedUserId);
            return View(stage);
        }

        // POST: Stages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StageId,ProjectId,StageName,StartDate,EndDate,Status,AssignedUserId")] Stage stage)
        {
            if (id != stage.StageId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(stage.Project));
            ModelState.Remove(nameof(stage.Tasks));
            ModelState.Remove(nameof(stage.AssignedUser));

            if (stage.StartDate.HasValue && stage.EndDate.HasValue && stage.EndDate < stage.StartDate)
            {
                ModelState.AddModelError(nameof(stage.EndDate), "Ngày kết thúc giai đoạn không được nhỏ hơn ngày bắt đầu.");
            }

            if (_context.Stages.Any(s => s.StageId != id && s.ProjectId == stage.ProjectId && s.StageName.Trim().ToLower() == stage.StageName.Trim().ToLower()))
            {
                ModelState.AddModelError(nameof(stage.StageName), "Tên giai đoạn đã tồn tại trong dự án này.");
            }

            if (stage.ProjectId != 0)
            {
                var project = await _context.Projects.FindAsync(stage.ProjectId);
                if (project != null)
                {
                    if (project.StartDate.HasValue && stage.StartDate.HasValue && stage.StartDate < project.StartDate)
                    {
                        ModelState.AddModelError(nameof(stage.StartDate), "Ngày bắt đầu giai đoạn không được nhỏ hơn ngày bắt đầu dự án.");
                    }
                    if (project.EndDate.HasValue && stage.EndDate.HasValue && stage.EndDate > project.EndDate)
                    {
                        ModelState.AddModelError(nameof(stage.EndDate), "Ngày kết thúc giai đoạn không được vượt quá ngày kết thúc dự án.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(stage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StageExists(stage.StageId))
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
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", stage.ProjectId);
            return View(stage);
        }

        // GET: Stages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stage = await _context.Stages
                .Include(s => s.Project)
                .FirstOrDefaultAsync(m => m.StageId == id);
            if (stage == null)
            {
                return NotFound();
            }

            return View(stage);
        }

        // POST: Stages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stage = await _context.Stages
                .Include(s => s.Tasks)
                .FirstOrDefaultAsync(s => s.StageId == id);

            if (stage != null)
            {
                if (stage.Tasks.Any())
                {
                    _context.Tasks.RemoveRange(stage.Tasks);
                }

                _context.Stages.Remove(stage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StageExists(int id)
        {
            return _context.Stages.Any(e => e.StageId == id);
        }

        private string TranslateRole(string? roleName)
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
