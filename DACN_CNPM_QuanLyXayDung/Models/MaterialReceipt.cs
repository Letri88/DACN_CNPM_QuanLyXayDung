using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class MaterialReceipt
{
    [Key]
    public int ReceiptId { get; set; }

    [Required]
    [StringLength(50)]
    public string ReceiptCode { get; set; } = null!;

    public int? POId { get; set; }

    [Required]
    [StringLength(50)]
    public string ReceiverId { get; set; } = null!;

    public DateTime ReceivedAt { get; set; } = DateTime.Now;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Completed

    [StringLength(500)]
    public string? Note { get; set; }

    public virtual PurchaseOrder? PurchaseOrder { get; set; }
    public virtual User Receiver { get; set; } = null!;

    public virtual ICollection<MaterialReceiptDetail> Details { get; set; } = new List<MaterialReceiptDetail>();
}
