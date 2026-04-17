using System.Net;
using doanC_Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using doanC_Admin.Hubs;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://*:{port}");

// ============================================
// 1. CẤU HÌNH SERVICES (TRƯỚC KHI BUILD)
// ============================================
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
// Add services
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddControllers();
builder.Services.AddSignalR();

// ✅ THÊM IHttpContextAccessor
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// CORS - Cho phép MAUI app gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Database Context
builder.Services.AddDbContext<FoodStreetGuideDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================
// 2. CẤU HÌNH KESTREL (PORT & IP)
// ============================================
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Listen on all IP addresses, port 5225
    serverOptions.Listen(IPAddress.Any, 5225);
    serverOptions.Listen(IPAddress.Loopback, 5225);
    serverOptions.Listen(IPAddress.IPv6Any, 5225);
});

var app = builder.Build();

// ============================================
// 3. CẤU HÌNH PIPELINE (SAU KHI BUILD)
// ============================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 👉 PHỤC VỤ FILE TĨNH (wwwroot)
app.UseStaticFiles();

// 👉 PHỤC VỤ ẢNH TỪ THƯ MỤC IMAGES CỦA MAUI APP (ĐƯỜNG DẪN TƯƠNG ĐỐI)
var currentDirectory = Directory.GetCurrentDirectory(); // .../doanC_Admin
var solutionDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory; // .../FoodStreetGuide_Solution
var mauiImagesPath = Path.Combine(solutionDirectory, "FoodStreetGuide", "Resources", "Images");
var qrImagesPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "FoodStreetGuide", "Resources", "qr");

// Tạo thư mục nếu chưa tồn tại
if (!Directory.Exists(mauiImagesPath))
{
    Directory.CreateDirectory(mauiImagesPath);
    Console.WriteLine($"📁 Created images directory: {mauiImagesPath}");
}

if (!Directory.Exists(qrImagesPath))
{
    Directory.CreateDirectory(qrImagesPath);
    Console.WriteLine($"📁 Created QR directory: {qrImagesPath}");
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(qrImagesPath),
    RequestPath = "/qr"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(mauiImagesPath),
    RequestPath = "/images"
});

app.UseRouting();
app.UseSession();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapHub<DashboardHub>("/dashboardHub");

// ✅ THÊM SIGNALR ENDPOINT
app.MapRazorPages();
app.MapControllers();

// ============================================
// 4. HIỂN THỊ THÔNG TIN KHỞI ĐỘNG
// ============================================

var localIP = GetLocalIPAddress();
port = Environment.GetEnvironmentVariable("PORT") ?? "5225";

var urls = new[]
{
    $"http://0.0.0.0:{port}"
};

app.Run();

// ============================================
// 5. HÀM TIỆN ÍCH
// ============================================

/// <summary>
/// Lấy địa chỉ IP cục bộ của máy tính
/// </summary>
static string GetLocalIPAddress()
{
    try
    {
        var hostName = Dns.GetHostName();
        var hostEntry = Dns.GetHostEntry(hostName);

        foreach (var ip in hostEntry.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(ip))
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
    catch
    {
        return "127.0.0.1";
    }
}