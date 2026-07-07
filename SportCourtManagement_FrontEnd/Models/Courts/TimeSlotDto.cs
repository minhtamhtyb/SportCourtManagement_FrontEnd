namespace SportCourtManagement_FrontEnd.Models.Courts;

public class TimeSlotDto
{
    public int SlotId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string DayType { get; set; } = string.Empty;
}
