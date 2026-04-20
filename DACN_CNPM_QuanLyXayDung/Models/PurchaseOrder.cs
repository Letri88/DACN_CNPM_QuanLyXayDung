using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class PurchaseOrder
{
    [Key]
    public int POId { get; set; }

    [Required]
    [StringLength(50)]
    public string POCode { get; set; } = null!;

    public int RequestId { get; set; }

    public int SupplierId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Sent, Partially Received, Completed, Cancelled

    [StringLength(500)]
    public string? DeliveryTerms { get; set; }

    [StringLength(255)]
    public string? ShippingAddress { get; set; }

    public decimal TotalAmount { get; set; }

    public virtual MaterialRequest Request { get; set; } = null!;
    public virtual Supplier Supplier { get; set; } = null!;

    public virtual ICollection<PurchaseOrderDetail> Details { get; set; } = new List<PurchaseOrderDetail>();
    public virtual ICollection<MaterialReceipt> MaterialReceipts { get; set; } = new List<MaterialReceipt>();
}
