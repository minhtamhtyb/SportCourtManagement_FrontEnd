namespace SportCourtManagement_FrontEnd.Models.Bookings;

public class SePayQrCodeDto
{
    public string BookingCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string BankBin { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
}
