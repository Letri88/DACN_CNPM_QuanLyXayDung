using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACN_CNPM_QuanLyXayDung.Models;

public class ProjectMaterialRequest
{
    [Key]
    public int RequestId { get; set; }

    [Required]
    [StringLength(50)]
    public string RequestCode { get; set; } = null!;

    public int ProjectId { get; set; }

    [StringLength(50)]
    public string RequesterId { get; set; } = null!; // Engineer ID

    [StringLength(50)]
    public string? ApproverId { get; set; } // Project Manager ID

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpectedDate { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Exported

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(500)]
    public string? ApprovalNote { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("RequesterId")]
    public virtual User? Requester { get; set; }

    [ForeignKey("ApproverId")]
    public virtual User? Approver { get; set; }

    public virtual ICollection<ProjectMaterialRequestDetail> Details { get; set; } = new List<ProjectMaterialRequestDetail>();
}
