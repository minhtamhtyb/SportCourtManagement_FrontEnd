using System;
using System.Collections.Generic;

namespace SportCourtManagement_FrontEnd.Models.Manager
{
    public class MaintenanceResponse
    {
        public int MaintenanceId { get; set; }
        public int CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public int ComplexId { get; set; }
        public string ComplexName { get; set; } = string.Empty;
        public string MaintenanceType { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int? AssignedStaffId { get; set; }
        public string? AssignedStaffName { get; set; }
        public string? Reason { get; set; }
        public string? Result { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PagedMaintenanceResponse
    {
        public List<MaintenanceResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class CourtInfoResponse
    {
        public int CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public string CourtCode { get; set; } = string.Empty;
        public string CourtTypeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // View Model for the Maintenance View
    public class MaintenanceViewModel
    {
        public PagedMaintenanceResponse Schedules { get; set; } = new();
        public List<CourtInfoResponse> Courts { get; set; } = new();
        public List<StaffSummaryResponse> Staffs { get; set; } = new();

        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
    }
}
