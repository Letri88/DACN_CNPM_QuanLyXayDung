using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACN_CNPM_QuanLyXayDung.Models;

public class ProjectMaterialRequestDetail
{
    [Key]
    public int Id { get; set; }

    public int RequestId { get; set; }

    public int MaterialId { get; set; }

    public int QuantityRequested { get; set; }

    [StringLength(255)]
    public string? Note { get; set; }

    // Optionally record how much was actually exported
    public int? QuantityExported { get; set; }

    [ForeignKey("RequestId")]
    public virtual ProjectMaterialRequest? Request { get; set; }

    [ForeignKey("MaterialId")]
    public virtual Material? Material { get; set; }
}
