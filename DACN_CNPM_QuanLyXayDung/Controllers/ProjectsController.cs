using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;
using Microsoft.AspNetCore.Authorization;

namespace DACN_CNPM_QuanLyXayDung.Controllers
{
    [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án,Engineer,Kỹ sư")]
    public class ProjectsController : Controller
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public ProjectsController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var heThongQlvongDoiDuAnTaiNguyenContext = _context.Projects
                .Include(p => p.Manager)
                    .ThenInclude(u => u.Role)
                .Include(p => p.Stages)
                    .ThenInclude(s => s.Tasks);
            return View(await heThongQlvongDoiDuAnTaiNguyenContext.ToListAsync());
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Manager)
                    .ThenInclude(u => u.Role)
                .Include(p => p.MaterialUsages)
                    .ThenInclude(mu => mu.Material)
                .Include(p => p.Stages)
                    .ThenInclude(s => s.Tasks)
                .FirstOrDefaultAsync(m => m.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            var usedMaterials = await _context.InventoryTransactions
                .Where(t => t.ProjectId == project.ProjectId && t.Type == "Xuất kho")
                .GroupBy(t => t.Material.MaterialName)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(t => t.Quantity));

            ViewBag.UsedMaterials = usedMaterials;

            return View(project);
        }

        // POST: Projects/ExtractContractBudget
        // Dùng AJAX để trích tổng chi phí từ PDF hợp đồng -> trả về budget cho UI.
        [HttpPost]
        public async Task<IActionResult> ExtractContractBudget(IFormFile? contractFile)
        {
            if (contractFile is null || contractFile.Length == 0)
            {
                return BadRequest(new { message = "Bạn cần chọn file hợp đồng (PDF)." });
            }

            if (!string.Equals(Path.GetExtension(contractFile.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Định dạng file phải là PDF." });
            }

            if (contractFile.Length > 15 * 1024 * 1024)
            {
                return BadRequest(new { message = "File hợp đồng quá lớn. Vui lòng chọn file nhỏ hơn 15MB." });
            }

            var extractedBudget = await ContractBudgetExtractor.TryExtractProjectBudgetAsync(contractFile);
            if (extractedBudget is null)
            {
                return BadRequest(new { message = "Không thể trích xuất tổng chi phí từ hợp đồng." });
            }

            return Ok(new { budget = extractedBudget.Value, budgetLocked = true });
        }

        // GET: Projects/DownloadContract
        // Trả về PDF hợp đồng đã upload để người dùng xem lại.
        [HttpGet]
        public async Task<IActionResult> DownloadContract(int id)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(m => m.ProjectId == id);
            if (project == null || project.ContractFileContent == null || project.ContractFileContent.Length == 0)
            {
                return NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(project.ContractFileContentType)
                ? "application/pdf"
                : project.ContractFileContentType;

            var fileName = string.IsNullOrWhiteSpace(project.ContractFileName)
                ? "contract.pdf"
                : project.ContractFileName;

            return File(project.ContractFileContent, contentType, fileName);
        }

        // GET: Projects/Create
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
        public IActionResult Create()
        {
            var currentUserId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            ViewData["ManagerId"] = GetUsersWithRoles(currentUserId, new[] { "Project Manager", "Quản lý dự án"});
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
        public async Task<IActionResult> Create([Bind("ProjectId,ManagerId,ProjectName,Description,Budget,StartDate,EndDate,Status")] Project project, IFormFile? contractFile)
        {
            ModelState.Remove(nameof(project.Manager));
            ModelState.Remove(nameof(project.InventoryTransactions));
            ModelState.Remove(nameof(project.MaterialUsages));
            ModelState.Remove(nameof(project.Stages));
            ModelState.Remove(nameof(project.Tasks));

            if (project.StartDate.HasValue && project.EndDate.HasValue && project.EndDate < project.StartDate)
            {
                ModelState.AddModelError(nameof(project.EndDate), "Ngày kết thúc dự án không được nhỏ hơn ngày bắt đầu.");
            }

            if (_context.Projects.Any(p => p.ProjectName.Trim().ToLower() == project.ProjectName.Trim().ToLower()))
            {
                ModelState.AddModelError(nameof(project.ProjectName), "Tên dự án đã tồn tại.");
            }

            if (contractFile is not null && contractFile.Length > 0)
            {
                if (contractFile.Length > 15 * 1024 * 1024)
                {
                    ModelState.AddModelError("contractFile", "File hợp đồng quá lớn. Vui lòng chọn file nhỏ hơn 15MB.");
                }
                else
                {
                    var extractedBudget = await ContractBudgetExtractor.TryExtractProjectBudgetAsync(contractFile);
                    if (extractedBudget is not null)
                    {
                        // If contract provides total cost, override user input to keep data consistent.
                        project.Budget = extractedBudget.Value;
                        project.BudgetLocked = true;

                        // Save uploaded PDF into database.
                        await using (var ms = new System.IO.MemoryStream())
                        {
                            await contractFile.CopyToAsync(ms);
                            project.ContractFileContent = ms.ToArray();
                        }
                        project.ContractFileName = contractFile.FileName;
                        project.ContractFileContentType = contractFile.ContentType;
                        project.ContractUploadedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        ModelState.AddModelError("contractFile", "Không thể trích xuất tổng chi phí từ hợp đồng. Hãy kiểm tra lại file (định dạng PDF với văn bản có thể chọn).");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ManagerId"] = GetUsersWithRoles(project.ManagerId, new[] { "Project Manager", "Quản lý dự án", "Admin", "Quản trị viên" });
            return View(project);
        }

        // GET: Projects/Edit/5
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            ViewData["ManagerId"] = GetUsersWithRoles(project.ManagerId, new[] { "Project Manager", "Quản lý dự án", "Admin", "Quản trị viên" });
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectId,ManagerId,ProjectName,Description,Budget,StartDate,EndDate,Status")] Project project)
        {
            if (id != project.ProjectId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(project.Manager));
            ModelState.Remove(nameof(project.InventoryTransactions));
            ModelState.Remove(nameof(project.MaterialUsages));
            ModelState.Remove(nameof(project.Stages));
            ModelState.Remove(nameof(project.Tasks));

            if (project.StartDate.HasValue && project.EndDate.HasValue && project.EndDate < project.StartDate)
            {
                ModelState.AddModelError(nameof(project.EndDate), "Ngày kết thúc dự án không được nhỏ hơn ngày bắt đầu.");
            }

            if (_context.Projects.Any(p => p.ProjectId != id && p.ProjectName.Trim().ToLower() == project.ProjectName.Trim().ToLower()))
            {
                ModelState.AddModelError(nameof(project.ProjectName), "Tên dự án đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Preserve contract file + BudgetLocked fields so that budget cannot be unlocked
                    // when user edits other properties.
                    var existing = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.ManagerId = project.ManagerId;
                    existing.ProjectName = project.ProjectName;
                    existing.Description = project.Description;
                    existing.StartDate = project.StartDate;
                    existing.EndDate = project.EndDate;
                    existing.Status = project.Status;

                    if (!existing.BudgetLocked)
                    {
                        existing.Budget = project.Budget;
                    }

                    // BudgetLocked is not changed here by UI.
                    // Contract file fields are also preserved (no contract upload on Edit screen).

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.ProjectId))
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
            ViewData["ManagerId"] = GetUsersWithRoles(project.ManagerId, new[] { "Project Manager", "Quản lý dự án", "Admin", "Quản trị viên" });
            return View(project);
        }

        // GET: Projects/Delete/5
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(m => m.ProjectId == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Stages)
                    .ThenInclude(s => s.Tasks)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project != null)
            {
                // Xóa toàn bộ task thuộc các stage của dự án
                var allTasks = project.Stages.SelectMany(s => s.Tasks).ToList();
                if (allTasks.Any())
                {
                    _context.Tasks.RemoveRange(allTasks);
                }

                // Xóa các stage của dự án
                if (project.Stages.Any())
                {
                    _context.Stages.RemoveRange(project.Stages);
                }

                // (Tuỳ chọn) Nếu bạn muốn, có thể xóa luôn Tasks gắn thẳng với Project mà không qua Stage
                var projectLevelTasks = _context.Tasks.Where(t => t.ProjectId == id).ToList();
                if (projectLevelTasks.Any())
                {
                    _context.Tasks.RemoveRange(projectLevelTasks);
                }

                // Xóa các giao dịch kho liên quan đến dự án
                var inventoryTransactions = _context.InventoryTransactions.Where(it => it.ProjectId == id).ToList();
                if (inventoryTransactions.Any())
                {
                    _context.InventoryTransactions.RemoveRange(inventoryTransactions);
                }
                
                // Xóa các giao dịch sử dụng vật liệu liên quan đến dự án
                var materialUsages = _context.MaterialUsages.Where(mu => mu.ProjectId == id).ToList();
                if (materialUsages.Any())
                {
                    _context.MaterialUsages.RemoveRange(materialUsages);
                }

                // Cuối cùng xóa chính dự án
                _context.Projects.Remove(project);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
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

        private SelectList GetUsersWithRoles(string? selectedId = null, string[]? allowedRoles = null)
        {
            var query = _context.Users.Include(u => u.Role).AsQueryable();
            if (allowedRoles != null && allowedRoles.Length > 0)
            {
                query = query.Where(u => u.Role != null && allowedRoles.Contains(u.Role.RoleName.Trim()));
            }

            var users = query.ToList().Select(u => new {
                UserId = u.UserId,
                DisplayName = $"{u.FullName} - {TranslateRole(u.Role?.RoleName)}"
            });
            return new SelectList(users, "UserId", "DisplayName", selectedId);
        }
    }
}
