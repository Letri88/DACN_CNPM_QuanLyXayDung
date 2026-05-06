using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using DACN_CNPM_QuanLyXayDung.Models;
using Microsoft.EntityFrameworkCore;

namespace DACN_CNPM_QuanLyXayDung.Controllers
{
    public class AccountController : Controller
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public AccountController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = "/")
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return Redirect(returnUrl ?? "/");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = "/")
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.Status == "Active");

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName ?? user.Username ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, user.Username ?? ""),
                    new Claim("UserId", user.UserId)
                };

                if (user.Role != null && !string.IsNullOrEmpty(user.Role.RoleName))
                {
                    claims.Add(new Claim(ClaimTypes.Role, user.Role.RoleName.Trim()));
                }

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), 
                    authProperties);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/")
                {
                    return Redirect(returnUrl);
                }
                
                // Redirect based on role if no specific returnUrl is provided
                if (user.Role != null)
                {
                    var role = user.Role.RoleName.Trim().ToLower();
                    if (role == "warehouse keeper" || role == "thủ kho")
                        return RedirectToAction("Index", "Materials");
                    if (role == "engineer" || role == "kỹ sư")
                        return RedirectToAction("Index", "Tasks");
                    if (role == "admin" || role == "quản trị viên")
                        return RedirectToAction("Index", "Users");
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Username hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Username"] = username;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult Register()
        {
            var roles = _context.Roles.ToList().Select(r => new {
                RoleId = r.RoleId,
                RoleName = r.RoleName?.Trim().ToLower() switch {
                    "admin" or "quản trị viên" => "Quản trị viên",
                    "project manager" or "quản lý dự án" => "Quản lý dự án",
                    "engineer" or "kỹ sư" => "Kỹ sư",
                    "warehouse keeper" or "thủ kho" => "Thủ kho",
                    _ => r.RoleName
                }
            }).ToList();
            
            ViewBag.Roles = roles;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string fullName, string password, int roleId)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fullName) || roleId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ thông tin và chọn vai trò.");
                
                // Re-populate roles
                ViewBag.Roles = _context.Roles.ToList().Select(r => new {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName?.Trim().ToLower() switch {
                        "admin" or "quản trị viên" => "Quản trị viên",
                        "project manager" or "quản lý dự án" => "Quản lý dự án",
                        "engineer" or "kỹ sư" => "Kỹ sư",
                        "warehouse keeper" or "thủ kho" => "Thủ kho",
                        _ => r.RoleName
                    }
                }).ToList();
                return View();
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Tên tài khoản (Username) đã tồn tại.");
                
                // Re-populate roles
                ViewBag.Roles = _context.Roles.ToList().Select(r => new {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName?.Trim().ToLower() switch {
                        "admin" or "quản trị viên" => "Quản trị viên",
                        "project manager" or "quản lý dự án" => "Quản lý dự án",
                        "engineer" or "kỹ sư" => "Kỹ sư",
                        "warehouse keeper" or "thủ kho" => "Thủ kho",
                        _ => r.RoleName
                    }
                }).ToList();
                return View();
            }

            var role = await _context.Roles.FindAsync(roleId);
            string prefix = role?.RoleName?.Trim().ToLower() switch {
                "admin" or "quản trị viên" => "Admin",
                "project manager" or "quản lý dự án" => "PM",
                "engineer" or "kỹ sư" => "EN",
                "warehouse keeper" or "thủ kho" => "WK",
                _ => "USER"
            };

            var lastUser = await _context.Users
                .Where(u => u.UserId.StartsWith(prefix))
                .OrderByDescending(u => u.UserId)
                .FirstOrDefaultAsync();

            int nextId = 1;
            if (lastUser != null)
            {
                string numericPart = lastUser.UserId.Replace(prefix, "");
                if (int.TryParse(numericPart, out int lastId))
                {
                    nextId = lastId + 1;
                }
            }
            string newUserId = prefix + nextId.ToString("D3");

            var newUser = new User
            {
                UserId = newUserId,
                Username = username,
                FullName = fullName,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId = roleId,
                Status = "Active"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập tên tài khoản.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản này trong hệ thống.");
                return View();
            }

            // Find all admins
            var admins = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role != null && (u.Role.RoleName == "Admin" || u.Role.RoleName == "Quản trị viên"))
                .ToListAsync();

            if (admins.Any())
            {
                foreach (var admin in admins)
                {
                    var notification = new Notification
                    {
                        UserId = admin.UserId,
                        Message = $"Tài khoản {user.Username} ({user.FullName}) vừa yêu cầu cấp lại mật khẩu. Vui lòng kiểm tra và hỗ trợ.",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        RelatedUrl = $"/Users/Edit/{user.UserId}"
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Yêu cầu cấp lại mật khẩu đã được gửi đến Quản trị viên. Vui lòng chờ liên hệ.";
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

    }
   
}
