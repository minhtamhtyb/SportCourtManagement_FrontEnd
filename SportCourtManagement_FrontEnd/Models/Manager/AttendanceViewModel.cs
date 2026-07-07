using System.Collections.Generic;

namespace SportCourtManagement_FrontEnd.Models.Manager
{
    public class AttendanceViewModel
    {
        public List<StaffShiftResponse> Records { get; set; } = new();
        public List<StaffSummaryResponse> Staffs { get; set; } = new();
        public string DateFrom { get; set; } = string.Empty;
        public string DateTo { get; set; } = string.Empty;
        public int? SelectedStaffId { get; set; }
        public string? SelectedShiftType { get; set; }
        public string? SearchQuery { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        // Statistics
        public int OnTimeCount { get; set; } = 0;
        public int LateCount { get; set; } = 0;
    }
}
