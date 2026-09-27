using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PhysiotherapyAndInvestigationClinicalDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "investigation_order_id",
                schema: "queue",
                table: "ticket",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "investigation_order",
                schema: "clinical",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    encounter_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    requested_service_id = table.Column<long>(type: "bigint", nullable: false),
                    request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    modality = table.Column<int>(type: "int", nullable: false),
                    clinical_indication = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<byte>(type: "tinyint", nullable: false),
                    result_status = table.Column<byte>(type: "tinyint", nullable: false),
                    requested_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    ordered_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    queue_handoff_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    queue_handoff_by_actor_id = table.Column<long>(type: "bigint", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investigation_order", x => x.id);
                    table.UniqueConstraint("AK_investigation_order_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_investigation_order_status", "[status] = 1");
                    table.CheckConstraint("ck_investigation_result_status", "[result_status] = 1");
                    table.ForeignKey(
                        name: "FK_investigation_order_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_investigation_order_encounter_tenant_id_encounter_id",
                        columns: x => new { x.tenant_id, x.encounter_id },
                        principalSchema: "clinical",
                        principalTable: "encounter",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_investigation_order_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_investigation_order_service_tenant_id_requested_service_id",
                        columns: x => new { x.tenant_id, x.requested_service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "physiotherapy_care_plan",
                schema: "clinical",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    initial_encounter_id = table.Column<long>(type: "bigint", nullable: false),
                    care_plan_number = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<byte>(type: "tinyint", nullable: false),
                    latest_revision_number = table.Column<int>(type: "int", nullable: false),
                    latest_revision_id = table.Column<long>(type: "bigint", nullable: false),
                    created_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    activated_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    activated_by_actor_id = table.Column<long>(type: "bigint", nullable: true),
                    closed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    closed_by_actor_id = table.Column<long>(type: "bigint", nullable: true),
                    closure_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_physiotherapy_care_plan", x => x.id);
                    table.UniqueConstraint("AK_physiotherapy_care_plan_tenant_id_branch_id_id", x => new { x.tenant_id, x.branch_id, x.id });
                    table.UniqueConstraint("AK_physiotherapy_care_plan_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_physio_care_plan_status", "[status] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_physiotherapy_care_plan_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_physiotherapy_care_plan_encounter_tenant_id_initial_encounter_id",
                        columns: x => new { x.tenant_id, x.initial_encounter_id },
                        principalSchema: "clinical",
                        principalTable: "encounter",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_physiotherapy_care_plan_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_physiotherapy_care_plan_service_tenant_id_service_id",
                        columns: x => new { x.tenant_id, x.service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investigation_order_event",
                schema: "clinical",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    actor_id = table.Column<long>(type: "bigint", nullable: false),
                    queue_ticket_id = table.Column<long>(type: "bigint", nullable: true),
                    order_version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investigation_order_event", x => x.id);
                    table.UniqueConstraint("AK_investigation_order_event_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_investigation_order_event_investigation_order_tenant_id_order_id",
                        columns: x => new { x.tenant_id, x.order_id },
                        principalSchema: "clinical",
                        principalTable: "investigation_order",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_investigation_order_event_ticket_tenant_id_queue_ticket_id",
                        columns: x => new { x.tenant_id, x.queue_ticket_id },
                        principalSchema: "queue",
                        principalTable: "ticket",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "physiotherapy_care_plan_revision",
                schema: "clinical",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    care_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    revision_number = table.Column<int>(type: "int", nullable: false),
                    parent_revision_id = table.Column<long>(type: "bigint", nullable: true),
                    author_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    goal_summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    frequency_and_duration = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    planned_interventions = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: false),
                    precautions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    review_on = table.Column<DateOnly>(type: "date", nullable: true),
                    content_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    change_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_physiotherapy_care_plan_revision", x => x.id);
                    table.UniqueConstraint("AK_physiotherapy_care_plan_revision_tenant_id_branch_id_id", x => new { x.tenant_id, x.branch_id, x.id });
                    table.UniqueConstraint("AK_physiotherapy_care_plan_revision_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_physiotherapy_care_plan_revision_physiotherapy_care_plan_revision_tenant_id_branch_id_parent_revision_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.parent_revision_id },
                        principalSchema: "clinical",
                        principalTable: "physiotherapy_care_plan_revision",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_physiotherapy_care_plan_revision_physiotherapy_care_plan_tenant_id_branch_id_care_plan_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.care_plan_id },
                        principalSchema: "clinical",
                        principalTable: "physiotherapy_care_plan",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "physiotherapy_treatment_session",
                schema: "clinical",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    care_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    encounter_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    sequence_number = table.Column<int>(type: "int", nullable: false),
                    author_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    subjective_response = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    interventions = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    tolerance = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    next_plan = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    had_adverse_event = table.Column<bool>(type: "bit", nullable: false),
                    adverse_event_details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    content_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    performed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_physiotherapy_treatment_session", x => x.id);
                    table.UniqueConstraint("AK_physiotherapy_treatment_session_tenant_id_branch_id_id", x => new { x.tenant_id, x.branch_id, x.id });
                    table.UniqueConstraint("AK_physiotherapy_treatment_session_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_physio_session_sequence", "[sequence_number] > 0");
                    table.ForeignKey(
                        name: "FK_physiotherapy_treatment_session_booking_tenant_id_booking_id",
                        columns: x => new { x.tenant_id, x.booking_id },
                        principalSchema: "scheduling",
                        principalTable: "booking",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_physiotherapy_treatment_session_encounter_tenant_id_encounter_id",
                        columns: x => new { x.tenant_id, x.encounter_id },
                        principalSchema: "clinical",
                        principalTable: "encounter",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_physiotherapy_treatment_session_physiotherapy_care_plan_tenant_id_branch_id_care_plan_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.care_plan_id },
                        principalSchema: "clinical",
                        principalTable: "physiotherapy_care_plan",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "physiotherapy_outcome_observation",
                schema: "clinical",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    care_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    treatment_session_id = table.Column<long>(type: "bigint", nullable: true),
                    context_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    measure_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    tool_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    value = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    body_site = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    laterality_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    observed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    author_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_physiotherapy_outcome_observation", x => x.id);
                    table.ForeignKey(
                        name: "FK_physiotherapy_outcome_observation_physiotherapy_care_plan_tenant_id_branch_id_care_plan_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.care_plan_id },
                        principalSchema: "clinical",
                        principalTable: "physiotherapy_care_plan",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_physiotherapy_outcome_observation_physiotherapy_treatment_session_tenant_id_branch_id_treatment_session_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.treatment_session_id },
                        principalSchema: "clinical",
                        principalTable: "physiotherapy_treatment_session",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 119, "bookdoc:permission", "Physiotherapy.CarePlans.Manage", 1L },
                    { 120, "bookdoc:permission", "Physiotherapy.CarePlans.View", 1L },
                    { 121, "bookdoc:permission", "Physiotherapy.Outcomes.Record", 1L },
                    { 122, "bookdoc:permission", "Physiotherapy.Sessions.Record", 1L },
                    { 123, "bookdoc:permission", "Physiotherapy.CarePlans.Manage", 2L },
                    { 124, "bookdoc:permission", "Physiotherapy.CarePlans.View", 2L },
                    { 125, "bookdoc:permission", "Physiotherapy.Outcomes.Record", 2L },
                    { 126, "bookdoc:permission", "Physiotherapy.Sessions.Record", 2L },
                    { 127, "bookdoc:permission", "Investigations.Orders.Create", 1L },
                    { 128, "bookdoc:permission", "Investigations.Queue.Handoff", 1L },
                    { 129, "bookdoc:permission", "Investigations.View", 1L },
                    { 130, "bookdoc:permission", "Investigations.Orders.Create", 2L },
                    { 131, "bookdoc:permission", "Investigations.Queue.Handoff", 2L },
                    { 132, "bookdoc:permission", "Investigations.View", 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_tenant_id_investigation_order_id",
                schema: "queue",
                table: "ticket",
                columns: new[] { "tenant_id", "investigation_order_id" },
                unique: true,
                filter: "[investigation_order_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_investigation_order_tenant_id_branch_id_encounter_id_ordered_utc",
                schema: "clinical",
                table: "investigation_order",
                columns: new[] { "tenant_id", "branch_id", "encounter_id", "ordered_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_investigation_order_tenant_id_branch_id_order_number",
                schema: "clinical",
                table: "investigation_order",
                columns: new[] { "tenant_id", "branch_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_investigation_order_tenant_id_encounter_id",
                schema: "clinical",
                table: "investigation_order",
                columns: new[] { "tenant_id", "encounter_id" });

            migrationBuilder.CreateIndex(
                name: "IX_investigation_order_tenant_id_patient_id",
                schema: "clinical",
                table: "investigation_order",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_investigation_order_tenant_id_request_id",
                schema: "clinical",
                table: "investigation_order",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_investigation_order_tenant_id_requested_service_id",
                schema: "clinical",
                table: "investigation_order",
                columns: new[] { "tenant_id", "requested_service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_investigation_order_event_tenant_id_order_id_order_version",
                schema: "clinical",
                table: "investigation_order_event",
                columns: new[] { "tenant_id", "order_id", "order_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_investigation_order_event_tenant_id_queue_ticket_id",
                schema: "clinical",
                table: "investigation_order_event",
                columns: new[] { "tenant_id", "queue_ticket_id" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_care_plan_tenant_id_branch_id_initial_encounter_id",
                schema: "clinical",
                table: "physiotherapy_care_plan",
                columns: new[] { "tenant_id", "branch_id", "initial_encounter_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_care_plan_tenant_id_branch_id_patient_id_status",
                schema: "clinical",
                table: "physiotherapy_care_plan",
                columns: new[] { "tenant_id", "branch_id", "patient_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_care_plan_tenant_id_initial_encounter_id",
                schema: "clinical",
                table: "physiotherapy_care_plan",
                columns: new[] { "tenant_id", "initial_encounter_id" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_care_plan_tenant_id_patient_id",
                schema: "clinical",
                table: "physiotherapy_care_plan",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_care_plan_tenant_id_service_id",
                schema: "clinical",
                table: "physiotherapy_care_plan",
                columns: new[] { "tenant_id", "service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_care_plan_revision_tenant_id_branch_id_care_plan_id",
                schema: "clinical",
                table: "physiotherapy_care_plan_revision",
                columns: new[] { "tenant_id", "branch_id", "care_plan_id" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_care_plan_revision_tenant_id_branch_id_parent_revision_id",
                schema: "clinical",
                table: "physiotherapy_care_plan_revision",
                columns: new[] { "tenant_id", "branch_id", "parent_revision_id" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_care_plan_revision_tenant_id_care_plan_id_revision_number",
                schema: "clinical",
                table: "physiotherapy_care_plan_revision",
                columns: new[] { "tenant_id", "care_plan_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_outcome_observation_tenant_id_branch_id_care_plan_id",
                schema: "clinical",
                table: "physiotherapy_outcome_observation",
                columns: new[] { "tenant_id", "branch_id", "care_plan_id" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_outcome_observation_tenant_id_branch_id_treatment_session_id",
                schema: "clinical",
                table: "physiotherapy_outcome_observation",
                columns: new[] { "tenant_id", "branch_id", "treatment_session_id" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_outcome_observation_tenant_id_care_plan_id_measure_code_tool_version_observed_utc",
                schema: "clinical",
                table: "physiotherapy_outcome_observation",
                columns: new[] { "tenant_id", "care_plan_id", "measure_code", "tool_version", "observed_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_treatment_session_tenant_id_booking_id",
                schema: "clinical",
                table: "physiotherapy_treatment_session",
                columns: new[] { "tenant_id", "booking_id" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_treatment_session_tenant_id_branch_id_care_plan_id",
                schema: "clinical",
                table: "physiotherapy_treatment_session",
                columns: new[] { "tenant_id", "branch_id", "care_plan_id" });

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_treatment_session_tenant_id_branch_id_encounter_id",
                schema: "clinical",
                table: "physiotherapy_treatment_session",
                columns: new[] { "tenant_id", "branch_id", "encounter_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_treatment_session_tenant_id_care_plan_id_sequence_number",
                schema: "clinical",
                table: "physiotherapy_treatment_session",
                columns: new[] { "tenant_id", "care_plan_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_physiotherapy_treatment_session_tenant_id_encounter_id",
                schema: "clinical",
                table: "physiotherapy_treatment_session",
                columns: new[] { "tenant_id", "encounter_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_ticket_investigation_order_tenant_id_investigation_order_id",
                schema: "queue",
                table: "ticket",
                columns: new[] { "tenant_id", "investigation_order_id" },
                principalSchema: "clinical",
                principalTable: "investigation_order",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ticket_investigation_order_tenant_id_investigation_order_id",
                schema: "queue",
                table: "ticket");

            migrationBuilder.DropTable(
                name: "investigation_order_event",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "physiotherapy_care_plan_revision",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "physiotherapy_outcome_observation",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "investigation_order",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "physiotherapy_treatment_session",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "physiotherapy_care_plan",
                schema: "clinical");

            migrationBuilder.DropIndex(
                name: "IX_ticket_tenant_id_investigation_order_id",
                schema: "queue",
                table: "ticket");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 121);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 122);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 123);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 124);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 125);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 126);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 127);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 128);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 129);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 130);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 131);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 132);

            migrationBuilder.DropColumn(
                name: "investigation_order_id",
                schema: "queue",
                table: "ticket");
        }
    }
}
