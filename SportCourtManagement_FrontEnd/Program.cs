using Microsoft.AspNetCore.Authentication.Cookies;
using SportCourtManagement_FrontEnd.Models.Configuration;
using SportCourtManagement_FrontEnd.Services;
using SportCourtManagement_FrontEnd.Services.Implementations;
using SportCourtManagement_FrontEnd.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection(ApiSettings.SectionName));
builder.Services.AddSingleton<MockDataStore>();

var useMock = builder.Configuration.GetValue<bool>($"{ApiSettings.SectionName}:UseMockData", true);
if (useMock)
{
    builder.Services.AddScoped<IAuthService, MockAuthService>();
    builder.Services.AddScoped<ICourtService, MockCourtService>();
    builder.Services.AddScoped<IServiceCatalogService, MockServiceCatalogService>();
    builder.Services.AddScoped<IReportService, MockReportService>();
    builder.Services.AddScoped<IUserService, MockUserService>();
    builder.Services.AddScoped<IRoleService, MockRoleService>();
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrStaff", policy => policy.RequireRole("Admin", "Staff"));
});

builder.Services.AddControllersWithViews();
builder.Services.AddSession();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
