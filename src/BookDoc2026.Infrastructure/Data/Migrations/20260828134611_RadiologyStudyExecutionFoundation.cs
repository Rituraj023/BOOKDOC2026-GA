using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RadiologyStudyExecutionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "radiology");

            migrationBuilder.CreateTable(
                name: "study",
                schema: "radiology",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    modality = table.Column<int>(type: "int", nullable: false),
                    registration_request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<byte>(type: "tinyint", nullable: false),
                    registered_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    registered_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    last_started_by_actor_id = table.Column<long>(type: "bigint", nullable: true),
                    last_started_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    acquisition_attempt_count = table.Column<int>(type: "int", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study", x => x.id);
                    table.UniqueConstraint("AK_study_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_radiology_study_attempt_count", "[acquisition_attempt_count] >= 0");
                    table.CheckConstraint("ck_radiology_study_modality", "[modality] BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_radiology_study_status", "[status] BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_study_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_study_investigation_order_tenant_id_order_id",
                        columns: x => new { x.tenant_id, x.order_id },
                        principalSchema: "clinical",
                        principalTable: "investigation_order",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_study_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_study_service_tenant_id_service_id",
                        columns: x => new { x.tenant_id, x.service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "acquisition_attempt",
                schema: "radiology",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    study_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    equipment_resource_id = table.Column<long>(type: "bigint", nullable: false),
                    protocol_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    protocol_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    has_protocol_deviation = table.Column<bool>(type: "bit", nullable: false),
                    deviation_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    deviation_note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    outcome = table.Column<byte>(type: "tinyint", nullable: false),
                    outcome_reason_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    outcome_note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    external_study_reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    performed_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    started_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    completed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acquisition_attempt", x => x.id);
                    table.UniqueConstraint("AK_acquisition_attempt_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_radiology_acquisition_abort_reason", "([outcome] = 2 AND [outcome_reason_code] IS NOT NULL) OR ([outcome] = 1 AND [outcome_reason_code] IS NULL AND [outcome_note] IS NULL)");
                    table.CheckConstraint("ck_radiology_acquisition_deviation", "([has_protocol_deviation] = 1 AND [deviation_code] IS NOT NULL) OR ([has_protocol_deviation] = 0 AND [deviation_code] IS NULL AND [deviation_note] IS NULL)");
                    table.CheckConstraint("ck_radiology_acquisition_outcome", "[outcome] BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_radiology_acquisition_sequence", "[sequence] > 0");
                    table.CheckConstraint("ck_radiology_acquisition_timing", "[completed_utc] >= [started_utc]");
                    table.ForeignKey(
                        name: "FK_acquisition_attempt_bookable_resource_tenant_id_branch_id_equipment_resource_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.equipment_resource_id },
                        principalSchema: "resource",
                        principalTable: "bookable_resource",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_acquisition_attempt_study_tenant_id_study_id",
                        columns: x => new { x.tenant_id, x.study_id },
                        principalSchema: "radiology",
                        principalTable: "study",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "study_event",
                schema: "radiology",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    study_id = table.Column<long>(type: "bigint", nullable: false),
                    request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    request_fingerprint = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    actor_id = table.Column<long>(type: "bigint", nullable: false),
                    study_version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_event", x => x.id);
                    table.UniqueConstraint("AK_study_event_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_study_event_study_tenant_id_study_id",
                        columns: x => new { x.tenant_id, x.study_id },
                        principalSchema: "radiology",
                        principalTable: "study",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quality_review",
                schema: "radiology",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    study_id = table.Column<long>(type: "bigint", nullable: false),
                    acquisition_attempt_id = table.Column<long>(type: "bigint", nullable: false),
                    request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    decision = table.Column<byte>(type: "tinyint", nullable: false),
                    reason_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    reviewed_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_review", x => x.id);
                    table.UniqueConstraint("AK_quality_review_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_radiology_quality_decision", "[decision] BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_quality_review_acquisition_attempt_tenant_id_acquisition_attempt_id",
                        columns: x => new { x.tenant_id, x.acquisition_attempt_id },
                        principalSchema: "radiology",
                        principalTable: "acquisition_attempt",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quality_review_study_tenant_id_study_id",
                        columns: x => new { x.tenant_id, x.study_id },
                        principalSchema: "radiology",
                        principalTable: "study",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 135, "bookdoc:permission", "Radiology.Acquisitions.Record", 1L },
                    { 136, "bookdoc:permission", "Radiology.Studies.QualityReview", 1L },
                    { 137, "bookdoc:permission", "Radiology.Studies.Start", 1L },
                    { 138, "bookdoc:permission", "Radiology.Studies.View", 1L },
                    { 139, "bookdoc:permission", "Radiology.Acquisitions.Record", 2L },
                    { 140, "bookdoc:permission", "Radiology.Studies.QualityReview", 2L },
                    { 141, "bookdoc:permission", "Radiology.Studies.Start", 2L },
                    { 142, "bookdoc:permission", "Radiology.Studies.View", 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_acquisition_attempt_tenant_id_branch_id_equipment_resource_id",
                schema: "radiology",
                table: "acquisition_attempt",
                columns: new[] { "tenant_id", "branch_id", "equipment_resource_id" });

            migrationBuilder.CreateIndex(
                name: "IX_acquisition_attempt_tenant_id_external_study_reference",
                schema: "radiology",
                table: "acquisition_attempt",
                columns: new[] { "tenant_id", "external_study_reference" },
                unique: true,
                filter: "[external_study_reference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_acquisition_attempt_tenant_id_request_id",
                schema: "radiology",
                table: "acquisition_attempt",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_acquisition_attempt_tenant_id_study_id_sequence",
                schema: "radiology",
                table: "acquisition_attempt",
                columns: new[] { "tenant_id", "study_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quality_review_tenant_id_acquisition_attempt_id",
                schema: "radiology",
                table: "quality_review",
                columns: new[] { "tenant_id", "acquisition_attempt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quality_review_tenant_id_request_id",
                schema: "radiology",
                table: "quality_review",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quality_review_tenant_id_study_id",
                schema: "radiology",
                table: "quality_review",
                columns: new[] { "tenant_id", "study_id" });

            migrationBuilder.CreateIndex(
                name: "IX_study_tenant_id_branch_id_status_registered_utc",
                schema: "radiology",
                table: "study",
                columns: new[] { "tenant_id", "branch_id", "status", "registered_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_study_tenant_id_order_id",
                schema: "radiology",
                table: "study",
                columns: new[] { "tenant_id", "order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_study_tenant_id_patient_id",
                schema: "radiology",
                table: "study",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_study_tenant_id_registration_request_id",
                schema: "radiology",
                table: "study",
                columns: new[] { "tenant_id", "registration_request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_study_tenant_id_service_id",
                schema: "radiology",
                table: "study",
                columns: new[] { "tenant_id", "service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_study_event_tenant_id_request_id",
                schema: "radiology",
                table: "study_event",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_study_event_tenant_id_study_id_study_version",
                schema: "radiology",
                table: "study_event",
                columns: new[] { "tenant_id", "study_id", "study_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quality_review",
                schema: "radiology");

            migrationBuilder.DropTable(
                name: "study_event",
                schema: "radiology");

            migrationBuilder.DropTable(
                name: "acquisition_attempt",
                schema: "radiology");

            migrationBuilder.DropTable(
                name: "study",
                schema: "radiology");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 135);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 136);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 137);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 138);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 139);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 140);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 141);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 142);
        }
    }
}
