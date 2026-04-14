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
                    new Claim("UserId", user.UserId.ToString())
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
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string fullName, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fullName))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ thông tin.");
                return View();
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Tên tài khoản (Username) đã tồn tại.");
                return View();
            }

            // Find Admin role ID
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin" || r.RoleName == "Quản trị viên");

            var newUser = new User
            {
                Username = username,
                FullName = fullName,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId = adminRole?.RoleId ?? 4, // Default Admin RoleId fallback
                Status = "Active"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm tài khoản Quản trị viên thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

    }
   
}
