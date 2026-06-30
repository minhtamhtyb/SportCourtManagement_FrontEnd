using System;
using System.Collections.Generic;

namespace SportCourtManagement_FrontEnd.Models.Courts;

public class CourtAvailabilityDto
{
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public List<AvailabilitySlotDto> Slots { get; set; } = new();
}

public class AvailabilitySlotDto
{
    public int SlotId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = "Available"; // Available | Booked | Maintenance
}
