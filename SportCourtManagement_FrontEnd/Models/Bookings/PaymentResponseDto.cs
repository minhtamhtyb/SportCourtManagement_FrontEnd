namespace SportCourtManagement_FrontEnd.Models.Bookings;

public class PaymentResponseDto
{
    public int BookingId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
}
