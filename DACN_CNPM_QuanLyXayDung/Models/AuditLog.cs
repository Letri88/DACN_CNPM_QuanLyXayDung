using System;
using System.ComponentModel.DataAnnotations;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class AuditLog
{
    [Key]
    public int LogId { get; set; }

    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = null!; // "MaterialRequest", "PurchaseOrder", "MaterialReceipt"

    [Required]
    public int EntityId { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = null!; // "Created", "Approved", "Received"

    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.Now;

    [StringLength(4000)]
    public string? Details { get; set; }

    public virtual User User { get; set; } = null!;
}
