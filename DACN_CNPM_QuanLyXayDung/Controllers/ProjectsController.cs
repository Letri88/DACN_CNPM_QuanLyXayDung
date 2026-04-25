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
            var query = _context.Projects
                .Include(p => p.Manager)
                    .ThenInclude(u => u.Role)
                .Include(p => p.Stages)
                    .ThenInclude(s => s.Tasks)
                .AsQueryable();

            var currentUserId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            
            // Nếu user là Engineer, chỉ hiển thị các dự án mà họ được phân công
            if (User.IsInRole("Engineer") || User.IsInRole("Kỹ sư"))
            {
                query = query.Where(p => p.ManagerId == currentUserId || p.Stages.Any(s => s.AssignedUserId == currentUserId));
            }

            return View(await query.ToListAsync());
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
                .Include(p => p.Stages)
                    .ThenInclude(s => s.AssignedUser)
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
        // Dùng AJAX để trích dữ liệu từ PDF hợp đồng -> trả về data cho UI.
        [HttpPost]
        public async Task<IActionResult> ExtractContractBudget(IFormFile? contractFile)
        {
            if (contractFile is null || contractFile.Length == 0)
            {
                return BadRequest(new { message = "Bạn cần chọn file hợp đồng (PDF)." });
            }

            if (!FileValidationHelper.IsPdfFile(contractFile))
            {
                return BadRequest(new { message = "File tải lên không hợp lệ hoặc không phải là file PDF thực sự (vui lòng không đổi đuôi file)." });
            }

            if (contractFile.Length > 15 * 1024 * 1024)
            {
                return BadRequest(new { message = "File hợp đồng quá lớn. Vui lòng chọn file nhỏ hơn 15MB." });
            }

            var extractedData = await ContractBudgetExtractor.TryExtractProjectDetailsAsync(contractFile);
            if (extractedData is null)
            {
                return BadRequest(new { message = "Không thể đọc dữ liệu từ hợp đồng." });
            }

            string? managerId = null;
            if (!string.IsNullOrWhiteSpace(extractedData.ManagerId))
            {
                // Try to find matching user by UserId and confirm they have the correct role
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId.ToLower() == extractedData.ManagerId.ToLower().Trim());
                    
                if (user != null && user.Role != null)
                {
                    var roleName = user.Role.RoleName.Trim().ToLower();
                    if (roleName == "project manager" || roleName == "quản lý dự án")
                    {
                        managerId = user.UserId;
                    }
                }
            }

            return Ok(new 
            { 
                budget = extractedData.Budget, 
                budgetLocked = extractedData.Budget.HasValue,
                projectName = extractedData.ProjectName,
                description = extractedData.Description,
                managerId = managerId,
                startDate = extractedData.StartDate?.ToString("yyyy-MM-dd"),
                endDate = extractedData.EndDate?.ToString("yyyy-MM-dd")
            });
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
                if (!FileValidationHelper.IsPdfFile(contractFile))
                {
                    ModelState.AddModelError(string.Empty, "File tải lên không hợp lệ hoặc không phải là file PDF thực sự.");
                }
                else if (contractFile.Length > 15 * 1024 * 1024)
                {
                    ModelState.AddModelError(string.Empty, "File hợp đồng quá lớn. Vui lòng chọn file nhỏ hơn 15MB.");
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

                        var extractedStages = await ContractBudgetExtractor.ExtractStagesAsync(contractFile);
                        if (extractedStages != null && extractedStages.Any())
                        {
                            project.Stages = new List<Stage>();
                            foreach (var dto in extractedStages)
                            {
                                var stageName = dto.StageName;
                                if (stageName.Length > 150) stageName = stageName.Substring(0, 150);

                                var newStage = new Stage
                                {
                                    StageName = stageName,
                                    StartDate = dto.StartDate,
                                    EndDate = dto.EndDate,
                                    Budget = dto.Budget,
                                    BudgetLocked = dto.Budget.HasValue,
                                    Status = "Not Started"
                                };

                                if (!string.IsNullOrEmpty(dto.AssignedUserId))
                                {
                                    var matchedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId.ToLower() == dto.AssignedUserId.ToLower());
                                    if (matchedUser != null)
                                    {
                                        newStage.AssignedUserId = matchedUser.UserId;
                                    }
                                }
                                project.Stages.Add(newStage);
                            }
                        }
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

                var notifications = new List<Notification>();

                if (project.ManagerId != null)
                {
                    notifications.Add(new Notification
                    {
                        UserId = project.ManagerId,
                        Message = $"Bạn đã được chọn làm Quản lý dự án cho dự án '{project.ProjectName}'.",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        RelatedUrl = $"/Projects/Details/{project.ProjectId}"
                    });
                }

                if (project.Stages != null && project.Stages.Any())
                {
                    foreach (var stage in project.Stages)
                    {
                        if (!string.IsNullOrEmpty(stage.AssignedUserId))
                        {
                            notifications.Add(new Notification
                            {
                                UserId = stage.AssignedUserId,
                                Message = $"Bạn đã được phân công quản lý giai đoạn '{stage.StageName}' trong dự án '{project.ProjectName}'.",
                                CreatedAt = DateTime.Now,
                                IsRead = false,
                                RelatedUrl = $"/Stages/Details/{stage.StageId}"
                            });
                        }
                    }
                }

                if (notifications.Any())
                {
                    _context.Notifications.AddRange(notifications);
                    await _context.SaveChangesAsync();
                }

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
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == id);
            if (project == null) return RedirectToAction(nameof(Index));

            // Dùng ExecuteDeleteAsync để đảm bảo thứ tự xóa trực tiếp dưới DB và chính xác tuyệt đối
            await _context.InventoryTransactions.Where(it => it.ProjectId == id).ExecuteDeleteAsync();
            await _context.MaterialUsages.Where(mu => mu.ProjectId == id).ExecuteDeleteAsync();
            
            // Xóa Tasks trực tiếp thuộc Project
            await _context.Tasks.Where(t => t.ProjectId == id).ExecuteDeleteAsync();

            var stageIds = _context.Stages.Where(s => s.ProjectId == id).Select(s => s.StageId);
            
            // Xóa Tasks và Giao dịch kho thuộc các Stage của dự án
            await _context.Tasks.Where(t => stageIds.Contains(t.StageId)).ExecuteDeleteAsync();
            await _context.InventoryTransactions.Where(it => it.StageId != null && stageIds.Contains(it.StageId.Value)).ExecuteDeleteAsync();

            // Xóa Stages
            await _context.Stages.Where(s => s.ProjectId == id).ExecuteDeleteAsync();

            // Cuối cùng xóa dự án (sẽ pass qua không bị kẹt reference constraint nào)
            _context.Projects.Remove(project);
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
            var usersList = _context.Users.Include(u => u.Role).ToList();

            if (allowedRoles != null && allowedRoles.Length > 0)
            {
                var allowedSet = allowedRoles.Select(r => r.Trim().ToLower()).ToHashSet();
                usersList = usersList.Where(u => u.Role != null && allowedSet.Contains(u.Role.RoleName.Trim().ToLower())).ToList();
            }

            // Exclude Admin from project management as well if requested, but typically Admin can manage projects.
            // Following the user's preference for role separation.
            usersList = usersList.Where(u => u.Role == null || 
                (u.Role.RoleName.Trim().ToLower() != "admin" && u.Role.RoleName.Trim().ToLower() != "quản trị viên")).ToList();

            var users = usersList.Select(u => new {
                UserId = u.UserId,
                DisplayName = $"{u.FullName} - {TranslateRole(u.Role?.RoleName)}"
            });
            return new SelectList(users, "UserId", "DisplayName", selectedId);
        }
    }
}
