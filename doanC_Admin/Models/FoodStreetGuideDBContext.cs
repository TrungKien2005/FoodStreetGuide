using Microsoft.EntityFrameworkCore;
using doanC_Admin.Models;

namespace doanC_Admin.Models
{
    public class FoodStreetGuideDBContext : DbContext
    {
        public FoodStreetGuideDBContext(DbContextOptions<FoodStreetGuideDBContext> options)
            : base(options)
        {
        }

        // ========== DBSETS HIỆN CÓ ==========
        public DbSet<LocationPoint> LocationPoints { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<AudioFile> AudioFiles { get; set; }
        public DbSet<QRScanLog> QRScanLogs { get; set; }
        public DbSet<GeoFenceLog> GeoFenceLogs { get; set; }
        public DbSet<TTSLog> TTSLogs { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<QRCode> QRCodes { get; set; }
        public DbSet<AdminLoginLog> AdminLoginLogs { get; set; }
        public DbSet<AdminSession> AdminSessions { get; set; }
        public DbSet<DeletedLog> DeletedLogs { get; set; }
        public DbSet<StoreOwner> StoreOwners { get; set; }
        public DbSet<DeviceTracking> DeviceTracking { get; set; }  // 👈 GIỮ NGUYÊN

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== CẤU HÌNH LocationPoint ==========
            modelBuilder.Entity<LocationPoint>(entity =>
            {
                entity.ToTable("LocationPoints");
                entity.HasKey(e => e.PointId);
                entity.Property(e => e.PointId).HasColumnName("PointId").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnName("Name").IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasColumnName("Description");
                entity.Property(e => e.Latitude).HasColumnName("Latitude");
                entity.Property(e => e.Longitude).HasColumnName("Longitude");
                entity.Property(e => e.Radius).HasColumnName("Radius");
                entity.Property(e => e.AudioFile).HasColumnName("AudioFile");
                entity.Property(e => e.Language).HasColumnName("Language");
                entity.Property(e => e.Address).HasColumnName("Address");
                entity.Property(e => e.Category).HasColumnName("Category");
                entity.Property(e => e.Image).HasColumnName("Image");
                entity.Property(e => e.Rating).HasColumnName("Rating");
                entity.Property(e => e.ReviewCount).HasColumnName("ReviewCount");
                entity.Property(e => e.OpeningHours).HasColumnName("OpeningHours");
                entity.Property(e => e.PriceRange).HasColumnName("PriceRange");
                entity.Property(e => e.OwnerId).HasColumnName("OwnerId");
                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsApproved).HasColumnName("IsApproved").HasDefaultValue(false);
                entity.Property(e => e.ApprovedBy).HasColumnName("ApprovedBy");
                entity.Property(e => e.ApprovedAt).HasColumnName("ApprovedAt");

                // Relationships
                entity.HasOne(e => e.StoreOwner)
                      .WithMany()
                      .HasForeignKey(e => e.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== CẤU HÌNH DeviceTracking (TÁCH RIÊNG) ==========
            modelBuilder.Entity<DeviceTracking>(entity =>
            {
                entity.ToTable("DeviceTracking");
                entity.HasKey(e => e.DeviceId);
                entity.Property(e => e.DeviceId).HasColumnName("DeviceId").ValueGeneratedOnAdd();
                entity.Property(e => e.DeviceUniqueId).HasColumnName("DeviceUniqueId").IsRequired().HasMaxLength(255);
                entity.Property(e => e.DeviceName).HasColumnName("DeviceName").HasMaxLength(255);
                entity.Property(e => e.Platform).HasColumnName("Platform").HasMaxLength(50);
                entity.Property(e => e.OSVersion).HasColumnName("OSVersion").HasMaxLength(50);
                entity.Property(e => e.AppVersion).HasColumnName("AppVersion").HasMaxLength(50);
                entity.Property(e => e.LastActivity).HasColumnName("LastActivity").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.FirstSeen).HasColumnName("FirstSeen").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.LastLocationLat).HasColumnName("LastLocationLat");
                entity.Property(e => e.LastLocationLng).HasColumnName("LastLocationLng");
                entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
                entity.Property(e => e.TotalScans).HasColumnName("TotalScans").HasDefaultValue(0);
                entity.Property(e => e.TotalListens).HasColumnName("TotalListens").HasDefaultValue(0);

                entity.HasIndex(e => e.DeviceUniqueId).IsUnique();
                entity.HasIndex(e => e.LastActivity);
            });

            // ========== CẤU HÌNH AdminUser ==========
            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.ToTable("AdminUsers");
                entity.HasKey(e => e.AdminId);
                entity.Property(e => e.AdminId).HasColumnName("AdminId").ValueGeneratedOnAdd();
                entity.Property(e => e.Username).HasColumnName("Username").IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).HasColumnName("PasswordHash").IsRequired();
                entity.Property(e => e.FullName).HasColumnName("FullName").HasMaxLength(100);
                entity.Property(e => e.Email).HasColumnName("Email").HasMaxLength(100);
                entity.Property(e => e.Role).HasColumnName("Role").HasMaxLength(20).HasDefaultValue("Admin");
                entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
                entity.Property(e => e.LastLogin).HasColumnName("LastLogin");
                entity.Property(e => e.LastLogout).HasColumnName("LastLogout");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql("GETDATE()");
            });

