using System.Net;
using doanC_Admin.Filters;
using doanC_Admin.Hubs;
using doanC_Admin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "5225";

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
builder.Services.AddScoped<SessionFilter>();
builder.Services.AddMvc(options =>
{
    options.Filters.Add<SessionFilter>();
});

// ============================================
// ✅ CẤU HÌNH DATABASE THEO MÔI TRƯỜNG
// ============================================
var isProduction = builder.Environment.IsProduction();

// ============================================
// 🔧 TẠM THỜI COMMENT CODE POSTGRESQL
// CHỈ DÙNG SQL SERVER
// ============================================
// if (isProduction)
// {
//     builder.Services.AddDbContext<FoodStreetGuideDBContext>(options =>
//         options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// }
// else
// {
//     builder.Services.AddDbContext<FoodStreetGuideDBContext>(options =>
//         options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// }

// ✅ TẠM THỜI CHỈ DÙNG SQL SERVER
builder.Services.AddDbContext<FoodStreetGuideDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// ============================================
// AUTO SEED DATA (CHỈ 1 LẦN)
// ============================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FoodStreetGuideDBContext>();

    try
    {
        // Kiểm tra kết nối
        dbContext.Database.CanConnect();
        Console.WriteLine("✅ Database connected!");

        // ============================================
        // 🔧 COMMENT ĐOẠN TẠO BẢNG POSTGRESQL
        // ============================================
        // Tạo bảng nếu chưa có (dùng SQL thuần)
        // await dbContext.Database.ExecuteSqlRawAsync(@"
        //     CREATE TABLE IF NOT EXISTS ""AdminUsers"" (
        //         ""AdminId"" SERIAL PRIMARY KEY,
        //         ""Username"" VARCHAR(50) NOT NULL UNIQUE,
        //         ""PasswordHash"" VARCHAR(255) NOT NULL,
        //         ""FullName"" VARCHAR(100),
        //         ""Email"" VARCHAR(100),
        //         ""Role"" VARCHAR(20) DEFAULT 'Admin',
        //         ""IsActive"" BOOLEAN DEFAULT TRUE,
        //         ""LastLogin"" TIMESTAMP,
        //         ""LastLogout"" TIMESTAMP,
        //         ""CreatedAt"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        //         ""UpdatedAt"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        //     );
        // ");

        // ✅ TẠM THỜI DÙNG CÁCH NÀY CHO SQL SERVER
        // Tạo bảng bằng Entity Framework (nếu chưa có)
        await dbContext.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ Database schema ensured!");

        // Kiểm tra và thêm dữ liệu
        var adminCount = dbContext.AdminUsers.Count();

        if (adminCount == 0)
        {
            Console.WriteLine("🌱 Seeding data...");

            dbContext.AdminUsers.AddRange(
                new AdminUser
                {
                    Username = "admin",
                    PasswordHash = "123456",
                    FullName = "Quản trị viên hệ thống",
                    Email = "admin@foodstreet.com",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new AdminUser
                {
                    Username = "manager",
                    PasswordHash = "123456",
                    FullName = "Chủ quán Vĩnh Khánh",
                    Email = "manager@foodstreet.com",
                    Role = "Manager",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            );

            await dbContext.SaveChangesAsync();
            Console.WriteLine("✅ Seed data completed!");
        }
        else
        {
            Console.WriteLine($"✅ Database already has {adminCount} admin users.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database error: {ex.Message}");
    }
}

Console.WriteLine("✅ Database initialization complete!");

// ============================================
// 2. PIPELINE
// ============================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

var currentDirectory = Directory.GetCurrentDirectory();

string mauiImagesPath;
string qrImagesPath;

if (Directory.Exists(Path.Combine(currentDirectory, "FoodStreetGuide")))
{
    var solutionDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory;
    mauiImagesPath = Path.Combine(solutionDirectory, "FoodStreetGuide", "Resources", "Images");
    qrImagesPath = Path.Combine(solutionDirectory, "FoodStreetGuide", "Resources", "qr");
}
else
{
    mauiImagesPath = Path.Combine(currentDirectory, "wwwroot", "images");
    qrImagesPath = Path.Combine(currentDirectory, "wwwroot", "qr");
}

Directory.CreateDirectory(mauiImagesPath);
Directory.CreateDirectory(qrImagesPath);

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

app.UseRouting();
app.UseSession();
app.UseCors("AllowAll");
// ========== MIDDLEWARE KIỂM TRA SESSION ==========
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    // Danh sách đường dẫn công khai
    var publicPaths = new[] { "/Login", "/Index", "/Error", "/AccessDenied", "/css", "/js", "/lib", "/favicon", "/swagger", "/api" };

    if (publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) || path == "/")
    {
        await next();
        return;
    }

    var adminId = context.Session.GetString("AdminId");

    // Chưa đăng nhập
    if (string.IsNullOrEmpty(adminId))
    {
        context.Response.Redirect("/Login");
        return;
    }

    var role = context.Session.GetString("Role") ?? "";

    // Owner chỉ được vào /Owner/*
    if (role == "Owner" && !path.StartsWith("/Owner", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Owner/Dashboard");
        return;
    }

    // Admin không được vào /Owner/*
    if ((role == "Admin" || role == "SuperAdmin") && path.StartsWith("/Owner", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Dashboard");
        return;
    }

    await next();
});
app.UseAuthorization();

app.MapHub<DashboardHub>("/dashboardHub");
app.MapRazorPages();
app.MapControllers();

app.Run();

// ============================================
// 🔧 COMMENT HÀM GetLocalIPAddress (chưa dùng)
// ============================================
// static string GetLocalIPAddress()
// {
//     try
//     {
//         var hostName = Dns.GetHostName();
//         var hostEntry = Dns.GetHostEntry(hostName);
//         foreach (var ip in hostEntry.AddressList)
//         {
//             if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
//                 !IPAddress.IsLoopback(ip))
//             {
//                 return ip.ToString();
//             }
//         }
//         return "127.0.0.1";
//     }
//     catch
//     {
//         return "127.0.0.1";
//     }
// }