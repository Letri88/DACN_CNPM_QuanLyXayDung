using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DACN_CNPM_QuanLyXayDung.Controllers;

[Authorize]
public class ProjectMaterialRequestsController : Controller
{
    private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

    public ProjectMaterialRequestsController(HeThongQlvongDoiDuAnTaiNguyenContext context)
    {
        _context = context;
    }

    // GET: ProjectMaterialRequests
    public async Task<IActionResult> Index()
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? _context.Users.FirstOrDefault()?.UserId ?? "Unknown";

        IQueryable<ProjectMaterialRequest> query = _context.ProjectMaterialRequests
            .Include(p => p.Project)
            .Include(p => p.Requester)
            .Include(p => p.Approver);

        if (User.IsInRole("Engineer") || User.IsInRole("Kỹ sư"))
        {
            query = query.Where(q => q.RequesterId == userId);
        }
        else if (User.IsInRole("Project Manager") || User.IsInRole("Quản lý dự án"))
        {
            // PM sees everything (or only for their projects)
        }
        else if (User.IsInRole("Warehouse Keeper") || User.IsInRole("Thủ kho"))
        {
            // Warehouse keeper primarily cares about Approved/Exported ones
            query = query.Where(q => q.Status == "Approved" || q.Status == "Exported");
        }

        var list = await query.OrderByDescending(q => q.CreatedAt).ToListAsync();
        return View(list);
    }

    // GET: ProjectMaterialRequests/Create
    [Authorize(Roles = "Engineer, Quản trị viên, Admin, Kỹ sư")]
    public IActionResult Create()
    {
        ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName");
        ViewData["MaterialId"] = new SelectList(_context.Materials, "MaterialId", "MaterialName");
        return View();
    }

    // POST: ProjectMaterialRequests/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Engineer, Quản trị viên, Admin, Kỹ sư")]
    public async Task<IActionResult> Create(ProjectMaterialRequest request, int[] MaterialId, int[] QuantityRequested, string[] Note)
    {
        if (MaterialId.Length == 0)
        {
            ModelState.AddModelError("", "Vui lòng chọn ít nhất 1 vật tư.");
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            ViewData["MaterialId"] = new SelectList(_context.Materials, "MaterialId", "MaterialName");
            return View(request);
        }

        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? _context.Users.FirstOrDefault()?.UserId ?? "Unknown";

        request.RequesterId = userId;
        request.RequestCode = $"PRJ-REQ-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        request.CreatedAt = DateTime.Now;
        request.Status = "Pending";

        _context.Add(request);
        await _context.SaveChangesAsync();

        for (int i = 0; i < MaterialId.Length; i++)
        {
            _context.ProjectMaterialRequestDetails.Add(new ProjectMaterialRequestDetail
            {
                RequestId = request.RequestId,
                MaterialId = MaterialId[i],
                QuantityRequested = QuantityRequested[i],
                Note = Note.Length > i ? Note[i] : null
            });
        }

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "ProjectMaterialRequest",
            EntityId = request.RequestId,
            Action = "Created",
            UserId = userId,
            Timestamp = DateTime.Now,
            Details = $"Kỹ sư lập yêu cầu xuất vật tư cho dự án. Mã: {request.RequestCode}"
        });

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: ProjectMaterialRequests/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var request = await _context.ProjectMaterialRequests
            .Include(r => r.Project)
            .Include(r => r.Requester)
            .Include(r => r.Approver)
            .Include(r => r.Details)
                .ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null) return NotFound();

        return View(request);
    }

    // POST: ProjectMaterialRequests/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin, Project Manager, Quản trị viên, Quản lý dự án")]
    public async Task<IActionResult> Approve(int id, string status, string approvalNote)
    {
        var request = await _context.ProjectMaterialRequests.FindAsync(id);
        if (request == null || request.Status != "Pending") return NotFound();

        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? _context.Users.FirstOrDefault()?.UserId ?? "Unknown";

        request.Status = status; // "Approved" or "Rejected"
        request.ApproverId = userId;
        request.ApprovedAt = DateTime.Now;
        request.ApprovalNote = approvalNote;

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "ProjectMaterialRequest",
            EntityId = request.RequestId,
            Action = status,
            UserId = userId,
            Timestamp = DateTime.Now,
            Details = $"Quản lý duyệt phiếu yêu cầu dự án {request.RequestCode}: {status}"
        });

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = request.RequestId });
    }

    // GET: ProjectMaterialRequests/Export/5
    [Authorize(Roles = "Admin, Warehouse Keeper, Thủ kho, Warehouse keeper")]
    public async Task<IActionResult> Export(int? id)
    {
        if (id == null) return NotFound();

        var request = await _context.ProjectMaterialRequests
            .Include(r => r.Project)
            .Include(r => r.Details)
                .ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null || request.Status != "Approved") return NotFound();

        return View(request);
    }

    // POST: ProjectMaterialRequests/Export/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin, Warehouse Keeper, Thủ kho, Warehouse keeper")]
    public async Task<IActionResult> Export(int id, int[] DetailIds, int[] QuantityExported)
    {
        var request = await _context.ProjectMaterialRequests
            .Include(r => r.Details)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null || request.Status != "Approved") return NotFound();

        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? _context.Users.FirstOrDefault()?.UserId ?? "Unknown";

        for (int i = 0; i < DetailIds.Length; i++)
        {
            var detail = request.Details.FirstOrDefault(d => d.Id == DetailIds[i]);
            if (detail != null)
            {
                detail.QuantityExported = QuantityExported[i];

                // 1. Sinh phiếu xuất trong InventoryTransactions
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    MaterialId = detail.MaterialId,
                    ProjectId = request.ProjectId,
                    Quantity = -QuantityExported[i], // Âm vì là xuất kho
                    Type = "Export",
                    Date = DateTime.Now,
                    WarehouseKeeperId = userId
                });

                // 2. Ghi nhận tiêu hao chi phí dự án vào MaterialUsages
                _context.MaterialUsages.Add(new MaterialUsage
                {
                    ProjectId = request.ProjectId,
                    MaterialId = detail.MaterialId,
                    QuantityUsage = QuantityExported[i],
                    Date = DateTime.Now
                });
            }
        }

        request.Status = "Exported";

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "ProjectMaterialRequest",
            EntityId = request.RequestId,
            Action = "Exported",
            UserId = userId,
            Timestamp = DateTime.Now,
            Details = $"Thủ kho đã xuất kho cho phiếu {request.RequestCode}."
        });

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = request.RequestId });
    }
}
