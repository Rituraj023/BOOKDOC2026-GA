using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DoctorSlotsAndBookingRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_request",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: true),
                    patient_full_name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    patient_phone = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    patient_email = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    preferred_practitioner_id = table.Column<long>(type: "bigint", nullable: true),
                    service_id = table.Column<long>(type: "bigint", nullable: true),
                    preferred_date = table.Column<DateOnly>(type: "date", nullable: false),
                    preferred_time_slot = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    reason_for_visit = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    assigned_practitioner_id = table.Column<long>(type: "bigint", nullable: true),
                    confirmed_booking_id = table.Column<long>(type: "bigint", nullable: true),
                    review_notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    reviewed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    reviewed_by_staff_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_request", x => x.id);
                    table.UniqueConstraint("AK_booking_request_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_booking_request_status", "[status] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_booking_request_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_request_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_request_practitioner_tenant_id_preferred_practitioner_id",
                        columns: x => new { x.tenant_id, x.preferred_practitioner_id },
                        principalSchema: "workforce",
                        principalTable: "practitioner",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_request_service_tenant_id_service_id",
                        columns: x => new { x.tenant_id, x.service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_slot",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    practitioner_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: true),
                    slot_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    end_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    duration_minutes = table.Column<int>(type: "int", nullable: false),
                    max_capacity = table.Column<int>(type: "int", nullable: false),
                    booked_count = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    block_reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_slot", x => x.id);
                    table.UniqueConstraint("AK_booking_slot_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_booking_slot_capacity", "[max_capacity] >= 1");
                    table.CheckConstraint("ck_booking_slot_duration", "[duration_minutes] BETWEEN 1 AND 1440");
                    table.CheckConstraint("ck_booking_slot_interval", "[start_utc] < [end_utc]");
                    table.CheckConstraint("ck_booking_slot_status", "[status] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_booking_slot_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_slot_practitioner_tenant_id_practitioner_id",
                        columns: x => new { x.tenant_id, x.practitioner_id },
                        principalSchema: "workforce",
                        principalTable: "practitioner",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_slot_service_tenant_id_service_id",
                        columns: x => new { x.tenant_id, x.service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_request_tenant_id_branch_id_status_preferred_date",
                schema: "scheduling",
                table: "booking_request",
                columns: new[] { "tenant_id", "branch_id", "status", "preferred_date" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_request_tenant_id_patient_id",
                schema: "scheduling",
                table: "booking_request",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_request_tenant_id_patient_phone",
                schema: "scheduling",
                table: "booking_request",
                columns: new[] { "tenant_id", "patient_phone" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_request_tenant_id_preferred_practitioner_id",
                schema: "scheduling",
                table: "booking_request",
                columns: new[] { "tenant_id", "preferred_practitioner_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_request_tenant_id_service_id",
                schema: "scheduling",
                table: "booking_request",
                columns: new[] { "tenant_id", "service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_slot_tenant_id_branch_id_practitioner_id_slot_date",
                schema: "scheduling",
                table: "booking_slot",
                columns: new[] { "tenant_id", "branch_id", "practitioner_id", "slot_date" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_slot_tenant_id_branch_id_practitioner_id_start_utc_end_utc",
                schema: "scheduling",
                table: "booking_slot",
                columns: new[] { "tenant_id", "branch_id", "practitioner_id", "start_utc", "end_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_slot_tenant_id_branch_id_status_slot_date",
                schema: "scheduling",
                table: "booking_slot",
                columns: new[] { "tenant_id", "branch_id", "status", "slot_date" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_slot_tenant_id_practitioner_id",
                schema: "scheduling",
                table: "booking_slot",
                columns: new[] { "tenant_id", "practitioner_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_slot_tenant_id_service_id",
                schema: "scheduling",
                table: "booking_slot",
                columns: new[] { "tenant_id", "service_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_request",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "booking_slot",
                schema: "scheduling");
        }
    }
}