            // ========== CẤU HÌNH StoreOwner ==========
            modelBuilder.Entity<StoreOwner>(entity =>
            {
                entity.ToTable("StoreOwners");
                entity.HasKey(e => e.OwnerId);
                entity.Property(e => e.OwnerId).HasColumnName("OwnerId").ValueGeneratedOnAdd();
                entity.Property(e => e.AdminId).HasColumnName("AdminId").IsRequired();
                entity.Property(e => e.PhoneNumber).HasColumnName("PhoneNumber").HasMaxLength(20);
                entity.Property(e => e.IdentityNumber).HasColumnName("IdentityNumber").HasMaxLength(20);
                entity.Property(e => e.TaxCode).HasColumnName("TaxCode").HasMaxLength(50);
                entity.Property(e => e.BankAccount).HasColumnName("BankAccount").HasMaxLength(50);
                entity.Property(e => e.ApprovedBy).HasColumnName("ApprovedBy");
                entity.Property(e => e.ApprovedAt).HasColumnName("ApprovedAt");
                entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20).HasDefaultValue("Pending");

                entity.HasOne(e => e.AdminUser)
                      .WithMany()
                      .HasForeignKey(e => e.AdminId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Approver)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== CẤU HÌNH QRCode ==========
            modelBuilder.Entity<QRCode>(entity =>
            {
                entity.ToTable("QRCodes");
                entity.HasKey(e => e.QrId);
                entity.Property(e => e.QrId).HasColumnName("QrId").ValueGeneratedOnAdd();
                entity.Property(e => e.PointId).HasColumnName("PointId").IsRequired();
                entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(255);
                entity.Property(e => e.QrContent).HasColumnName("QrContent");
                entity.Property(e => e.QrImagePath).HasColumnName("QrImagePath");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("GETDATE()");
            });

            // ========== CẤU HÌNH AudioFile ==========
            modelBuilder.Entity<AudioFile>(entity =>
            {
                entity.ToTable("AudioFiles");
                entity.HasKey(e => e.AudioId);
                entity.Property(e => e.AudioId).HasColumnName("AudioId").ValueGeneratedOnAdd();
                entity.Property(e => e.PointId).HasColumnName("PointId").IsRequired();
                entity.Property(e => e.LanguageId).HasColumnName("LanguageId").IsRequired();
                entity.Property(e => e.FileName).HasColumnName("FileName").HasMaxLength(255);
                entity.Property(e => e.FilePath).HasColumnName("FilePath").HasMaxLength(255);
                entity.Property(e => e.Duration).HasColumnName("Duration");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("GETDATE()");
            });

            // ========== CẤU HÌNH QRScanLog ==========
            modelBuilder.Entity<QRScanLog>(entity =>
            {
                entity.ToTable("QRScanLogs");
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.LogId).HasColumnName("LogId").ValueGeneratedOnAdd();
                entity.Property(e => e.PointId).HasColumnName("PointId").IsRequired();
                entity.Property(e => e.DeviceId).HasColumnName("DeviceId").HasMaxLength(100);
                entity.Property(e => e.ScanTime).HasColumnName("ScanTime").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Latitude).HasColumnName("Latitude");
                entity.Property(e => e.Longitude).HasColumnName("Longitude");
            });

            // ========== CẤU HÌNH GeoFenceLog ==========
            modelBuilder.Entity<GeoFenceLog>(entity =>
            {
                entity.ToTable("GeoFenceLogs");
                entity.HasKey(e => e.GeoLogId);
                entity.Property(e => e.GeoLogId).HasColumnName("GeoLogId").ValueGeneratedOnAdd();
                entity.Property(e => e.PointId).HasColumnName("PointId").IsRequired();
                entity.Property(e => e.DeviceId).HasColumnName("DeviceId").HasMaxLength(100);
                entity.Property(e => e.EnterTime).HasColumnName("EnterTime");
                entity.Property(e => e.ExitTime).HasColumnName("ExitTime");
                entity.Property(e => e.DurationSeconds).HasColumnName("DurationSeconds");
            });

            // ========== CẤU HÌNH TTSLog ==========
            modelBuilder.Entity<TTSLog>(entity =>
            {
                entity.ToTable("TTSLogs");
                entity.HasKey(e => e.TtsLogId);
                entity.Property(e => e.TtsLogId).HasColumnName("TtsLogId").ValueGeneratedOnAdd();
                entity.Property(e => e.PointId).HasColumnName("PointId").IsRequired();
                entity.Property(e => e.LanguageId).HasColumnName("LanguageId").IsRequired();
                entity.Property(e => e.PlayedAt).HasColumnName("PlayedAt").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.DurationSeconds).HasColumnName("DurationSeconds");
            });

            // ========== CẤU HÌNH Language ==========
            modelBuilder.Entity<Language>(entity =>
            {
                entity.ToTable("Languages");
                entity.HasKey(e => e.LanguageId);
                entity.Property(e => e.LanguageId).HasColumnName("LanguageId").ValueGeneratedOnAdd();
                entity.Property(e => e.LanguageCode).HasColumnName("LanguageCode").IsRequired().HasMaxLength(10);
                entity.Property(e => e.LanguageName).HasColumnName("LanguageName").IsRequired().HasMaxLength(50);
                entity.Property(e => e.FlagIcon).HasColumnName("FlagIcon").HasMaxLength(10);
                entity.Property(e => e.DisplayOrder).HasColumnName("DisplayOrder").HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true);
            });

            // ========== CẤU HÌNH AdminLoginLog ==========
            modelBuilder.Entity<AdminLoginLog>(entity =>
            {
                entity.ToTable("AdminLoginLogs");
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.LogId).HasColumnName("LogId").ValueGeneratedOnAdd();
                entity.Property(e => e.AdminId).HasColumnName("AdminId").IsRequired();
                entity.Property(e => e.Username).HasColumnName("Username").HasMaxLength(50);
                entity.Property(e => e.LoginTime).HasColumnName("LoginTime").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.LogoutTime).HasColumnName("LogoutTime");
                entity.Property(e => e.IPAddress).HasColumnName("IPAddress").HasMaxLength(50);
                entity.Property(e => e.DeviceInfo).HasColumnName("DeviceInfo").HasMaxLength(255);
                entity.Property(e => e.Status).HasColumnName("Status").HasMaxLength(20).HasDefaultValue("Success");
                entity.Property(e => e.FailureReason).HasColumnName("FailureReason").HasMaxLength(255);

                entity.HasOne(e => e.AdminUser)
                      .WithMany()
                      .HasForeignKey(e => e.AdminId);
            });

            // ========== CẤU HÌNH AdminSession ==========
            modelBuilder.Entity<AdminSession>(entity =>
            {
                entity.ToTable("AdminSessions");
                entity.HasKey(e => e.SessionId);
                entity.Property(e => e.SessionId).HasColumnName("SessionId").ValueGeneratedOnAdd();
                entity.Property(e => e.AdminId).HasColumnName("AdminId").IsRequired();
                entity.Property(e => e.Username).HasColumnName("Username").HasMaxLength(50);
                entity.Property(e => e.LoginTime).HasColumnName("LoginTime").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.LastActivity).HasColumnName("LastActivity").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.LastHeartbeat).HasColumnName("LastHeartbeat").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.SessionToken).HasColumnName("SessionToken").HasMaxLength(255);
                entity.Property(e => e.IPAddress).HasColumnName("IPAddress").HasMaxLength(50);
                entity.Property(e => e.DeviceInfo).HasColumnName("DeviceInfo").HasMaxLength(255);
                entity.Property(e => e.UserAgent).HasColumnName("UserAgent").HasMaxLength(500);
                entity.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(1);
                entity.Property(e => e.SessionTimeoutMinutes).HasColumnName("SessionTimeoutMinutes").HasDefaultValue(30);

                entity.HasOne(e => e.AdminUser)
                      .WithMany()
                      .HasForeignKey(e => e.AdminId);
            });
        }
    }
}