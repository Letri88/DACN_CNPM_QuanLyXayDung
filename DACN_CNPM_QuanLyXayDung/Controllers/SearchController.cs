using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;
using System.Linq;
using System.Threading.Tasks;

namespace DACN_CNPM_QuanLyXayDung.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public SearchController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(new object[] { });
            }

            var query = q.ToLower().Trim();

            // 1. Projects
            var projects = await _context.Projects
                .Where(p => p.ProjectName.Contains(query))
                .Select(p => new
                {
                    type = "Dự án",
                    id = p.ProjectId.ToString(),
                    title = p.ProjectName,
                    description = p.Status,
                    url = $"/Projects/Details/{p.ProjectId}",
                    icon = "architecture"
                })
                .Take(5)
                .ToListAsync();

            // 2. Materials
            var materials = await _context.Materials
                .Where(m => m.MaterialName.Contains(query))
                .Select(m => new
                {
                    type = "Vật tư",
                    id = m.MaterialId.ToString(),
                    title = m.MaterialName,
                    description = $"Đơn vị: {m.Unit}",
                    url = $"/Materials/Details/{m.MaterialId}",
                    icon = "inventory_2"
                })
                .Take(5)
                .ToListAsync();

            // 3. Stages
            var stages = await _context.Stages
                .Include(s => s.Project)
                .Where(s => s.StageName.Contains(query))
                .Select(s => new
                {
                    type = "Giai đoạn",
                    id = s.StageId.ToString(),
                    title = s.StageName,
                    description = $"Dự án: {s.Project.ProjectName}",
                    url = $"/Stages/Details/{s.StageId}",
                    icon = "linear_scale"
                })
                .Take(5)
                .ToListAsync();

            // 4. Users / Resources
            var users = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.FullName.Contains(query) || u.Username.Contains(query))
                .Select(u => new
                {
                    type = "Tài nguyên",
                    id = u.UserId,
                    title = u.FullName,
                    description = u.Role.RoleName,
                    url = $"/Users/Details/{u.UserId}",
                    icon = "person"
                })
                .Take(5)
                .ToListAsync();

            // 5. Equipments
            var equipments = await _context.Equipments
                .Where(e => e.EquipmentName.Contains(query) || e.EquipmentCode.Contains(query))
                .Select(e => new
                {
                    type = "Máy móc",
                    id = e.EquipmentId.ToString(),
                    title = e.EquipmentName,
                    description = $"Mã: {e.EquipmentCode} - Trạng thái: {e.Status}",
                    url = $"/Equipments/Details/{e.EquipmentId}",
                    icon = "construction"
                })
                .Take(5)
                .ToListAsync();

            var results = projects.Cast<object>()
                .Concat(materials)
                .Concat(stages)
                .Concat(users)
                .Concat(equipments);

            return Ok(results);
        }
    }
}
