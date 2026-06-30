namespace SportCourtManagement_FrontEnd.Models.Bookings;

public class PaymentRequestDto
{
    public int BookingId { get; set; }
    public string PaymentMethod { get; set; } = "VNPay";
}
