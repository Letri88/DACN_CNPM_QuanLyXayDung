using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class MaterialReceiptDetail
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    public int MaterialId { get; set; }

    public int? RequestedQuantity { get; set; }

    [Required]
    public int ActualQuantity { get; set; }

    public int? DamagedQuantity { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public virtual MaterialReceipt MaterialReceipt { get; set; } = null!;
    
    public virtual Material Material { get; set; } = null!;
}
