using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FamilyRelationAndSupervisingDoctor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "supervising_practitioner_id",
                schema: "clinical",
                table: "encounter",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "patient_relation",
                schema: "patient",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    related_patient_id = table.Column<long>(type: "bigint", nullable: false),
                    relationship_type = table.Column<int>(type: "int", nullable: false),
                    is_emergency_contact = table.Column<bool>(type: "bit", nullable: false),
                    is_guardian = table.Column<bool>(type: "bit", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_relation", x => x.id);
                    table.CheckConstraint("ck_patient_relation_not_self", "[patient_id] <> [related_patient_id]");
                    table.ForeignKey(
                        name: "FK_patient_relation_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_relation_patient_tenant_id_related_patient_id",
                        columns: x => new { x.tenant_id, x.related_patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_encounter_tenant_id_branch_id_supervising_practitioner_id",
                schema: "clinical",
                table: "encounter",
                columns: new[] { "tenant_id", "branch_id", "supervising_practitioner_id" });

            migrationBuilder.CreateIndex(
                name: "IX_patient_relation_tenant_id_patient_id_related_patient_id",
                schema: "patient",
                table: "patient_relation",
                columns: new[] { "tenant_id", "patient_id", "related_patient_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patient_relation_tenant_id_related_patient_id",
                schema: "patient",
                table: "patient_relation",
                columns: new[] { "tenant_id", "related_patient_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patient_relation",
                schema: "patient");

            migrationBuilder.DropIndex(
                name: "IX_encounter_tenant_id_branch_id_supervising_practitioner_id",
                schema: "clinical",
                table: "encounter");

            migrationBuilder.DropColumn(
                name: "supervising_practitioner_id",
                schema: "clinical",
                table: "encounter");
        }
    }
}
