using System.Net;
using doanC_Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using doanC_Admin.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// ✅ PORT CHO RENDER
// ============================================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(int.Parse(port));
});

// ============================================
// 1. SERVICES
// ============================================
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

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

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

builder.Services.AddDbContext<FoodStreetGuideDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// ============================================
// 2. PIPELINE
// ============================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ❌ KHÔNG dùng HTTPS redirect trên Render
// app.UseHttpsRedirection();

app.UseStaticFiles();

// ============================================
// ✅ XỬ LÝ PATH 2 CHẾ ĐỘ (LOCAL + RENDER)
// ============================================

var currentDirectory = Directory.GetCurrentDirectory();

string mauiImagesPath;
string qrImagesPath;

// 👉 Nếu chạy LOCAL (có folder FoodStreetGuide)
if (Directory.Exists(Path.Combine(currentDirectory, "FoodStreetGuide")))
{
    var solutionDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory;

    mauiImagesPath = Path.Combine(solutionDirectory, "FoodStreetGuide", "Resources", "Images");
    qrImagesPath = Path.Combine(solutionDirectory, "FoodStreetGuide", "Resources", "qr");
}
else
{
    // 👉 Nếu chạy trên Render
    mauiImagesPath = Path.Combine(currentDirectory, "wwwroot", "images");
    qrImagesPath = Path.Combine(currentDirectory, "wwwroot", "qr");
}

// Tạo folder nếu chưa có
Directory.CreateDirectory(mauiImagesPath);
Directory.CreateDirectory(qrImagesPath);

// Serve images
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(mauiImagesPath),
    RequestPath = "/images"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(qrImagesPath),
    RequestPath = "/qr"
});

// ============================================
// ROUTING
// ============================================

app.UseRouting();
app.UseSession();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapHub<DashboardHub>("/dashboardHub");
app.MapRazorPages();
app.MapControllers();

// ============================================
// RUN
// ============================================

app.Run();

// ============================================
// HELPER
// ============================================

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