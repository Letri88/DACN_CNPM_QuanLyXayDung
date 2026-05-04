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
    public class EquipmentDispatchesController : Controller
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public EquipmentDispatchesController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        // GET: EquipmentDispatches
        public async Task<IActionResult> Index()
        {
            var context = _context.EquipmentDispatches.Include(e => e.Equipment).Include(e => e.Project).Include(e => e.Stage);
            return View(await context.ToListAsync());
        }

        // GET: EquipmentDispatches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipmentDispatch = await _context.EquipmentDispatches
                .Include(e => e.Equipment)
                .Include(e => e.Project)
                .Include(e => e.Stage)
                .FirstOrDefaultAsync(m => m.DispatchId == id);
            if (equipmentDispatch == null)
            {
                return NotFound();
            }

            return View(equipmentDispatch);
        }

        // GET: EquipmentDispatches/Create
        public IActionResult Create()
        {
            ViewData["EquipmentId"] = new SelectList(_context.Equipments.Where(e => e.Status == "Sẵn sàng"), "EquipmentId", "EquipmentName");
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            ViewData["StageId"] = new SelectList(new List<Stage>(), "StageId", "StageName");
            return View();
        }

        // POST: EquipmentDispatches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DispatchId,EquipmentId,ProjectId,StageId,StartDate,EndDate,Status,Notes")] EquipmentDispatch equipmentDispatch)
        {
            if (ModelState.IsValid)
            {
                equipmentDispatch.Notes ??= "";
                _context.Add(equipmentDispatch);
                
                // Update equipment status
                var equipment = await _context.Equipments.FindAsync(equipmentDispatch.EquipmentId);
                if (equipment != null && equipmentDispatch.Status == "Đang điều động")
                {
                    equipment.Status = "Đang hoạt động";
                    _context.Update(equipment);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EquipmentId"] = new SelectList(_context.Equipments.Where(e => e.Status == "Sẵn sàng"), "EquipmentId", "EquipmentName", equipmentDispatch.EquipmentId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", equipmentDispatch.ProjectId);
            var stages = equipmentDispatch.ProjectId.HasValue 
                ? await _context.Stages.Where(s => s.ProjectId == equipmentDispatch.ProjectId).ToListAsync()
                : new List<Stage>();
            ViewData["StageId"] = new SelectList(stages, "StageId", "StageName", equipmentDispatch.StageId);
            return View(equipmentDispatch);
        }

        // GET: EquipmentDispatches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipmentDispatch = await _context.EquipmentDispatches.FindAsync(id);
            if (equipmentDispatch == null)
            {
                return NotFound();
            }
            ViewData["EquipmentId"] = new SelectList(_context.Equipments, "EquipmentId", "EquipmentName", equipmentDispatch.EquipmentId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", equipmentDispatch.ProjectId);
            var stages = equipmentDispatch.ProjectId.HasValue 
                ? await _context.Stages.Where(s => s.ProjectId == equipmentDispatch.ProjectId).ToListAsync()
                : new List<Stage>();
            ViewData["StageId"] = new SelectList(stages, "StageId", "StageName", equipmentDispatch.StageId);
            return View(equipmentDispatch);
        }

        // POST: EquipmentDispatches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DispatchId,EquipmentId,ProjectId,StageId,StartDate,EndDate,Status,Notes")] EquipmentDispatch equipmentDispatch)
        {
            if (id != equipmentDispatch.DispatchId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    equipmentDispatch.Notes ??= "";
                    var originalDispatch = await _context.EquipmentDispatches.AsNoTracking().FirstOrDefaultAsync(e => e.DispatchId == id);
                    _context.Update(equipmentDispatch);

                    // Update equipment status based on dispatch status
                    if (originalDispatch?.Status != equipmentDispatch.Status)
                    {
                        var equipment = await _context.Equipments.FindAsync(equipmentDispatch.EquipmentId);
                        if (equipment != null)
                        {
                            if (equipmentDispatch.Status == "Đã hoàn thành")
                                equipment.Status = "Sẵn sàng";
                            else if (equipmentDispatch.Status == "Đang điều động")
                                equipment.Status = "Đang hoạt động";
                            _context.Update(equipment);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EquipmentDispatchExists(equipmentDispatch.DispatchId))
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
            ViewData["EquipmentId"] = new SelectList(_context.Equipments, "EquipmentId", "EquipmentName", equipmentDispatch.EquipmentId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", equipmentDispatch.ProjectId);
            var stages = equipmentDispatch.ProjectId.HasValue 
                ? await _context.Stages.Where(s => s.ProjectId == equipmentDispatch.ProjectId).ToListAsync()
                : new List<Stage>();
            ViewData["StageId"] = new SelectList(stages, "StageId", "StageName", equipmentDispatch.StageId);
            return View(equipmentDispatch);
        }

        // GET: EquipmentDispatches/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipmentDispatch = await _context.EquipmentDispatches
                .Include(e => e.Equipment)
                .Include(e => e.Project)
                .Include(e => e.Stage)
                .FirstOrDefaultAsync(m => m.DispatchId == id);
            if (equipmentDispatch == null)
            {
                return NotFound();
            }

            return View(equipmentDispatch);
        }

        // POST: EquipmentDispatches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var equipmentDispatch = await _context.EquipmentDispatches.FindAsync(id);
            if (equipmentDispatch != null)
            {
                var equipment = await _context.Equipments.FindAsync(equipmentDispatch.EquipmentId);
                if (equipment != null && equipmentDispatch.Status == "Đang điều động")
                {
                    equipment.Status = "Sẵn sàng";
                    _context.Update(equipment);
                }
                _context.EquipmentDispatches.Remove(equipmentDispatch);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EquipmentDispatchExists(int id)
        {
            return _context.EquipmentDispatches.Any(e => e.DispatchId == id);
        }

        [HttpGet]
        public async Task<IActionResult> GetStagesByProject(int projectId)
        {
            var stages = await _context.Stages
                .Where(s => s.ProjectId == projectId)
                .Select(s => new { value = s.StageId, text = s.StageName })
                .ToListAsync();
            return Json(stages);
        }
    }
}
