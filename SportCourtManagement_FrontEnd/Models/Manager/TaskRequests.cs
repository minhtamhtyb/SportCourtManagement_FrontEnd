using System;
using System.ComponentModel.DataAnnotations;

namespace SportCourtManagement_FrontEnd.Models.Manager
{
    public enum TaskCategory
    {
        Cleanup,
        ServicePrep,
        Repair,
        Complaint
    }

    public enum TaskPriority
    {
        Urgent,
        High,
        Medium,
        Low
    }

    public enum TaskItemStatus
    {
        Pending,
        InProgress,
        Completed,
        Approved
    }

    public class CreateTaskRequest
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [MaxLength(150, ErrorMessage = "Tiêu đề không được vượt quá 150 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Danh mục không được để trống.")]
        public TaskCategory? Category { get; set; }

        [Required(ErrorMessage = "Độ ưu tiên không được để trống.")]
        public TaskPriority? Priority { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhân viên thực hiện.")]
        [Range(1, int.MaxValue, ErrorMessage = "Nhân viên thực hiện không hợp lệ.")]
        public int? AssignedStaffId { get; set; }

        public int? BookingId { get; set; }

        [Required(ErrorMessage = "Thời hạn không được để trống.")]
        public DateTime? DueDate { get; set; }
    }

    public class UpdateTaskRequest
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [MaxLength(150, ErrorMessage = "Tiêu đề không được vượt quá 150 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Danh mục không được để trống.")]
        public TaskCategory? Category { get; set; }

        [Required(ErrorMessage = "Độ ưu tiên không được để trống.")]
        public TaskPriority? Priority { get; set; }

        public int? AssignedStaffId { get; set; }

        public int? BookingId { get; set; }

        [Required(ErrorMessage = "Thời hạn không được để trống.")]
        public DateTime? DueDate { get; set; }
    }

    public class VerifyTaskRequest
    {
        [Required(ErrorMessage = "Vui lòng xác định duyệt hay từ chối.")]
        public bool? IsApproved { get; set; }

        [MaxLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        public string? Note { get; set; }
    }
}
