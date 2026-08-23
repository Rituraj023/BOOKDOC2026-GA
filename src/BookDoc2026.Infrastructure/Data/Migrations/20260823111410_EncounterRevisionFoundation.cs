using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EncounterRevisionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "clinical");

            migrationBuilder.CreateTable(
                name: "encounter",
                schema: "clinical",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    encounter_number = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    status = table.Column<byte>(type: "tinyint", nullable: false),
                    latest_revision_number = table.Column<int>(type: "int", nullable: false),
                    latest_revision_id = table.Column<long>(type: "bigint", nullable: false),
                    signed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    signed_by_actor_id = table.Column<long>(type: "bigint", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encounter", x => x.id);
                    table.UniqueConstraint("AK_encounter_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_clinical_encounter_status", "[status] BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_encounter_booking_tenant_id_booking_id",
                        columns: x => new { x.tenant_id, x.booking_id },
                        principalSchema: "scheduling",
                        principalTable: "booking",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_encounter_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_encounter_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_encounter_service_tenant_id_service_id",
                        columns: x => new { x.tenant_id, x.service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "encounter_revision",
                schema: "clinical",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    encounter_id = table.Column<long>(type: "bigint", nullable: false),
                    revision_number = table.Column<int>(type: "int", nullable: false),
                    kind = table.Column<byte>(type: "tinyint", nullable: false),
                    parent_revision_id = table.Column<long>(type: "bigint", nullable: true),
                    author_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    specialty_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    template_key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    template_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    chief_complaint = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    history = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    examination = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    assessment = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    plan = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    instructions = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    body_site = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    laterality_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    content_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    amendment_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    signed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encounter_revision", x => x.id);
                    table.UniqueConstraint("AK_encounter_revision_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_encounter_revision_kind", "[kind] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_encounter_revision_encounter_revision_tenant_id_parent_revision_id",
                        columns: x => new { x.tenant_id, x.parent_revision_id },
                        principalSchema: "clinical",
                        principalTable: "encounter_revision",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_encounter_revision_encounter_tenant_id_encounter_id",
                        columns: x => new { x.tenant_id, x.encounter_id },
                        principalSchema: "clinical",
                        principalTable: "encounter",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 93, "bookdoc:permission", "Encounters.Amend", 1L },
                    { 94, "bookdoc:permission", "Encounters.Drafts.Manage", 1L },
                    { 95, "bookdoc:permission", "Encounters.Sign", 1L },
                    { 96, "bookdoc:permission", "Encounters.View", 1L },
                    { 97, "bookdoc:permission", "Encounters.Amend", 2L },
                    { 98, "bookdoc:permission", "Encounters.Drafts.Manage", 2L },
                    { 99, "bookdoc:permission", "Encounters.Sign", 2L },
                    { 100, "bookdoc:permission", "Encounters.View", 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_encounter_tenant_id_booking_id",
                schema: "clinical",
                table: "encounter",
                columns: new[] { "tenant_id", "booking_id" });

            migrationBuilder.CreateIndex(
                name: "IX_encounter_tenant_id_branch_id_booking_id",
                schema: "clinical",
                table: "encounter",
                columns: new[] { "tenant_id", "branch_id", "booking_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_encounter_tenant_id_branch_id_patient_id_status",
                schema: "clinical",
                table: "encounter",
                columns: new[] { "tenant_id", "branch_id", "patient_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_encounter_tenant_id_patient_id",
                schema: "clinical",
                table: "encounter",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_encounter_tenant_id_service_id",
                schema: "clinical",
                table: "encounter",
                columns: new[] { "tenant_id", "service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_encounter_revision_tenant_id_encounter_id_revision_number",
                schema: "clinical",
                table: "encounter_revision",
                columns: new[] { "tenant_id", "encounter_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_encounter_revision_tenant_id_parent_revision_id",
                schema: "clinical",
                table: "encounter_revision",
                columns: new[] { "tenant_id", "parent_revision_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "encounter_revision",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "encounter",
                schema: "clinical");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 100);
        }
    }
}
