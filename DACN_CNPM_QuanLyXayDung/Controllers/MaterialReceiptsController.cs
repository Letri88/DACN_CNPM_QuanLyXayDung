using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;
using System.Security.Claims;

namespace DACN_CNPM_QuanLyXayDung.Controllers;

public class MaterialReceiptsController : Controller
{
    private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

    public MaterialReceiptsController(HeThongQlvongDoiDuAnTaiNguyenContext context)
    {
        _context = context;
    }

    // GET: MaterialReceipts
    public async Task<IActionResult> Index()
    {
        var receipts = await _context.MaterialReceipts
            .Include(r => r.PurchaseOrder)
            .Include(r => r.Receiver)
            .OrderByDescending(r => r.ReceivedAt)
            .ToListAsync();
        return View(receipts);
    }

    // GET: MaterialReceipts/Create/5 (PO Id)
    public async Task<IActionResult> Create(int? poId)
    {
        if (poId == null) return NotFound();

        var po = await _context.PurchaseOrders
            .Include(p => p.Details)
                .ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(p => p.POId == poId);

        if (po == null) return NotFound();

        var receipt = new MaterialReceipt
        {
            POId = po.POId,
            ReceiptCode = $"PNK-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
            PurchaseOrder = po,
            Details = po.Details.Select(d => new MaterialReceiptDetail
            {
                MaterialId = d.MaterialId,
                Material = d.Material,
                RequestedQuantity = d.Quantity,
                ActualQuantity = d.Quantity // Default to requested
            }).ToList()
        };

        return View(receipt);
    }

    // POST: MaterialReceipts/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaterialReceipt receipt, int[] MaterialId, int[] RequestedQuantity, int[] ActualQuantity, int[] DamagedQuantity, string[] Note)
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? _context.Users.FirstOrDefault()?.UserId ?? "Unknown";
        
        receipt.ReceiverId = userId;
        receipt.ReceivedAt = DateTime.Now;
        receipt.Status = "Completed"; // Once created, it is completed
        if (string.IsNullOrEmpty(receipt.ReceiptCode))
        {
            receipt.ReceiptCode = $"PNK-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        _context.MaterialReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        for(int i = 0; i < MaterialId.Length; i++)
        {
            var detail = new MaterialReceiptDetail
            {
                ReceiptId = receipt.ReceiptId,
                MaterialId = MaterialId[i],
                RequestedQuantity = RequestedQuantity[i],
                ActualQuantity = ActualQuantity[i],
                DamagedQuantity = DamagedQuantity.Length > i ? DamagedQuantity[i] : 0,
                Note = Note.Length > i ? Note[i] : null,
                MaterialReceipt = receipt
            };
            _context.MaterialReceiptDetails.Add(detail);

            // Update Inventory Transaction
            var transaction = new InventoryTransaction
            {
                MaterialId = MaterialId[i],
                Quantity = ActualQuantity[i],
                Type = "Import",
                Date = DateTime.Now,
                WarehouseKeeperId = userId
            };
            _context.InventoryTransactions.Add(transaction);
        }

        // Update PO Status
        if (receipt.POId.HasValue)
        {
            var po = await _context.PurchaseOrders.FindAsync(receipt.POId.Value);
            if (po != null)
            {
                bool isPartial = false;
                for (int i = 0; i < MaterialId.Length; i++)
                {
                    if (ActualQuantity[i] < RequestedQuantity[i])
                    {
                        isPartial = true;
                        break;
                    }
                }
                po.Status = isPartial ? "Partially Received" : "Completed";
            }
        }

        // Audit Log
        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "MaterialReceipt",
            EntityId = receipt.ReceiptId,
            Action = "Received",
            UserId = userId,
            Timestamp = DateTime.Now,
            Details = $"Received material into warehouse. Receipt: {receipt.ReceiptCode}"
        });

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: MaterialReceipts/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var receipt = await _context.MaterialReceipts
            .Include(r => r.PurchaseOrder)
            .Include(r => r.Receiver)
            .Include(r => r.Details)
                .ThenInclude(d => d.Material)
            .FirstOrDefaultAsync(m => m.ReceiptId == id);

        if (receipt == null) return NotFound();

        return View(receipt);
    }
}
