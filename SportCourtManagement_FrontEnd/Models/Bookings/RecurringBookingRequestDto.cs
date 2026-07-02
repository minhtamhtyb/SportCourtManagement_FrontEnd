using System;
using System.Collections.Generic;

namespace SportCourtManagement_FrontEnd.Models.Bookings
{
    public class RecurringBookingRequestDto
    {
        public int CourtId { get; set; }
        public int SlotId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public List<int> DaysOfWeek { get; set; } = new();
        public string? PromotionCode { get; set; }
        public string? Note { get; set; }
    }
}
