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
    [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án,Engineer,Kỹ sư,Warehouse Keeper,Thủ kho")]
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

            var usedMaterials = await _context.InventoryTransactions
                .Where(t => t.StageId == stage.StageId && t.Type == "Xuất kho")
                .GroupBy(t => t.Material.MaterialName)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(t => t.Quantity));

            ViewBag.UsedMaterials = usedMaterials;

            return View(stage);
        }

        // GET: Stages/DownloadMaterialDeclaration
        // Trả về PDF đã upload để người dùng xem lại.
        [HttpGet]
        public async Task<IActionResult> DownloadMaterialDeclaration(int id)
        {
            var stage = await _context.Stages.FirstOrDefaultAsync(m => m.StageId == id);
            if (stage == null || stage.MaterialDeclarationFileContent == null || stage.MaterialDeclarationFileContent.Length == 0)
            {
                return NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(stage.MaterialDeclarationContentType)
                ? "application/pdf"
                : stage.MaterialDeclarationContentType;

            var fileName = string.IsNullOrWhiteSpace(stage.MaterialDeclarationFileName)
                ? "material-declaration.pdf"
                : stage.MaterialDeclarationFileName;

            return File(stage.MaterialDeclarationFileContent, contentType, fileName);
        }

        [HttpPost]
        public async Task<IActionResult> ExtractStageBudget(string stageName, IFormFile? contractFile)
        {
            if (string.IsNullOrWhiteSpace(stageName)) return BadRequest(new { message = "Vui lòng nhập tên giai đoạn trước khi tải file lên." });
            if (contractFile is null || contractFile.Length == 0) return BadRequest(new { message = "Bạn cần chọn file hợp đồng (PDF)." });
            if (!FileValidationHelper.IsPdfFile(contractFile)) return BadRequest(new { message = "File tải lên không hợp lệ hoặc không phải là file PDF thực sự." });
            if (contractFile.Length > 15 * 1024 * 1024) return BadRequest(new { message = "File hợp đồng quá lớn. Vui lòng chọn file nhỏ hơn 15MB." });

            var extractedBudget = await ContractBudgetExtractor.TryExtractStageBudgetAsync(contractFile, stageName);
            if (extractedBudget is null) return BadRequest(new { message = $"Không thể trích xuất chi phí cho giai đoạn '{stageName}' từ hợp đồng. Vui lòng kiểm tra file." });

            return Ok(new { budget = extractedBudget.Value, budgetLocked = true });
        }

        // POST: Stages/UploadMaterialDeclaration
        // Upload bản kê khai vật liệu (PDF) -> trích "tổng chi phí" -> điền Budget cho giai đoạn.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMaterialDeclaration(int id, IFormFile? materialDeclarationFile)
        {
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

            if (stage.MaterialDeclarationFileName != null && stage.MaterialDeclarationFileName.StartsWith("[Đã duyệt]"))
            {
                ModelState.AddModelError(string.Empty, "Yêu cầu vật tư này đã được duyệt. Không thể tải lên lại.");
                return View("Details", stage);
            }

            if (materialDeclarationFile is null || materialDeclarationFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Bạn cần chọn file PDF để tải lên.");
                return View("Details", stage);
            }

            if (!FileValidationHelper.IsPdfFile(materialDeclarationFile))
            {
                ModelState.AddModelError(string.Empty, "File tải lên không hợp lệ hoặc không phải file PDF thực sự.");
                return View("Details", stage);
            }

            if (materialDeclarationFile.Length > 15 * 1024 * 1024)
            {
                ModelState.AddModelError(string.Empty, "File quá lớn. Vui lòng chọn file nhỏ hơn 15MB.");
                return View("Details", stage);
            }

            // DO NOT EXTRACT BUDGET HERE.
            // Just save the PDF into database and notify the PM.

            await using (var ms = new System.IO.MemoryStream())
            {
                await materialDeclarationFile.CopyToAsync(ms);
                stage.MaterialDeclarationFileContent = ms.ToArray();
            }
            stage.MaterialDeclarationFileName = materialDeclarationFile.FileName;
            stage.MaterialDeclarationContentType = materialDeclarationFile.ContentType;
            stage.MaterialDeclarationUploadedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify PM
            if (stage.Project?.ManagerId != null)
            {
                var notification = new Notification
                {
                    UserId = stage.Project.ManagerId.Value,
                    Message = $"Kỹ sư đã gửi yêu cầu vật tư cho giai đoạn '{stage.StageName}'. Vui lòng kiểm tra và duyệt.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    RelatedUrl = $"/Stages/Details/{stage.StageId}"
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Đã gửi bản kê khai vật liệu cho Quản lý dự án để duyệt.";
            return RedirectToAction("Details", new { id = stage.StageId });
        }

        // POST: Stages/ApproveMaterialRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
        public async Task<IActionResult> ApproveMaterialRequest(int id)
        {
            var stage = await _context.Stages
                .Include(s => s.Project)
                .FirstOrDefaultAsync(m => m.StageId == id);

            if (stage == null || stage.MaterialDeclarationFileContent == null)
            {
                return NotFound();
            }

            if (stage.MaterialDeclarationFileName != null && stage.MaterialDeclarationFileName.StartsWith("[Đã duyệt]"))
            {
                TempData["ErrorMessage"] = "Yêu cầu này đã được duyệt trước đó.";
                return RedirectToAction("Details", new { id = stage.StageId });
            }

            var extractedCost = ContractBudgetExtractor.TryExtractMaterialRequestTotal(stage.MaterialDeclarationFileContent);
            if (extractedCost is null)
            {
                TempData["ErrorMessage"] = "Không thể trích xuất 'Tổng chi phí' từ file PDF yêu cầu vật tư. Vui lòng kiểm tra lại cấu trúc file.";
                return RedirectToAction("Details", new { id = stage.StageId });
            }

            if (extractedCost > (stage.Budget ?? 0))
            {
                TempData["ErrorMessage"] = "Số tiền yêu cầu vật tư (" + extractedCost.Value.ToString("N0") + " đ) lớn hơn ngân sách hiện tại của giai đoạn (" + (stage.Budget ?? 0).ToString("N0") + " đ). Không thể duyệt.";
                return RedirectToAction("Details", new { id = stage.StageId });
            }

            stage.Budget = (stage.Budget ?? 0) - extractedCost.Value;
            
            // Mark as approved via filename
            stage.MaterialDeclarationFileName = "[Đã duyệt] " + (stage.MaterialDeclarationFileName ?? "Bản_kê_khai.pdf");

            await _context.SaveChangesAsync();

            // Notify Warehouse Keepers
            var warehouseKeepers = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role != null && (u.Role.RoleName.Trim() == "Warehouse Keeper" || u.Role.RoleName.Trim() == "Thủ kho"))
                .ToListAsync();

            foreach (var wk in warehouseKeepers)
            {
                var notification = new Notification
                {
                    UserId = wk.UserId,
                    Message = $"PM đã duyệt yêu cầu vật tư cho giai đoạn '{stage.StageName}'. Vui lòng tiến hành xuất kho.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    RelatedUrl = $"/Stages/Details/{stage.StageId}"
                };
                _context.Notifications.Add(notification);
            }
            if (warehouseKeepers.Any())
            {
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Đã duyệt yêu cầu vật tư thành công. Chi phí đã được trừ vào ngân sách giai đoạn.";
            return RedirectToAction("Details", new { id = stage.StageId });
        }

        // GET: Stages/Create
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
        public IActionResult Create()
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            ViewData["AssignedUserId"] = GetUsersWithRoles(null, new[] { "Engineer", "Kỹ sư" });
            return View();
        }

        // POST: Stages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
        public async Task<IActionResult> Create([Bind("StageId,ProjectId,StageName,StartDate,EndDate,Status,AssignedUserId,Budget")] Stage stage, IFormFile? contractFile)
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

            if (contractFile is not null && contractFile.Length > 0)
            {
                if (!FileValidationHelper.IsPdfFile(contractFile))
                {
                    ModelState.AddModelError(string.Empty, "File tải lên không hợp lệ hoặc không phải file PDF thực sự.");
                }
                else if (contractFile.Length > 15 * 1024 * 1024)
                {
                    ModelState.AddModelError(string.Empty, "File hợp đồng quá lớn. Vui lòng chọn file nhỏ hơn 15MB.");
                }
                else
                {
                    var extractedBudget = await ContractBudgetExtractor.TryExtractStageBudgetAsync(contractFile, stage.StageName);
                    if (extractedBudget is not null)
                    {
                        stage.Budget = extractedBudget.Value;
                        stage.BudgetLocked = true;
                        // Note: We deliberately do NOT save this Contract File into the MaterialDeclaration fields
                        // so that the Engineer can later upload their own Material Request PDF.
                    }
                    else
                    {
                        ModelState.AddModelError("contractFile", "Không thể trích xuất chi phí từ hợp đồng. Hãy kiểm tra lại file (nhớ nhập đúng Tên giai đoạn).");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(stage);
                await _context.SaveChangesAsync();

                if (stage.AssignedUserId.HasValue)
                {
                    var notification = new Notification
                    {
                        UserId = stage.AssignedUserId.Value,
                        Message = $"Bạn đã được phân công vào giai đoạn '{stage.StageName}'",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        RelatedUrl = $"/Stages/Details/{stage.StageId}"
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectId"] = new SelectList(_context.Projects, "ProjectId", "ProjectName", stage.ProjectId);
            return View(stage);
        }

        // GET: Stages/Edit/5
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
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
            ViewData["AssignedUserId"] = GetUsersWithRoles(stage.AssignedUserId, new[] { "Engineer", "Kỹ sư" });
            return View(stage);
        }

        // POST: Stages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
        public async Task<IActionResult> Edit(int id, [Bind("StageId,ProjectId,StageName,StartDate,EndDate,Status,AssignedUserId,Budget")] Stage stage)
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
                    var originalStage = await _context.Stages.AsNoTracking().FirstOrDefaultAsync(s => s.StageId == stage.StageId);
                    bool assigneeChanged = originalStage?.AssignedUserId != stage.AssignedUserId;

                    _context.Update(stage);
                    await _context.SaveChangesAsync();

                    if (assigneeChanged && stage.AssignedUserId.HasValue)
                    {
                        var notification = new Notification
                        {
                            UserId = stage.AssignedUserId.Value,
                            Message = $"Bạn đã được phân công vào giai đoạn '{stage.StageName}'",
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            RelatedUrl = $"/Stages/Details/{stage.StageId}"
                        };
                        _context.Notifications.Add(notification);
                        await _context.SaveChangesAsync();
                    }
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
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
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
        [Authorize(Roles = "Admin,Project Manager,Quản trị viên,Quản lý dự án")]
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

        private SelectList GetUsersWithRoles(int? selectedId = null, string[]? allowedRoles = null)
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
