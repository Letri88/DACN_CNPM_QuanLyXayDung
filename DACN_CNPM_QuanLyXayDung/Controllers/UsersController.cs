using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;

using Microsoft.AspNetCore.Authorization;

namespace DACN_CNPM_QuanLyXayDung.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public UsersController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role == null || (u.Role.RoleName != "Admin" && u.Role.RoleName != "Quản trị viên"))
                .ToListAsync();
            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            ViewData["RoleId"] = new SelectList(GetTranslatedRoles(), "RoleId", "RoleName");
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,RoleId,FullName,Username,Password,Status")] User user)
        {
            ModelState.Remove(nameof(user.Role));
            ModelState.Remove(nameof(user.InventoryTransactions));
            ModelState.Remove(nameof(user.Projects));

            if (ModelState.IsValid)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoleId"] = new SelectList(GetTranslatedRoles(), "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ViewData["RoleId"] = new SelectList(GetTranslatedRoles(), "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,RoleId,FullName,Username,Password,Status")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(user.Role));
            ModelState.Remove(nameof(user.InventoryTransactions));
            ModelState.Remove(nameof(user.Projects));

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if password has changed (assuming it's not a hash starting with $2a$ or $2b$)
                    if (!user.Password.StartsWith("$2"))
                    {
                        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                    }
                    else
                    {
                        // If password field is unchanged from edit form, keep the original hash.
                        // Ideally we should make it empty and check string.IsNullOrEmpty.
                        // Setting state to modified handles updates safely.
                    }
                    
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
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
            ViewData["RoleId"] = new SelectList(GetTranslatedRoles(), "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        private System.Collections.IEnumerable GetTranslatedRoles()
        {
            return _context.Roles
                .Where(r => r.RoleName != "Admin" && r.RoleName != "Quản trị viên")
                .ToList()
                .Select(r => new {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName?.Trim().ToLower() switch {
                        "project manager" => "Quản lý dự án",
                        "engineer" => "Kỹ sư",
                        "warehouse keeper" => "Thủ kho",
                        _ => r.RoleName
                    }
                });
        }
    }
}
