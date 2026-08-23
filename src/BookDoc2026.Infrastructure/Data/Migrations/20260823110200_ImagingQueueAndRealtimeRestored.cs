using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImagingQueueAndRealtimeRestored : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "queue");

            migrationBuilder.CreateTable(
                name: "imaging_service_point",
                schema: "queue",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    modality = table.Column<int>(type: "int", nullable: false),
                    resource_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imaging_service_point", x => x.id);
                    table.UniqueConstraint("AK_imaging_service_point_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_imaging_service_point_modality", "[modality] BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_imaging_service_point_bookable_resource_tenant_id_branch_id_resource_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.resource_id },
                        principalSchema: "resource",
                        principalTable: "bookable_resource",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_imaging_service_point_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ticket",
                schema: "queue",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    service_point_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_id = table.Column<long>(type: "bigint", nullable: true),
                    request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    display_token = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    arrived_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    called_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    service_started_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    completed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    cancelled_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    call_count = table.Column<int>(type: "int", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket", x => x.id);
                    table.UniqueConstraint("AK_ticket_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_queue_ticket_priority", "[priority] BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_queue_ticket_status", "[status] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_ticket_booking_tenant_id_booking_id",
                        columns: x => new { x.tenant_id, x.booking_id },
                        principalSchema: "scheduling",
                        principalTable: "booking",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ticket_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ticket_imaging_service_point_tenant_id_service_point_id",
                        columns: x => new { x.tenant_id, x.service_point_id },
                        principalSchema: "queue",
                        principalTable: "imaging_service_point",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ticket_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ticket_event",
                schema: "queue",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_id = table.Column<long>(type: "bigint", nullable: false),
                    from_status = table.Column<int>(type: "int", nullable: true),
                    to_status = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    actor_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ticket_version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_ticket_event_ticket_tenant_id_ticket_id",
                        columns: x => new { x.tenant_id, x.ticket_id },
                        principalSchema: "queue",
                        principalTable: "ticket",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 16,
                column: "claim_value",
                value: "Queues.Call");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 17,
                column: "claim_value",
                value: "Queues.Cancel");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 18,
                column: "claim_value",
                value: "Queues.CheckIn");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 19,
                column: "claim_value",
                value: "Queues.Display.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 20,
                column: "claim_value",
                value: "Queues.Priority.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 21,
                column: "claim_value",
                value: "Queues.Progress");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 22,
                column: "claim_value",
                value: "Queues.ServicePoints.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 23,
                column: "claim_value",
                value: "Queues.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 24,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 25,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 26,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 27,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 28,
                column: "claim_value",
                value: "Scheduling.Availability.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 29,
                column: "claim_value",
                value: "Scheduling.Bookings.Cancel");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 30,
                column: "claim_value",
                value: "Scheduling.Bookings.Confirm");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 31,
                column: "claim_value",
                value: "Scheduling.Bookings.Reschedule");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 32,
                column: "claim_value",
                value: "Scheduling.Bookings.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 33,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 34,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 35,
                column: "claim_value",
                value: "Scheduling.Waitlist.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 36,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Scheduling.Waitlist.View", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 37,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Stakeholders.Documents.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 38,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Stakeholders.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 39,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Stakeholders.View", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 40,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Tenants.Approve", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 41,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Tenants.Register", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 42,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Users.BranchAdministrators.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 43,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Users.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 44,
                column: "claim_value",
                value: "Branches.Configuration.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 45,
                column: "claim_value",
                value: "Branches.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 46,
                column: "claim_value",
                value: "Catalog.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 47,
                column: "claim_value",
                value: "Catalog.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 48,
                column: "claim_value",
                value: "Messaging.Callbacks.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 49,
                column: "claim_value",
                value: "Messaging.Deliveries.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 50,
                column: "claim_value",
                value: "Messaging.Preferences.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 51,
                column: "claim_value",
                value: "Messaging.Preferences.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 52,
                column: "claim_value",
                value: "Messaging.Templates.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 53,
                column: "claim_value",
                value: "Messaging.Templates.Publish");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 54,
                column: "claim_value",
                value: "Messaging.Templates.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 55,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 56,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 57,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 58,
                column: "claim_value",
                value: "Patients.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 59,
                column: "claim_value",
                value: "Queues.Call");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 60,
                column: "claim_value",
                value: "Queues.Cancel");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 61,
                column: "claim_value",
                value: "Queues.CheckIn");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 62,
                column: "claim_value",
                value: "Queues.Display.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 63,
                column: "claim_value",
                value: "Queues.Priority.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 64,
                column: "claim_value",
                value: "Queues.Progress");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 65,
                column: "claim_value",
                value: "Queues.ServicePoints.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 66,
                column: "claim_value",
                value: "Queues.View");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 67, "bookdoc:permission", "Resources.Manage", 2L },
                    { 68, "bookdoc:permission", "Resources.Status.Manage", 2L },
                    { 69, "bookdoc:permission", "Resources.View", 2L },
                    { 70, "bookdoc:permission", "Scheduling.Availability.Manage", 2L },
                    { 71, "bookdoc:permission", "Scheduling.Availability.View", 2L },
                    { 72, "bookdoc:permission", "Scheduling.Bookings.Cancel", 2L },
                    { 73, "bookdoc:permission", "Scheduling.Bookings.Confirm", 2L },
                    { 74, "bookdoc:permission", "Scheduling.Bookings.Reschedule", 2L },
                    { 75, "bookdoc:permission", "Scheduling.Bookings.View", 2L },
                    { 76, "bookdoc:permission", "Scheduling.Holds.Create", 2L },
                    { 77, "bookdoc:permission", "Scheduling.Holds.Release", 2L },
                    { 78, "bookdoc:permission", "Scheduling.Waitlist.Manage", 2L },
                    { 79, "bookdoc:permission", "Scheduling.Waitlist.View", 2L },
                    { 80, "bookdoc:permission", "Stakeholders.Documents.Manage", 2L },
                    { 81, "bookdoc:permission", "Stakeholders.Manage", 2L },
                    { 82, "bookdoc:permission", "Stakeholders.View", 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_imaging_service_point_tenant_id_branch_id_code",
                schema: "queue",
                table: "imaging_service_point",
                columns: new[] { "tenant_id", "branch_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_imaging_service_point_tenant_id_branch_id_resource_id",
                schema: "queue",
                table: "imaging_service_point",
                columns: new[] { "tenant_id", "branch_id", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_tenant_id_booking_id",
                schema: "queue",
                table: "ticket",
                columns: new[] { "tenant_id", "booking_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_tenant_id_branch_id_display_token",
                schema: "queue",
                table: "ticket",
                columns: new[] { "tenant_id", "branch_id", "display_token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ticket_tenant_id_branch_id_service_point_id_status_priority_arrived_utc",
                schema: "queue",
                table: "ticket",
                columns: new[] { "tenant_id", "branch_id", "service_point_id", "status", "priority", "arrived_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_tenant_id_patient_id",
                schema: "queue",
                table: "ticket",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_tenant_id_request_id",
                schema: "queue",
                table: "ticket",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ticket_tenant_id_service_point_id_booking_id",
                schema: "queue",
                table: "ticket",
                columns: new[] { "tenant_id", "service_point_id", "booking_id" },
                unique: true,
                filter: "[booking_id] IS NOT NULL AND [status] >= 1 AND [status] <= 4");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_event_tenant_id_ticket_id_ticket_version",
                schema: "queue",
                table: "ticket_event",
                columns: new[] { "tenant_id", "ticket_id", "ticket_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_event",
                schema: "queue");

            migrationBuilder.DropTable(
                name: "ticket",
                schema: "queue");

            migrationBuilder.DropTable(
                name: "imaging_service_point",
                schema: "queue");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 82);

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 16,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 17,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 18,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 19,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 20,
                column: "claim_value",
                value: "Scheduling.Availability.View");

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
                column: "claim_value",
                value: "Tenants.Approve");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 33,
                column: "claim_value",
                value: "Tenants.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 34,
                column: "claim_value",
                value: "Users.BranchAdministrators.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 35,
                column: "claim_value",
                value: "Users.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 36,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Branches.Configuration.Manage", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 37,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Branches.View", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 38,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Catalog.Manage", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 39,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Catalog.View", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 40,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Messaging.Callbacks.View", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 41,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Messaging.Deliveries.View", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 42,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Messaging.Preferences.Manage", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 43,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Messaging.Preferences.View", 2L });

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

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 59,
                column: "claim_value",
                value: "Scheduling.Bookings.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 60,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 61,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 62,
                column: "claim_value",
                value: "Scheduling.Waitlist.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 63,
                column: "claim_value",
                value: "Scheduling.Waitlist.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 64,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 65,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 66,
                column: "claim_value",
                value: "Stakeholders.View");
        }
    }
}
