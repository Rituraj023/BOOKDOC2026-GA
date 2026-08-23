using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizedBookingConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "requirement_role_code",
                schema: "scheduling",
                table: "resource_reservation",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_resource_reservation_tenant_id_id",
                schema: "scheduling",
                table: "resource_reservation",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "booking",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    hold_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_number = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    start_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    end_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    confirmed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking", x => x.id);
                    table.UniqueConstraint("AK_booking_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_booking_interval", "[start_utc] < [end_utc]");
                    table.CheckConstraint("ck_booking_status", "[status] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_booking_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_hold_tenant_id_hold_id",
                        columns: x => new { x.tenant_id, x.hold_id },
                        principalSchema: "scheduling",
                        principalTable: "hold",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_service_tenant_id_service_id",
                        columns: x => new { x.tenant_id, x.service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_resource",
                schema: "scheduling",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    hold_reservation_id = table.Column<long>(type: "bigint", nullable: false),
                    resource_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    requirement_role_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_resource", x => x.id);
                    table.CheckConstraint("ck_booking_resource_quantity", "[quantity] BETWEEN 1 AND 1000");
                    table.ForeignKey(
                        name: "FK_booking_resource_bookable_resource_tenant_id_branch_id_resource_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.resource_id },
                        principalSchema: "resource",
                        principalTable: "bookable_resource",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_resource_booking_tenant_id_booking_id",
                        columns: x => new { x.tenant_id, x.booking_id },
                        principalSchema: "scheduling",
                        principalTable: "booking",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_booking_resource_resource_reservation_tenant_id_hold_reservation_id",
                        columns: x => new { x.tenant_id, x.hold_reservation_id },
                        principalSchema: "scheduling",
                        principalTable: "resource_reservation",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

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
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Users.BranchAdministrators.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 31,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Users.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 32,
                column: "claim_value",
                value: "Branches.Configuration.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 33,
                column: "claim_value",
                value: "Branches.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 34,
                column: "claim_value",
                value: "Catalog.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 35,
                column: "claim_value",
                value: "Catalog.View");

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

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 55, "bookdoc:permission", "Scheduling.Holds.Release", 2L },
                    { 56, "bookdoc:permission", "Stakeholders.Documents.Manage", 2L },
                    { 57, "bookdoc:permission", "Stakeholders.Manage", 2L },
                    { 58, "bookdoc:permission", "Stakeholders.View", 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_tenant_id_booking_number",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "booking_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_tenant_id_branch_id_start_utc_status",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "branch_id", "start_utc", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_tenant_id_hold_id",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "hold_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_tenant_id_patient_id",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_tenant_id_service_id",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_resource_tenant_id_booking_id_resource_id",
                schema: "scheduling",
                table: "booking_resource",
                columns: new[] { "tenant_id", "booking_id", "resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_resource_tenant_id_branch_id_resource_id",
                schema: "scheduling",
                table: "booking_resource",
                columns: new[] { "tenant_id", "branch_id", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_resource_tenant_id_hold_reservation_id",
                schema: "scheduling",
                table: "booking_resource",
                columns: new[] { "tenant_id", "hold_reservation_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_resource",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "booking",
                schema: "scheduling");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_resource_reservation_tenant_id_id",
                schema: "scheduling",
                table: "resource_reservation");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 58);

            migrationBuilder.DropColumn(
                name: "requirement_role_code",
                schema: "scheduling",
                table: "resource_reservation");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 21,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 22,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 23,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 24,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 25,
                column: "claim_value",
                value: "Stakeholders.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 26,
                column: "claim_value",
                value: "Tenants.Approve");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 27,
                column: "claim_value",
                value: "Tenants.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 28,
                column: "claim_value",
                value: "Users.BranchAdministrators.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 29,
                column: "claim_value",
                value: "Users.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 30,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Branches.Configuration.Manage", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 31,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Branches.View", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 32,
                column: "claim_value",
                value: "Catalog.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 33,
                column: "claim_value",
                value: "Catalog.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 34,
                column: "claim_value",
                value: "Messaging.Callbacks.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 35,
                column: "claim_value",
                value: "Messaging.Deliveries.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 36,
                column: "claim_value",
                value: "Messaging.Preferences.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 37,
                column: "claim_value",
                value: "Messaging.Preferences.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 38,
                column: "claim_value",
                value: "Messaging.Templates.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 39,
                column: "claim_value",
                value: "Messaging.Templates.Publish");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 40,
                column: "claim_value",
                value: "Messaging.Templates.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 41,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 42,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 43,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 44,
                column: "claim_value",
                value: "Patients.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 45,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 46,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 47,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 48,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 49,
                column: "claim_value",
                value: "Scheduling.Availability.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 50,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 51,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 52,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 53,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 54,
                column: "claim_value",
                value: "Stakeholders.View");
        }
    }
}
