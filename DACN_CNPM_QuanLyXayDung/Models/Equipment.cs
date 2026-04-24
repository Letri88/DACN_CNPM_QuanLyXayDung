using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DACN_CNPM_QuanLyXayDung.Models;

public class Equipment
{
    [Key]
    public int EquipmentId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thiết bị")]
    [StringLength(150)]
    [Display(Name = "Tên thiết bị")]
    public string EquipmentName { get; set; }

    [StringLength(50)]
    [Display(Name = "Mã thiết bị")]
    public string EquipmentCode { get; set; }

    [StringLength(100)]
    [Display(Name = "Loại thiết bị")]
    public string Type { get; set; }

    [StringLength(50)]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Sẵn sàng"; // Sẵn sàng, Đang hoạt động, Đang bảo trì, Hỏng

    [DataType(DataType.Date)]
    [Display(Name = "Ngày mua")]
    public DateTime? PurchaseDate { get; set; }

    [Display(Name = "Đơn giá (Ca/Giờ)")]
    public decimal? HourlyRate { get; set; }

    public virtual ICollection<EquipmentDispatch> EquipmentDispatches { get; set; } = new List<EquipmentDispatch>();
}
