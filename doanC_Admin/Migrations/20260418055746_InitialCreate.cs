using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace doanC_Admin.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    AdminId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Admin"),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLogout = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.AdminId);
                });

            migrationBuilder.CreateTable(
                name: "DeletedLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PointId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedLogs", x => x.LogId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceTracking",
                columns: table => new
                {
                    DeviceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceUniqueId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OSVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AppVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastActivity = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FirstSeen = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    LastLocationLat = table.Column<double>(type: "float", nullable: true),
                    LastLocationLng = table.Column<double>(type: "float", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TotalScans = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalListens = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTracking", x => x.DeviceId);
                });

            migrationBuilder.CreateTable(
                name: "GeoFenceLogs",
                columns: table => new
                {
                    GeoLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PointId = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EnterTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeoFenceLogs", x => x.GeoLogId);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    LanguageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LanguageName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FlagIcon = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.LanguageId);
                });

            migrationBuilder.CreateTable(
                name: "TTSLogs",
                columns: table => new
                {
                    TtsLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PointId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TTSLogs", x => x.TtsLogId);
                });

            migrationBuilder.CreateTable(
                name: "AdminLoginLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    LogoutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Success"),
                    FailureReason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminLoginLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_AdminLoginLogs_AdminUsers_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminSessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    LastActivity = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    SessionToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 30)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_AdminSessions_AdminUsers_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreOwners",
                columns: table => new
                {
                    OwnerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IdentityNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreOwners", x => x.OwnerId);
                    table.ForeignKey(
                        name: "FK_StoreOwners_AdminUsers_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreOwners_AdminUsers_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocationPoints",
                columns: table => new
                {
                    PointId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Radius = table.Column<double>(type: "float", nullable: true),
                    AudioFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rating = table.Column<double>(type: "float", nullable: true),
                    ReviewCount = table.Column<int>(type: "int", nullable: true),
                    OpeningHours = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PriceRange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationPoints", x => x.PointId);
                    table.ForeignKey(
                        name: "FK_LocationPoints_AdminUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId");
                    table.ForeignKey(
                        name: "FK_LocationPoints_StoreOwners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "StoreOwners",
                        principalColumn: "OwnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AudioFiles",
                columns: table => new
                {
                    AudioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PointId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioFiles", x => x.AudioId);
                    table.ForeignKey(
                        name: "FK_AudioFiles_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "LanguageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AudioFiles_LocationPoints_PointId",
                        column: x => x.PointId,
                        principalTable: "LocationPoints",
                        principalColumn: "PointId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QRCodes",
                columns: table => new
                {
                    QrId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PointId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    QrContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QrImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRCodes", x => x.QrId);
                    table.ForeignKey(
                        name: "FK_QRCodes_LocationPoints_PointId",
                        column: x => x.PointId,
                        principalTable: "LocationPoints",
                        principalColumn: "PointId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QRScanLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PointId = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ScanTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRScanLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_QRScanLogs_LocationPoints_PointId",
                        column: x => x.PointId,
                        principalTable: "LocationPoints",
                        principalColumn: "PointId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginLogs_AdminId",
                table: "AdminLoginLogs",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminSessions_AdminId",
                table: "AdminSessions",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_LanguageId",
                table: "AudioFiles",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_PointId",
                table: "AudioFiles",
                column: "PointId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTracking_DeviceUniqueId",
                table: "DeviceTracking",
                column: "DeviceUniqueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTracking_LastActivity",
                table: "DeviceTracking",
                column: "LastActivity");

            migrationBuilder.CreateIndex(
                name: "IX_LocationPoints_CreatedBy",
                table: "LocationPoints",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LocationPoints_OwnerId",
                table: "LocationPoints",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_PointId",
                table: "QRCodes",
                column: "PointId");

            migrationBuilder.CreateIndex(
                name: "IX_QRScanLogs_PointId",
                table: "QRScanLogs",
                column: "PointId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreOwners_AdminId",
                table: "StoreOwners",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreOwners_ApprovedBy",
                table: "StoreOwners",
                column: "ApprovedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminLoginLogs");

            migrationBuilder.DropTable(
                name: "AdminSessions");

            migrationBuilder.DropTable(
                name: "AudioFiles");

            migrationBuilder.DropTable(
                name: "DeletedLogs");

            migrationBuilder.DropTable(
                name: "DeviceTracking");

            migrationBuilder.DropTable(
                name: "GeoFenceLogs");

            migrationBuilder.DropTable(
                name: "QRCodes");

            migrationBuilder.DropTable(
                name: "QRScanLogs");

            migrationBuilder.DropTable(
                name: "TTSLogs");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "LocationPoints");

            migrationBuilder.DropTable(
                name: "StoreOwners");

            migrationBuilder.DropTable(
                name: "AdminUsers");
        }
    }
}
