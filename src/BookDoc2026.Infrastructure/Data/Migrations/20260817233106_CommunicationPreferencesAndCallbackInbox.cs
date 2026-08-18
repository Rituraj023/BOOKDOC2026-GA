using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CommunicationPreferencesAndCallbackInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "communication_preference_event",
                schema: "communication",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    stakeholder_id = table.Column<long>(type: "bigint", nullable: false),
                    purpose_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    message_class = table.Column<int>(type: "int", nullable: false),
                    channel = table.Column<int>(type: "int", nullable: false),
                    decision = table.Column<int>(type: "int", nullable: false),
                    quiet_hours_start = table.Column<TimeOnly>(type: "time", nullable: true),
                    quiet_hours_end = table.Column<TimeOnly>(type: "time", nullable: true),
                    time_zone_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    evidence_source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    evidence_reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    recorded_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_preference_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_communication_preference_event_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_communication_preference_event_stakeholder_tenant_id_stakeholder_id",
                        columns: x => new { x.tenant_id, x.stakeholder_id },
                        principalSchema: "stakeholder",
                        principalTable: "stakeholder",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_communication_preference_event_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "platform",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "provider_callback_inbox",
                schema: "communication",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    delivery_attempt_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    external_event_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    provider_message_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    delivery_status = table.Column<int>(type: "int", nullable: false),
                    occurred_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    signature_key_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    payload_sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    processed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    error_code = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_callback_inbox", x => x.id);
                    table.ForeignKey(
                        name: "FK_provider_callback_inbox_message_delivery_attempt_delivery_attempt_id",
                        column: x => x.delivery_attempt_id,
                        principalSchema: "communication",
                        principalTable: "message_delivery_attempt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_provider_callback_inbox_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "platform",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "message_delivery_status_event",
                schema: "communication",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    delivery_attempt_id = table.Column<long>(type: "bigint", nullable: false),
                    callback_inbox_id = table.Column<long>(type: "bigint", nullable: false),
                    operation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    provider_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    external_event_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    occurred_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_delivery_status_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_message_delivery_status_event_message_delivery_attempt_delivery_attempt_id",
                        column: x => x.delivery_attempt_id,
                        principalSchema: "communication",
                        principalTable: "message_delivery_attempt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_message_delivery_status_event_provider_callback_inbox_callback_inbox_id",
                        column: x => x.callback_inbox_id,
                        principalSchema: "communication",
                        principalTable: "provider_callback_inbox",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_message_delivery_status_event_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "platform",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 5,
                column: "claim_value",
                value: "Messaging.Callbacks.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 6,
                column: "claim_value",
                value: "Messaging.Deliveries.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 7,
                column: "claim_value",
                value: "Messaging.Preferences.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 8,
                column: "claim_value",
                value: "Messaging.Preferences.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 9,
                column: "claim_value",
                value: "Messaging.Templates.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 10,
                column: "claim_value",
                value: "Messaging.Templates.Publish");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 11,
                column: "claim_value",
                value: "Messaging.Templates.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 12,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 13,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 14,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 15,
                column: "claim_value",
                value: "Patients.View");

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
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Tenants.Register", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 28,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Users.BranchAdministrators.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 29,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Users.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 30,
                column: "claim_value",
                value: "Branches.Configuration.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 31,
                column: "claim_value",
                value: "Branches.View");

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

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 49, "bookdoc:permission", "Scheduling.Availability.View", 2L },
                    { 50, "bookdoc:permission", "Scheduling.Holds.Create", 2L },
                    { 51, "bookdoc:permission", "Scheduling.Holds.Release", 2L },
                    { 52, "bookdoc:permission", "Stakeholders.Documents.Manage", 2L },
                    { 53, "bookdoc:permission", "Stakeholders.Manage", 2L },
                    { 54, "bookdoc:permission", "Stakeholders.View", 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_communication_preference_event_tenant_id_branch_id",
                schema: "communication",
                table: "communication_preference_event",
                columns: new[] { "tenant_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_preference_event_tenant_id_stakeholder_id_purpose_code_channel_created_utc",
                schema: "communication",
                table: "communication_preference_event",
                columns: new[] { "tenant_id", "stakeholder_id", "purpose_code", "channel", "created_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_message_delivery_status_event_callback_inbox_id",
                schema: "communication",
                table: "message_delivery_status_event",
                column: "callback_inbox_id");

            migrationBuilder.CreateIndex(
                name: "IX_message_delivery_status_event_delivery_attempt_id",
                schema: "communication",
                table: "message_delivery_status_event",
                column: "delivery_attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_message_delivery_status_event_provider_code_external_event_id",
                schema: "communication",
                table: "message_delivery_status_event",
                columns: new[] { "provider_code", "external_event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_message_delivery_status_event_tenant_id_branch_id_created_utc",
                schema: "communication",
                table: "message_delivery_status_event",
                columns: new[] { "tenant_id", "branch_id", "created_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_callback_inbox_delivery_attempt_id",
                schema: "communication",
                table: "provider_callback_inbox",
                column: "delivery_attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_callback_inbox_provider_code_external_event_id",
                schema: "communication",
                table: "provider_callback_inbox",
                columns: new[] { "provider_code", "external_event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_callback_inbox_tenant_id_branch_id_created_utc",
                schema: "communication",
                table: "provider_callback_inbox",
                columns: new[] { "tenant_id", "branch_id", "created_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "communication_preference_event",
                schema: "communication");

            migrationBuilder.DropTable(
                name: "message_delivery_status_event",
                schema: "communication");

            migrationBuilder.DropTable(
                name: "provider_callback_inbox",
                schema: "communication");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 54);

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 5,
                column: "claim_value",
                value: "Messaging.Deliveries.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 6,
                column: "claim_value",
                value: "Messaging.Templates.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 7,
                column: "claim_value",
                value: "Messaging.Templates.Publish");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 8,
                column: "claim_value",
                value: "Messaging.Templates.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 9,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 10,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 11,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 12,
                column: "claim_value",
                value: "Patients.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 13,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 14,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 15,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 16,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 17,
                column: "claim_value",
                value: "Scheduling.Availability.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 18,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 19,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 20,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 21,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 22,
                column: "claim_value",
                value: "Stakeholders.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 23,
                column: "claim_value",
                value: "Tenants.Approve");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 24,
                column: "claim_value",
                value: "Tenants.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 25,
                column: "claim_value",
                value: "Users.BranchAdministrators.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 26,
                column: "claim_value",
                value: "Users.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 27,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Branches.Configuration.Manage", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 28,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Branches.View", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 29,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Catalog.Manage", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 30,
                column: "claim_value",
                value: "Catalog.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 31,
                column: "claim_value",
                value: "Messaging.Deliveries.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 32,
                column: "claim_value",
                value: "Messaging.Templates.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 33,
                column: "claim_value",
                value: "Messaging.Templates.Publish");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 34,
                column: "claim_value",
                value: "Messaging.Templates.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 35,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 36,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 37,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 38,
                column: "claim_value",
                value: "Patients.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 39,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 40,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 41,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 42,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 43,
                column: "claim_value",
                value: "Scheduling.Availability.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 44,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 45,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 46,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 47,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 48,
                column: "claim_value",
                value: "Stakeholders.View");
        }
    }
}
