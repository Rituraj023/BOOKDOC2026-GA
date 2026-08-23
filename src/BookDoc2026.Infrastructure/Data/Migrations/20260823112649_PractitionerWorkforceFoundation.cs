using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PractitionerWorkforceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workforce");

            migrationBuilder.CreateTable(
                name: "practitioner",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    stakeholder_id = table.Column<long>(type: "bigint", nullable: false),
                    identity_subject_id = table.Column<long>(type: "bigint", nullable: false),
                    practitioner_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    practitioner_type_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_practitioner", x => x.id);
                    table.UniqueConstraint("AK_practitioner_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_workforce_practitioner_status", "[status] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_practitioner_stakeholder_tenant_id_stakeholder_id",
                        columns: x => new { x.tenant_id, x.stakeholder_id },
                        principalSchema: "stakeholder",
                        principalTable: "stakeholder",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_practitioner_user_identity_subject_id",
                        column: x => x.identity_subject_id,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "practitioner_assignment",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    practitioner_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    bookable_resource_id = table.Column<long>(type: "bigint", nullable: true),
                    role_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_practitioner_assignment", x => x.id);
                    table.UniqueConstraint("AK_practitioner_assignment_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_workforce_assignment_status", "[status] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_practitioner_assignment_bookable_resource_tenant_id_branch_id_bookable_resource_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.bookable_resource_id },
                        principalSchema: "resource",
                        principalTable: "bookable_resource",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_practitioner_assignment_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_practitioner_assignment_practitioner_tenant_id_practitioner_id",
                        columns: x => new { x.tenant_id, x.practitioner_id },
                        principalSchema: "workforce",
                        principalTable: "practitioner",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_practitioner_assignment_service_tenant_id_service_id",
                        columns: x => new { x.tenant_id, x.service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "practitioner_credential",
                schema: "workforce",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    practitioner_id = table.Column<long>(type: "bigint", nullable: false),
                    credential_type_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    registration_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    issuing_authority = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    verification_status = table.Column<int>(type: "int", nullable: false),
                    verified_by_actor_id = table.Column<long>(type: "bigint", nullable: true),
                    verified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    decision_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_practitioner_credential", x => x.id);
                    table.UniqueConstraint("AK_practitioner_credential_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_workforce_credential_status", "[verification_status] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_practitioner_credential_practitioner_tenant_id_practitioner_id",
                        columns: x => new { x.tenant_id, x.practitioner_id },
                        principalSchema: "workforce",
                        principalTable: "practitioner",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 101, "bookdoc:permission", "Practitioners.Assignments.Manage", 1L },
                    { 102, "bookdoc:permission", "Practitioners.Credentials.Verify", 1L },
                    { 103, "bookdoc:permission", "Practitioners.Manage", 1L },
                    { 104, "bookdoc:permission", "Practitioners.View", 1L },
                    { 105, "bookdoc:permission", "Practitioners.Assignments.Manage", 2L },
                    { 106, "bookdoc:permission", "Practitioners.Credentials.Verify", 2L },
                    { 107, "bookdoc:permission", "Practitioners.Manage", 2L },
                    { 108, "bookdoc:permission", "Practitioners.View", 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_practitioner_identity_subject_id",
                schema: "workforce",
                table: "practitioner",
                column: "identity_subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_practitioner_tenant_id_identity_subject_id",
                schema: "workforce",
                table: "practitioner",
                columns: new[] { "tenant_id", "identity_subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_practitioner_tenant_id_practitioner_code",
                schema: "workforce",
                table: "practitioner",
                columns: new[] { "tenant_id", "practitioner_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_practitioner_tenant_id_stakeholder_id",
                schema: "workforce",
                table: "practitioner",
                columns: new[] { "tenant_id", "stakeholder_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_practitioner_assignment_tenant_id_branch_id_bookable_resource_id",
                schema: "workforce",
                table: "practitioner_assignment",
                columns: new[] { "tenant_id", "branch_id", "bookable_resource_id" });

            migrationBuilder.CreateIndex(
                name: "IX_practitioner_assignment_tenant_id_practitioner_id_branch_id_service_id_status_effective_from",
                schema: "workforce",
                table: "practitioner_assignment",
                columns: new[] { "tenant_id", "practitioner_id", "branch_id", "service_id", "status", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "IX_practitioner_assignment_tenant_id_service_id",
                schema: "workforce",
                table: "practitioner_assignment",
                columns: new[] { "tenant_id", "service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_practitioner_credential_tenant_id_credential_type_code_issuing_authority_registration_number",
                schema: "workforce",
                table: "practitioner_credential",
                columns: new[] { "tenant_id", "credential_type_code", "issuing_authority", "registration_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_practitioner_credential_tenant_id_practitioner_id_verification_status_valid_to",
                schema: "workforce",
                table: "practitioner_credential",
                columns: new[] { "tenant_id", "practitioner_id", "verification_status", "valid_to" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "practitioner_assignment",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "practitioner_credential",
                schema: "workforce");

            migrationBuilder.DropTable(
                name: "practitioner",
                schema: "workforce");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 108);
        }
    }
}
