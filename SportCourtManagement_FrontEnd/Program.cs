using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SportCourtManagement_FrontEnd.Models.Configuration;
using SportCourtManagement_FrontEnd.Services;
using SportCourtManagement_FrontEnd.Services.Api;
using SportCourtManagement_FrontEnd.Services.Implementations;
using SportCourtManagement_FrontEnd.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection(ApiSettings.SectionName));
builder.Services.AddHttpContextAccessor();

var useMock = builder.Configuration.GetValue<bool>($"{ApiSettings.SectionName}:UseMockData", true);
if (useMock)
{
    builder.Services.AddSingleton<MockDataStore>();
    builder.Services.AddScoped<IAuthService, MockAuthService>();
    builder.Services.AddScoped<ICourtService, MockCourtService>();
    builder.Services.AddScoped<IServiceCatalogService, MockServiceCatalogService>();
    builder.Services.AddScoped<IComplexServiceOfferingService, MockComplexServiceOfferingService>();
    builder.Services.AddScoped<IReportService, MockReportService>();
    builder.Services.AddScoped<IUserService, MockUserService>();
    builder.Services.AddScoped<IRoleService, MockRoleService>();
}
else
{
    var apiBaseUrl = builder.Configuration.GetValue<string>($"{ApiSettings.SectionName}:BaseUrl")
        ?? "http://localhost:5000";

    builder.Services.AddScoped<JwtForwardingHandler>();
    builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    }).AddHttpMessageHandler<JwtForwardingHandler>();

    builder.Services.AddScoped<IAuthService, ApiAuthService>();
    builder.Services.AddScoped<ICourtService, ApiCourtService>();
    builder.Services.AddScoped<IServiceCatalogService, ApiServiceCatalogService>();
    builder.Services.AddScoped<IComplexServiceOfferingService, ApiComplexServiceOfferingService>();
    builder.Services.AddScoped<IReportService, ApiReportService>();
    builder.Services.AddScoped<IUserService, ApiUserService>();
    builder.Services.AddScoped<IRoleService, ApiRoleService>();
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var token = context.Principal?.FindFirst(JwtForwardingHandler.AccessTokenClaimType)?.Value;
                if (string.IsNullOrWhiteSpace(token))
                    return;

                var session = context.HttpContext.Session;
                if (!session.IsAvailable)
                    return;

                await session.LoadAsync(context.HttpContext.RequestAborted);
                if (string.IsNullOrWhiteSpace(session.GetString(JwtForwardingHandler.SessionTokenKey)))
                    session.SetString(JwtForwardingHandler.SessionTokenKey, token);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AdminOrStaff", policy => policy.RequireRole("Admin", "Staff", "Coach"));
});

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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
