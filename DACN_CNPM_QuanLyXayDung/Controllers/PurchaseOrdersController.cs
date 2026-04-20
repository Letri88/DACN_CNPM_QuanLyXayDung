using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;
using System.Security.Claims;

namespace DACN_CNPM_QuanLyXayDung.Controllers;

public class PurchaseOrdersController : Controller
{
    private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

    public PurchaseOrdersController(HeThongQlvongDoiDuAnTaiNguyenContext context)
    {
        _context = context;
    }

    // GET: PurchaseOrders
    public async Task<IActionResult> Index()
    {
        var pos = await _context.PurchaseOrders
            .Include(p => p.Request)
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(pos);
    }

    // GET: PurchaseOrders/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var po = await _context.PurchaseOrders
            .Include(p => p.Request)
            .Include(p => p.Supplier)
            .Include(p => p.Details)
                .ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(m => m.POId == id);

        if (po == null) return NotFound();

        return View(po);
    }

    // POST: PurchaseOrders/UpdatePrices
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrices(int POId, int[] DetailIds, decimal[] UnitPrices)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.POId == POId);

        if (po == null) return NotFound();

        decimal total = 0;
        for (int i = 0; i < DetailIds.Length; i++)
        {
            var detail = po.Details.FirstOrDefault(d => d.Id == DetailIds[i]);
            if (detail != null)
            {
                detail.UnitPrice = UnitPrices[i];
                detail.Total = detail.Quantity * detail.UnitPrice;
                total += detail.Total;
            }
        }

        po.TotalAmount = total;
        po.Status = "Sent"; // Changing status to Sent after pricing

        // Audit Log
        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? _context.Users.FirstOrDefault()?.UserId ?? "Unknown";
        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "PurchaseOrder",
            EntityId = po.POId,
            Action = "Updated & Sent",
            UserId = userId,
            Timestamp = DateTime.Now,
            Details = $"Purchase Order {po.POCode} prices updated and sent to supplier."
        });

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = po.POId });
    }
}
