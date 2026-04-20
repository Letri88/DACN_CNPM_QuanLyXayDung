using System;
using System.ComponentModel.DataAnnotations;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class DocumentAttachment
{
    [Key]
    public int DocId { get; set; }

    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = null!; // "MaterialRequest", "PurchaseOrder", "MaterialReceipt"

    [Required]
    public int EntityId { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = null!;

    public DateTime UploadedAt { get; set; } = DateTime.Now;
}
