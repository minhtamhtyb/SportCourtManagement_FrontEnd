using System.Collections.Generic;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.Bookings;

namespace SportCourtManagement_FrontEnd.Models.ViewModels
{
    public class HomePageViewModel
    {
        public List<CourtListDto> FeaturedCourts { get; set; } = new();
        public List<PromotionDto> ActivePromotions { get; set; } = new();
    }
}
