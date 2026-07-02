namespace SportCourtManagement_FrontEnd.Models.Courts;

public class CourtTypeDto
{
    public int CourtTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int CourtCount { get; set; }
}
