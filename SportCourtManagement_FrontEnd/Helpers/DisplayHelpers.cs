namespace SportCourtManagement_FrontEnd.Helpers;

public static class DisplayHelpers
{
    public static string FormatCurrency(decimal amount) =>
        string.Format(System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}₫", amount);

    public static string FormatDate(DateTime? date) =>
        date?.ToString("dd/MM/yyyy") ?? "—";

    public static (string Label, string CssClass) GetCourtStatus(string status) => status switch
    {
        "Available" => ("Hoạt động", "status-available"),
        "Booked" => ("Đã đặt", "status-booked"),
        "InUse" => ("Đang sử dụng", "status-inuse"),
        "Maintenance" => ("Bảo trì", "status-maintenance"),
        "Inactive" => ("Ngưng hoạt động", "status-inactive"),
        "Active" => ("Hoạt động", "status-available"),
        _ => (status, "status-inactive")
    };

    public static (string Label, string CssClass) GetServiceCategory(string category) => category switch
    {
        "Equipment" => ("Dụng cụ", "cat-equipment"),
        "Drink" => ("Đồ uống", "cat-drink"),
        "Coach" => ("Huấn luyện", "cat-coach"),
        "Event" => ("Sự kiện", "cat-event"),
        _ => (category, "cat-equipment")
    };

    public static string GetCourtTypeBadgeClass(int courtTypeId) => courtTypeId switch
    {
        1 => "type-pickleball",
        2 => "type-badminton",
        3 => "type-football",
        4 => "type-tennis",
        5 => "type-basketball",
        _ => "type-default"
    };
}
