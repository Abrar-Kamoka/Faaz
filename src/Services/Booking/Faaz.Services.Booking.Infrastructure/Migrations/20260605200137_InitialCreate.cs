using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faaz.Services.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsultantUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsultantProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionTypeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    SessionPriceGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PlatformCommissionGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PromoDiscountGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalChargedGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CallType = table.Column<int>(type: "int", nullable: false),
                    ScheduledStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StudentTimezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SessionBrief = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SlotReservedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<int>(type: "int", nullable: true),
                    CancellationNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefundPercentage = table.Column<int>(type: "int", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PromoCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookingStatusHistory",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingStatusHistory_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefundAppeals",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAmountGbp = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundAppeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundAppeals_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LiveKitRoomName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LiveKitRoomSid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RoomCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    CompletionPct = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CreateRoomJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NoShowJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ForceCloseJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsultantProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    ReviewText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "booking",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "booking",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionEvents",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LiveKitRoomSid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LiveKitEventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    ParticipantIdentity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawWebhookPayload = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionEvents_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "booking",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionParticipants",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    FirstJoinedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLeftUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalSecondsInRoom = table.Column<int>(type: "int", nullable: false),
                    DisconnectionCount = table.Column<int>(type: "int", nullable: false),
                    CompletedPreSessionCheck = table.Column<bool>(type: "bit", nullable: false),
                    PendingReconnectionJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FinalStatus = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraField2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SrNo = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionParticipants_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "booking",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ConsultantProfileId_ScheduledStartUtc",
                schema: "booking",
                table: "Bookings",
                columns: new[] { "ConsultantProfileId", "ScheduledStartUtc" },
                unique: true,
                filter: "[Status] IN (0, 1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ConsultantUserId_Status_ScheduledStartUtc",
                schema: "booking",
                table: "Bookings",
                columns: new[] { "ConsultantUserId", "Status", "ScheduledStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_ExpiresAt",
                schema: "booking",
                table: "Bookings",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StudentUserId_Status_ScheduledStartUtc",
                schema: "booking",
                table: "Bookings",
                columns: new[] { "StudentUserId", "Status", "ScheduledStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistory_BookingId",
                schema: "booking",
                table: "BookingStatusHistory",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundAppeals_BookingId",
                schema: "booking",
                table: "RefundAppeals",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundAppeals_Status",
                schema: "booking",
                table: "RefundAppeals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                schema: "booking",
                table: "Reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ConsultantProfileId_IsPublic",
                schema: "booking",
                table: "Reviews",
                columns: new[] { "ConsultantProfileId", "IsPublic" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_SessionId",
                schema: "booking",
                table: "Reviews",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionEvents_LiveKitEventId",
                schema: "booking",
                table: "SessionEvents",
                column: "LiveKitEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionEvents_SessionId",
                schema: "booking",
                table: "SessionEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionParticipants_SessionId",
                schema: "booking",
                table: "SessionParticipants",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionParticipants_SessionId_UserId",
                schema: "booking",
                table: "SessionParticipants",
                columns: new[] { "SessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_BookingId",
                schema: "booking",
                table: "Sessions",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LiveKitRoomName",
                schema: "booking",
                table: "Sessions",
                column: "LiveKitRoomName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingStatusHistory",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "RefundAppeals",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "Reviews",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "SessionEvents",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "SessionParticipants",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "Sessions",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "booking");
        }
    }
}
