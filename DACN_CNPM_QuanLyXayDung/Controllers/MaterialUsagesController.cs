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
    public class MaterialUsagesController : Controller
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public MaterialUsagesController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        // GET: MaterialUsages
        public async Task<IActionResult> Index()
        {
            var heThongQlvongDoiDuAnTaiNguyenContext = _context.MaterialUsages.Include(m => m.Material).Include(m => m.Project);
            return View(await heThongQlvongDoiDuAnTaiNguyenContext.ToListAsync());
        }

        // GET: MaterialUsages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materialUsage = await _context.MaterialUsages
                .Include(m => m.Material)
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.UsageId == id);
            if (materialUsage == null)
            {
                return NotFound();
            }

            return View(materialUsage);
        }

        // Helper method to get materials with available stock
        private async Task<SelectList> GetMaterialsWithStockAsync(int? selectedId = null)
        {
            var materials = await _context.Materials.ToListAsync();
            var materialOptions = new List<object>();

            foreach (var mat in materials)
            {
                var totalImport = await _context.InventoryTransactions
                    .Where(it => it.MaterialId == mat.MaterialId)
                    .SumAsync(it => it.Quantity);
                    
                var totalUsed = await _context.MaterialUsages
                    .Where(mu => mu.MaterialId == mat.MaterialId)
                    .SumAsync(mu => mu.QuantityUsage);

                var availableStock = totalImport - totalUsed;

                materialOptions.Add(new
                {
                    Id = mat.MaterialId,
                    Name = $"{mat.MaterialName} (Còn lại: {availableStock} {mat.Unit})"
                });
            }

            return new SelectList(materialOptions, "Id", "Name", selectedId);
        }

        // GET: MaterialUsages/Create
        public async Task<IActionResult> Create()
        {
            ViewData["MaterialId"] = await GetMaterialsWithStockAsync();
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            return View();
        }

        // POST: MaterialUsages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UsageId,ProjectId,MaterialId,QuantityUsage,Date")] MaterialUsage materialUsage)
        {
            ModelState.Remove(nameof(materialUsage.Material));
            ModelState.Remove(nameof(materialUsage.Project));

            if (ModelState.IsValid)
            {
                // Validate sufficient stock
                var totalImport = await _context.InventoryTransactions
                    .Where(it => it.MaterialId == materialUsage.MaterialId)
                    .SumAsync(it => it.Quantity);
                    
                var totalUsed = await _context.MaterialUsages
                    .Where(mu => mu.MaterialId == materialUsage.MaterialId)
                    .SumAsync(mu => mu.QuantityUsage);

                var availableStock = totalImport - totalUsed;

                if (materialUsage.QuantityUsage > availableStock)
                {
                    ModelState.AddModelError("QuantityUsage", $"Số lượng sử dụng không được vượt quá số lượng tồn kho (Còn lại: {availableStock}).");
                }
                else
                {
                    _context.Add(materialUsage);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewData["MaterialId"] = await GetMaterialsWithStockAsync(materialUsage.MaterialId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", materialUsage.ProjectId);
            return View(materialUsage);
        }

        // GET: MaterialUsages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materialUsage = await _context.MaterialUsages.FindAsync(id);
            if (materialUsage == null)
            {
                return NotFound();
            }
            ViewData["MaterialId"] = await GetMaterialsWithStockAsync(materialUsage.MaterialId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", materialUsage.ProjectId);
            return View(materialUsage);
        }

        // POST: MaterialUsages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UsageId,ProjectId,MaterialId,QuantityUsage,Date")] MaterialUsage materialUsage)
        {
            if (id != materialUsage.UsageId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(materialUsage.Material));
            ModelState.Remove(nameof(materialUsage.Project));

            if (ModelState.IsValid)
            {
                // To properly calculate available stock during edit, we need to know the ORIGINAL quantity used
                // so we don't count it twice against the limit.
                var originalUsage = await _context.MaterialUsages.AsNoTracking().FirstOrDefaultAsync(m => m.UsageId == id);
                int originalQuantity = originalUsage?.QuantityUsage ?? 0;

                var totalImport = await _context.InventoryTransactions
                    .Where(it => it.MaterialId == materialUsage.MaterialId)
                    .SumAsync(it => it.Quantity);
                    
                var totalUsed = await _context.MaterialUsages
                    .Where(mu => mu.MaterialId == materialUsage.MaterialId && mu.UsageId != id)
                    .SumAsync(mu => mu.QuantityUsage);

                var availableStock = totalImport - totalUsed;

                if (materialUsage.QuantityUsage > availableStock)
                {
                     ModelState.AddModelError("QuantityUsage", $"Số lượng sử dụng không được vượt quá số lượng tồn kho (Còn lại: {availableStock}).");
                }
                else
                {
                    try
                    {
                        _context.Update(materialUsage);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!MaterialUsageExists(materialUsage.UsageId))
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
            }
            ViewData["MaterialId"] = await GetMaterialsWithStockAsync(materialUsage.MaterialId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", materialUsage.ProjectId);
            return View(materialUsage);
        }

        // GET: MaterialUsages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materialUsage = await _context.MaterialUsages
                .Include(m => m.Material)
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.UsageId == id);
            if (materialUsage == null)
            {
                return NotFound();
            }

            return View(materialUsage);
        }

        // POST: MaterialUsages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var materialUsage = await _context.MaterialUsages.FindAsync(id);
            if (materialUsage != null)
            {
                _context.MaterialUsages.Remove(materialUsage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MaterialUsageExists(int id)
        {
            return _context.MaterialUsages.Any(e => e.UsageId == id);
        }
    }
}
