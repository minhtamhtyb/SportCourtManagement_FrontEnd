using System;

namespace SportCourtManagement_FrontEnd.Models.Bookings
{
    public class PromotionDto
    {
        public int PromotionId { get; set; }
        public string PromoCode { get; set; } = null!;
        public string PromoName { get; set; } = null!;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = null!; // "Percent" or "FixedAmount"
        public decimal DiscountValue { get; set; }
        public decimal MinOrderAmount { get; set; }
        public decimal? MaxDiscount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
