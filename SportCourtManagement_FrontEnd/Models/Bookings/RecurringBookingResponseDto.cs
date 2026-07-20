using System;
using System.Collections.Generic;

namespace SportCourtManagement_FrontEnd.Models.Bookings
{
    public class RecurringBookingResponseDto
    {
        public int RecurringId { get; set; }
        public int CourtId { get; set; }
        public string CourtName { get; set; } = null!;
        public int SlotId { get; set; }
        public string SlotName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DaysOfWeek { get; set; } = null!;
        public string Status { get; set; } = null!;
        public List<BookingDetailDto> CreatedBookings { get; set; } = new();
        public List<string> ConflictDates { get; set; } = new();
        public int TotalRequestedSessions { get; set; }
        public int TotalBookedSessions { get; set; }
        public decimal TotalEstimatedAmount { get; set; }
        public bool HasConflicts => ConflictDates.Count > 0;
    }
}
