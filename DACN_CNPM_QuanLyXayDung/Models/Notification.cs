using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACN_CNPM_QuanLyXayDung.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public string UserId { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string Message { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        [StringLength(500)]
        public string? RelatedUrl { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
