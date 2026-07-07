namespace SportCourtManagement_FrontEnd.Models.Services;

public class ServiceDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQty { get; set; }
}
