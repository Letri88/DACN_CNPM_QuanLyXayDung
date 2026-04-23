using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace DACN_CNPM_QuanLyXayDung.Controllers;

[Authorize]
public class SiteDiariesController : Controller
{
    private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

    public SiteDiariesController(HeThongQlvongDoiDuAnTaiNguyenContext context)
    {
        _context = context;
    }

    // GET: SiteDiaries
    public async Task<IActionResult> Index()
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        var query = _context.SiteDiaries
            .Include(s => s.Project)
            .Include(s => s.Stage)
            .Include(s => s.Engineer)
            .AsQueryable();

        if (User.IsInRole("Engineer") || User.IsInRole("Kỹ sư"))
        {
            query = query.Where(s => s.EngineerId == userId);
        }

        var list = await query.OrderByDescending(s => s.Date).ThenByDescending(s => s.CreatedAt).ToListAsync();
        return View(list);
    }

    // GET: SiteDiaries/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var siteDiary = await _context.SiteDiaries
            .Include(s => s.Project)
            .Include(s => s.Stage)
            .Include(s => s.Engineer)
            .FirstOrDefaultAsync(m => m.DiaryId == id);

        if (siteDiary == null) return NotFound();

        return View(siteDiary);
    }

    // GET: SiteDiaries/Create
    [Authorize(Roles = "Engineer,Kỹ sư,Admin,Quản trị viên")]
    public IActionResult Create()
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        IQueryable<Project> projectQuery = _context.Projects;
        
        if (User.IsInRole("Engineer") || User.IsInRole("Kỹ sư"))
        {
            projectQuery = projectQuery.Where(p => _context.Stages.Any(s => s.ProjectId == p.ProjectId && s.AssignedUserId == userId));
        }

        ViewData["ProjectId"] = new SelectList(projectQuery, "ProjectId", "ProjectName");
        ViewData["StageId"] = new SelectList(Enumerable.Empty<Stage>(), "StageId", "StageName");
        return View(new SiteDiary { Date = DateTime.Today });
    }

    // POST: SiteDiaries/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Engineer,Kỹ sư,Admin,Quản trị viên")]
    public async Task<IActionResult> Create([Bind("ProjectId,StageId,Date,Weather,Temperature,WorkerCount,MachineryCount,WorkCompleted,Issues")] SiteDiary siteDiary, IFormFile? photoFile)
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId)) userId = _context.Users.FirstOrDefault()?.UserId ?? "Unknown";

        siteDiary.EngineerId = userId;
        siteDiary.CreatedAt = DateTime.Now;

        ModelState.Remove(nameof(siteDiary.Engineer));
        ModelState.Remove(nameof(siteDiary.Project));
        ModelState.Remove(nameof(siteDiary.Stage));
        ModelState.Remove(nameof(siteDiary.EngineerId));

        if (ModelState.IsValid)
        {
            if (photoFile != null && photoFile.Length > 0)
            {
                if (photoFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "File ảnh không được vượt quá 5MB.");
                    PopulateDropdowns(siteDiary, userId);
                    return View(siteDiary);
                }

                using (var ms = new MemoryStream())
                {
                    await photoFile.CopyToAsync(ms);
                    siteDiary.PhotoContent = ms.ToArray();
                    siteDiary.PhotoContentType = photoFile.ContentType;
                }
            }

            _context.Add(siteDiary);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Đã lưu nhật ký thi công thành công.";
            return RedirectToAction(nameof(Index));
        }

        PopulateDropdowns(siteDiary, userId);
        return View(siteDiary);
    }

    private void PopulateDropdowns(SiteDiary siteDiary, string userId)
    {
        IQueryable<Project> projectQuery = _context.Projects;
        if (User.IsInRole("Engineer") || User.IsInRole("Kỹ sư"))
        {
            projectQuery = projectQuery.Where(p => _context.Stages.Any(s => s.ProjectId == p.ProjectId && s.AssignedUserId == userId));
        }
        
        ViewData["ProjectId"] = new SelectList(projectQuery, "ProjectId", "ProjectName", siteDiary.ProjectId);
        
        IQueryable<Stage> stageQuery = _context.Stages.Where(s => s.ProjectId == siteDiary.ProjectId);
        if (User.IsInRole("Engineer") || User.IsInRole("Kỹ sư"))
        {
            stageQuery = stageQuery.Where(s => s.AssignedUserId == userId);
        }
        ViewData["StageId"] = new SelectList(stageQuery, "StageId", "StageName", siteDiary.StageId);
    }

    // GET: SiteDiaries/GetPhoto/5
    [HttpGet]
    public async Task<IActionResult> GetPhoto(int id)
    {
        var diary = await _context.SiteDiaries.FindAsync(id);
        if (diary == null || diary.PhotoContent == null) return NotFound();

        return File(diary.PhotoContent, diary.PhotoContentType ?? "image/jpeg");
    }

    // Lấy Stage theo ProjectId cho dropdown
    [HttpGet]
    public JsonResult GetStagesByProject(int projectId)
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        var query = _context.Stages.Where(s => s.ProjectId == projectId);

        if (User.IsInRole("Engineer") || User.IsInRole("Kỹ sư"))
        {
            query = query.Where(s => s.AssignedUserId == userId);
        }

        var stages = query.Select(s => new { s.StageId, s.StageName }).ToList();
        return Json(stages);
    }
}
