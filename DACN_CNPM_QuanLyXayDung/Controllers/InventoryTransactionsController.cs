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
    public class InventoryTransactionsController : Controller
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public InventoryTransactionsController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        // GET: InventoryTransactions
        public async Task<IActionResult> Index()
        {
            var heThongQlvongDoiDuAnTaiNguyenContext = _context.InventoryTransactions
                .Include(i => i.Material)
                .Include(i => i.Project)
                .Include(i => i.WarehouseKeeper)
                    .ThenInclude(u => u.Role);
            return View(await heThongQlvongDoiDuAnTaiNguyenContext.ToListAsync());
        }

        // GET: InventoryTransactions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryTransaction = await _context.InventoryTransactions
                .Include(i => i.Material)
                .Include(i => i.Project)
                .Include(i => i.WarehouseKeeper)
                    .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(m => m.TransactionId == id);
            if (inventoryTransaction == null)
            {
                return NotFound();
            }

            return View(inventoryTransaction);
        }

        // GET: InventoryTransactions/Create
        public IActionResult Create()
        {
            ViewData["MaterialId"] = new SelectList(_context.Materials, "MaterialId", "MaterialName");
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            ViewData["WarehouseKeeperId"] = GetUsersWithRoles();
            return View();
        }

        // POST: InventoryTransactions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TransactionId,MaterialId,ProjectId,WarehouseKeeperId,Quantity,Type,Date")] InventoryTransaction inventoryTransaction)
        {
            ModelState.Remove(nameof(inventoryTransaction.Material));
            ModelState.Remove(nameof(inventoryTransaction.Project));
            ModelState.Remove(nameof(inventoryTransaction.WarehouseKeeper));

            if (ModelState.IsValid)
            {
                _context.Add(inventoryTransaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaterialId"] = new SelectList(_context.Materials, "MaterialId", "MaterialName", inventoryTransaction.MaterialId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", inventoryTransaction.ProjectId);
            ViewData["WarehouseKeeperId"] = GetUsersWithRoles(inventoryTransaction.WarehouseKeeperId);
            return View(inventoryTransaction);
        }

        // GET: InventoryTransactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryTransaction = await _context.InventoryTransactions.FindAsync(id);
            if (inventoryTransaction == null)
            {
                return NotFound();
            }
            ViewData["MaterialId"] = new SelectList(_context.Materials, "MaterialId", "MaterialName", inventoryTransaction.MaterialId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", inventoryTransaction.ProjectId);
            ViewData["WarehouseKeeperId"] = GetUsersWithRoles(inventoryTransaction.WarehouseKeeperId);
            return View(inventoryTransaction);
        }

        // POST: InventoryTransactions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TransactionId,MaterialId,ProjectId,WarehouseKeeperId,Quantity,Type,Date")] InventoryTransaction inventoryTransaction)
        {
            if (id != inventoryTransaction.TransactionId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(inventoryTransaction.Material));
            ModelState.Remove(nameof(inventoryTransaction.Project));
            ModelState.Remove(nameof(inventoryTransaction.WarehouseKeeper));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventoryTransaction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryTransactionExists(inventoryTransaction.TransactionId))
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
            ViewData["MaterialId"] = new SelectList(_context.Materials, "MaterialId", "MaterialName", inventoryTransaction.MaterialId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", inventoryTransaction.ProjectId);
            ViewData["WarehouseKeeperId"] = GetUsersWithRoles(inventoryTransaction.WarehouseKeeperId);
            return View(inventoryTransaction);
        }

        // GET: InventoryTransactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventoryTransaction = await _context.InventoryTransactions
                .Include(i => i.Material)
                .Include(i => i.Project)
                .Include(i => i.WarehouseKeeper)
                .FirstOrDefaultAsync(m => m.TransactionId == id);
            if (inventoryTransaction == null)
            {
                return NotFound();
            }

            return View(inventoryTransaction);
        }

        // POST: InventoryTransactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventoryTransaction = await _context.InventoryTransactions.FindAsync(id);
            if (inventoryTransaction != null)
            {
                _context.InventoryTransactions.Remove(inventoryTransaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InventoryTransactionExists(int id)
        {
            return _context.InventoryTransactions.Any(e => e.TransactionId == id);
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
