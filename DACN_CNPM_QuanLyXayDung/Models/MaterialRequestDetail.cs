using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACN_CNPM_QuanLyXayDung.Models;

public partial class MaterialRequestDetail
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public int MaterialId { get; set; }

    [Required]
    public int QuantityRequested { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public virtual MaterialRequest MaterialRequest { get; set; } = null!;
    
    public virtual Material Material { get; set; } = null!;
}
