using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SportCourtManagement_FrontEnd.Models
{
    public enum ShiftType
    {
        Morning,
        Afternoon,
        Evening
    }

    public class CreateShiftRequest
    {
        [Required(ErrorMessage = "StaffId không được để trống.")]
        public int? StaffId { get; set; }

        [Required(ErrorMessage = "Ngày trực không được để trống.")]
        public DateOnly? ShiftDate { get; set; }

        [Required(ErrorMessage = "Loại ca không được để trống.")]
        public ShiftType? ShiftType { get; set; }

        [MaxLength(300, ErrorMessage = "Ghi chú không được vượt quá 300 ký tự.")]
        public string? Note { get; set; }
    }

    public class UpdateShiftRequest
    {
        [Required(ErrorMessage = "Loại ca không được để trống.")]
        public ShiftType? ShiftType { get; set; }

        [MaxLength(300, ErrorMessage = "Ghi chú không được vượt quá 300 ký tự.")]
        public string? Note { get; set; }
    }
}
