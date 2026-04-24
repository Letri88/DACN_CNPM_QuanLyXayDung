using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACN_CNPM_QuanLyXayDung.Models;

public class EquipmentDispatch
{
    [Key]
    public int DispatchId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thiết bị")]
    [Display(Name = "Thiết bị")]
    public int EquipmentId { get; set; }
    
    [ForeignKey("EquipmentId")]
    public virtual Equipment? Equipment { get; set; }

    [Display(Name = "Dự án")]
    public int? ProjectId { get; set; }
    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }

    [Display(Name = "Giai đoạn")]
    public int? StageId { get; set; }
    [ForeignKey("StageId")]
    public virtual Stage? Stage { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày kết thúc")]
    public DateTime? EndDate { get; set; }

    [StringLength(50)]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Đang điều động"; // Đang điều động, Đã hoàn thành
    
    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
}
