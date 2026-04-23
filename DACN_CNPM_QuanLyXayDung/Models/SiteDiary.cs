using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DACN_CNPM_QuanLyXayDung.Models;

public class SiteDiary
{
    [Key]
    public int DiaryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Dự án")]
    public int ProjectId { get; set; }
    
    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }

    public int? StageId { get; set; }
    [ForeignKey("StageId")]
    public virtual Stage? Stage { get; set; }

    [Required]
    [StringLength(50)]
    public string EngineerId { get; set; } = null!;
    
    [ForeignKey("EngineerId")]
    public virtual User? Engineer { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn Ngày báo cáo")]
    public DateTime Date { get; set; } = DateTime.Today;

    [StringLength(50)]
    public string? Weather { get; set; } // Nắng, Mưa, Râm mát...

    [StringLength(20)]
    public string? Temperature { get; set; } // VD: 32°C

    [Required(ErrorMessage = "Vui lòng nhập số lượng công nhân")]
    [Range(0, 1000, ErrorMessage = "Số lượng công nhân không hợp lệ")]
    public int WorkerCount { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng máy móc")]
    [Range(0, 1000, ErrorMessage = "Số lượng máy móc không hợp lệ")]
    public int MachineryCount { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung công việc hoàn thành")]
    public string WorkCompleted { get; set; } = null!;

    public string? Issues { get; set; }

    public byte[]? PhotoContent { get; set; }

    [StringLength(100)]
    public string? PhotoContentType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
