namespace SportCourtManagement_FrontEnd.Models.Configuration;

public class ApiSettings
{
    public const string SectionName = "ApiSettings";
    public string BaseUrl { get; set; } = "http://localhost:5203";
    public bool UseMockData { get; set; } = true;
}
