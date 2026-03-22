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
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = "/")
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password && u.Status == "Active");

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName ?? user.Email ?? ""),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
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

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Email"] = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
