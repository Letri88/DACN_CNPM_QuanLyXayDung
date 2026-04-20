using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DACN_CNPM_QuanLyXayDung.Controllers;

public class MaterialRequestsController : Controller
{
    private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

    public MaterialRequestsController(HeThongQlvongDoiDuAnTaiNguyenContext context)
    {
        _context = context;
    }

    // GET: MaterialRequests
    public async Task<IActionResult> Index()
    {
        var requests = await _context.MaterialRequests
            .Include(m => m.Creator)
            .Include(m => m.Approver)
            .Include(m => m.Details)
                .ThenInclude(d => d.Material)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
        return View(requests);
    }

    // GET: MaterialRequests/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var request = await _context.MaterialRequests
            .Include(m => m.Creator)
            .Include(m => m.Approver)
            .Include(m => m.Details)
                .ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(m => m.RequestId == id);

        if (request == null) return NotFound();
        return View(request);
    }

    // GET: MaterialRequests/Create
    public IActionResult Create()
    {
        ViewData["MaterialId"] = new SelectList(_context.Materials, "MaterialId", "MaterialName");
        return View();
    }

    // POST: MaterialRequests/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaterialRequest materialRequest, int[] MaterialId, int[] QuantityRequested, string[] Note)
    {
        if (MaterialId.Length == 0)
        {
            ModelState.AddModelError("", "Please select at least one material.");
            ViewData["MaterialId"] = new SelectList(_context.Materials, "MaterialId", "MaterialName");
            return View(materialRequest);
        }

        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId)) userId = _context.Users.FirstOrDefault()?.UserId ?? "Unknown";

        materialRequest.CreatorId = userId;
        materialRequest.RequestCode = $"YCX-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        materialRequest.CreatedAt = DateTime.Now;
        materialRequest.Status = "Pending";

        _context.Add(materialRequest);
        await _context.SaveChangesAsync();

        // Add details
        for (int i = 0; i < MaterialId.Length; i++)
        {
            var detail = new MaterialRequestDetail
            {
                RequestId = materialRequest.RequestId,
                MaterialId = MaterialId[i],
                QuantityRequested = QuantityRequested[i],
                Note = Note.Length > i ? Note[i] : null,
                MaterialRequest = materialRequest
            };
            _context.MaterialRequestDetails.Add(detail);
        }

        // Add Audit Log
        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "MaterialRequest",
            EntityId = materialRequest.RequestId,
            Action = "Created",
            UserId = userId,
            Timestamp = DateTime.Now,
            Details = $"Material Request {materialRequest.RequestCode} created."
        });

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST: MaterialRequests/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string status, string approvalNote)
    {
        var request = await _context.MaterialRequests
            .Include(r => r.Details)
                .ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null || request.Status != "Pending") return NotFound();

        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? _context.Users.FirstOrDefault()?.UserId ?? "Unknown";

        request.Status = status; // "Approved" or "Rejected"
        request.ApproverId = userId;
        request.ApprovedAt = DateTime.Now;
        request.ApprovalNote = approvalNote;

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "MaterialRequest",
            EntityId = request.RequestId,
            Action = status,
            UserId = userId,
            Timestamp = DateTime.Now,
            Details = $"Request {request.RequestCode} {status}. Note: {approvalNote}"
        });

        if (status == "Approved")
        {
            var defaultSupplier = _context.Suppliers.FirstOrDefault();
            if (defaultSupplier == null)
            {
                // Create a default supplier if the table is completely empty to prevent FK constraint error
                defaultSupplier = new Supplier { Name = "Nhà cung cấp mặc định", Phone = "0123456789" };
                _context.Suppliers.Add(defaultSupplier);
                await _context.SaveChangesAsync();
            }

            var fallbackSupplierId = defaultSupplier.SupplierId;

            var groupedDetails = request.Details.GroupBy(d => d.Material?.SupplierId ?? fallbackSupplierId);

            foreach (var group in groupedDetails)
            {
                var po = new PurchaseOrder
                {
                    POCode = $"PO-{DateTime.Now:yyyyMMdd}-{new Random().Next(10000, 99999)}",
                    RequestId = request.RequestId,
                    SupplierId = group.Key,
                    CreatedAt = DateTime.Now,
                    Status = "Draft",
                    TotalAmount = 0
                };
                
                _context.PurchaseOrders.Add(po);
                
                // Add details
                foreach (var detail in group)
                {
                    var unitPrice = detail.Material?.UnitPrice ?? 0;
                    var poDetail = new PurchaseOrderDetail
                    {
                        MaterialId = detail.MaterialId,
                        Quantity = detail.QuantityRequested,
                        UnitPrice = unitPrice,
                        Total = unitPrice * detail.QuantityRequested,
                        PurchaseOrder = po
                    };
                    _context.PurchaseOrderDetails.Add(poDetail);
                }

                po.TotalAmount = group.Sum(d => (d.Material?.UnitPrice ?? 0) * d.QuantityRequested);
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = request.RequestId });
    }
}
