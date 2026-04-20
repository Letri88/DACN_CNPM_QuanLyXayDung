using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class MaterialRequest
{
    [Key]
    public int RequestId { get; set; }

    [Required]
    [StringLength(50)]
    public string RequestCode { get; set; } = null!; // Format: YCX-YYYYMMDD-XXXX

    [Required]
    [StringLength(50)]
    public string CreatorId { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Returned

    [StringLength(500)]
    public string? Reason { get; set; }

    public DateTime? ExpectedDate { get; set; }

    [StringLength(50)]
    public string? ApproverId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [StringLength(500)]
    public string? ApprovalNote { get; set; }

    public virtual User Creator { get; set; } = null!;
    public virtual User? Approver { get; set; }

    public virtual ICollection<MaterialRequestDetail> Details { get; set; } = new List<MaterialRequestDetail>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
