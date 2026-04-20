using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class PurchaseOrderDetail
{
    public int Id { get; set; }

    public int POId { get; set; }

    public int MaterialId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Total { get; set; }

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    
    public virtual Material Material { get; set; } = null!;
}
