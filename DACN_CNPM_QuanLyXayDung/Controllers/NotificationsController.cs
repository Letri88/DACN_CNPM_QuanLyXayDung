using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DACN_CNPM_QuanLyXayDung.Models;

namespace DACN_CNPM_QuanLyXayDung.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly HeThongQlvongDoiDuAnTaiNguyenContext _context;

        public NotificationsController(HeThongQlvongDoiDuAnTaiNguyenContext context)
        {
            _context = context;
        }

        // GET: /Notifications
        public async Task<IActionResult> Index()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();
                return View(notifications);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id, string? returnUrl)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (!string.IsNullOrEmpty(userId) && notification.UserId == userId)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            if (!string.IsNullOrEmpty(notification?.RelatedUrl) && Url.IsLocalUrl(notification.RelatedUrl))
            {
                return Redirect(notification.RelatedUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
