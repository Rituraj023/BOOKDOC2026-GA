using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BookingLifecycleAndWaitlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                schema: "scheduling",
                table: "booking",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_utc",
                schema: "scheduling",
                table: "booking",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "previous_booking_id",
                schema: "scheduling",
                table: "booking",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "replaced_by_booking_id",
                schema: "scheduling",
                table: "booking",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "waitlist_entry_id",
                schema: "scheduling",
                table: "booking",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "booking_waitlist",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    earliest_start_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    latest_start_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    promoted_booking_id = table.Column<long>(type: "bigint", nullable: true),
                    promoted_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    withdrawn_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    withdrawal_reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_waitlist", x => x.id);
                    table.UniqueConstraint("AK_booking_waitlist_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_booking_waitlist_priority", "[priority] BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_booking_waitlist_status", "[status] BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_booking_waitlist_window", "[earliest_start_utc] <= [latest_start_utc]");
                    table.ForeignKey(
                        name: "FK_booking_waitlist_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_waitlist_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_waitlist_service_tenant_id_service_id",
                        columns: x => new { x.tenant_id, x.service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 21,
                column: "claim_value",
                value: "Scheduling.Bookings.Cancel");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 22,
                column: "claim_value",
                value: "Scheduling.Bookings.Confirm");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 23,
                column: "claim_value",
                value: "Scheduling.Bookings.Reschedule");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 24,
                column: "claim_value",
                value: "Scheduling.Bookings.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 25,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 26,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 27,
                column: "claim_value",
                value: "Scheduling.Waitlist.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 28,
                column: "claim_value",
                value: "Scheduling.Waitlist.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 29,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 30,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 31,
                column: "claim_value",
                value: "Stakeholders.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 32,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Tenants.Approve", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 33,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Tenants.Register", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 34,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Users.BranchAdministrators.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 35,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Users.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 36,
                column: "claim_value",
                value: "Branches.Configuration.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 37,
                column: "claim_value",
                value: "Branches.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 38,
                column: "claim_value",
                value: "Catalog.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 39,
                column: "claim_value",
                value: "Catalog.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 40,
                column: "claim_value",
                value: "Messaging.Callbacks.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 41,
                column: "claim_value",
                value: "Messaging.Deliveries.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 42,
                column: "claim_value",
                value: "Messaging.Preferences.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 43,
                column: "claim_value",
                value: "Messaging.Preferences.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 44,
                column: "claim_value",
                value: "Messaging.Templates.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 45,
                column: "claim_value",
                value: "Messaging.Templates.Publish");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 46,
                column: "claim_value",
                value: "Messaging.Templates.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 47,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 48,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 49,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 50,
                column: "claim_value",
                value: "Patients.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 51,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 52,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 53,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 54,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 55,
                column: "claim_value",
                value: "Scheduling.Availability.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 56,
                column: "claim_value",
                value: "Scheduling.Bookings.Cancel");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 57,
                column: "claim_value",
                value: "Scheduling.Bookings.Confirm");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 58,
                column: "claim_value",
                value: "Scheduling.Bookings.Reschedule");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 59, "bookdoc:permission", "Scheduling.Bookings.View", 2L },
                    { 60, "bookdoc:permission", "Scheduling.Holds.Create", 2L },
                    { 61, "bookdoc:permission", "Scheduling.Holds.Release", 2L },
                    { 62, "bookdoc:permission", "Scheduling.Waitlist.Manage", 2L },
                    { 63, "bookdoc:permission", "Scheduling.Waitlist.View", 2L },
                    { 64, "bookdoc:permission", "Stakeholders.Documents.Manage", 2L },
                    { 65, "bookdoc:permission", "Stakeholders.Manage", 2L },
                    { 66, "bookdoc:permission", "Stakeholders.View", 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_tenant_id_previous_booking_id",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "previous_booking_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_tenant_id_replaced_by_booking_id",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "replaced_by_booking_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_tenant_id_waitlist_entry_id",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "waitlist_entry_id" },
                unique: true,
                filter: "[waitlist_entry_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_booking_waitlist_tenant_id_branch_id_service_id_status_priority_created_utc",
                schema: "scheduling",
                table: "booking_waitlist",
                columns: new[] { "tenant_id", "branch_id", "service_id", "status", "priority", "created_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_waitlist_tenant_id_patient_id",
                schema: "scheduling",
                table: "booking_waitlist",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_waitlist_tenant_id_promoted_booking_id",
                schema: "scheduling",
                table: "booking_waitlist",
                columns: new[] { "tenant_id", "promoted_booking_id" },
                unique: true,
                filter: "[promoted_booking_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_booking_waitlist_tenant_id_service_id",
                schema: "scheduling",
                table: "booking_waitlist",
                columns: new[] { "tenant_id", "service_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_booking_booking_tenant_id_previous_booking_id",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "previous_booking_id" },
                principalSchema: "scheduling",
                principalTable: "booking",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_booking_booking_tenant_id_replaced_by_booking_id",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "replaced_by_booking_id" },
                principalSchema: "scheduling",
                principalTable: "booking",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_booking_booking_tenant_id_previous_booking_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropForeignKey(
                name: "FK_booking_booking_tenant_id_replaced_by_booking_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropTable(
                name: "booking_waitlist",
                schema: "scheduling");

            migrationBuilder.DropIndex(
                name: "IX_booking_tenant_id_previous_booking_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropIndex(
                name: "IX_booking_tenant_id_replaced_by_booking_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropIndex(
                name: "IX_booking_tenant_id_waitlist_entry_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 66);

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "cancelled_utc",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "previous_booking_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "replaced_by_booking_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "waitlist_entry_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 21,
                column: "claim_value",
                value: "Scheduling.Bookings.Confirm");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 22,
                column: "claim_value",
                value: "Scheduling.Bookings.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 23,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 24,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 25,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 26,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 27,
                column: "claim_value",
                value: "Stakeholders.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 28,
                column: "claim_value",
                value: "Tenants.Approve");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 29,
                column: "claim_value",
                value: "Tenants.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 30,
                column: "claim_value",
                value: "Users.BranchAdministrators.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 31,
                column: "claim_value",
                value: "Users.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 32,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Branches.Configuration.Manage", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 33,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Branches.View", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 34,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Catalog.Manage", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 35,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Catalog.View", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 36,
                column: "claim_value",
                value: "Messaging.Callbacks.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 37,
                column: "claim_value",
                value: "Messaging.Deliveries.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 38,
                column: "claim_value",
                value: "Messaging.Preferences.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 39,
                column: "claim_value",
                value: "Messaging.Preferences.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 40,
                column: "claim_value",
                value: "Messaging.Templates.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 41,
                column: "claim_value",
                value: "Messaging.Templates.Publish");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 42,
                column: "claim_value",
                value: "Messaging.Templates.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 43,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 44,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 45,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 46,
                column: "claim_value",
                value: "Patients.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 47,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 48,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 49,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 50,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 51,
                column: "claim_value",
                value: "Scheduling.Availability.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 52,
                column: "claim_value",
                value: "Scheduling.Bookings.Confirm");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 53,
                column: "claim_value",
                value: "Scheduling.Bookings.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 54,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 55,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 56,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 57,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 58,
                column: "claim_value",
                value: "Stakeholders.View");
        }
    }
}
