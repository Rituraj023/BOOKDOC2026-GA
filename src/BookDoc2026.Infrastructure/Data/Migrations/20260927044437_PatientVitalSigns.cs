using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PatientVitalSigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "patient_vital_signs",
                schema: "clinical",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_id = table.Column<long>(type: "bigint", nullable: true),
                    clinical_encounter_id = table.Column<long>(type: "bigint", nullable: true),
                    systolic_bp = table.Column<int>(type: "int", nullable: true),
                    diastolic_bp = table.Column<int>(type: "int", nullable: true),
                    pulse_bpm = table.Column<int>(type: "int", nullable: true),
                    temperature_f = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    sp_o2_percent = table.Column<int>(type: "int", nullable: true),
                    respiratory_rate = table.Column<int>(type: "int", nullable: true),
                    weight_kg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    height_cm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    bmi = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    blood_glucose_mg_dl = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    recorded_by_actor_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    recorded_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    clinical_notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_vital_signs", x => x.id);
                    table.UniqueConstraint("AK_patient_vital_signs_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_patient_vital_signs_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_vital_signs_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_patient_vital_signs_tenant_id_branch_id_booking_id",
                schema: "clinical",
                table: "patient_vital_signs",
                columns: new[] { "tenant_id", "branch_id", "booking_id" });

            migrationBuilder.CreateIndex(
                name: "IX_patient_vital_signs_tenant_id_branch_id_clinical_encounter_id",
                schema: "clinical",
                table: "patient_vital_signs",
                columns: new[] { "tenant_id", "branch_id", "clinical_encounter_id" });

            migrationBuilder.CreateIndex(
                name: "IX_patient_vital_signs_tenant_id_branch_id_patient_id_recorded_utc",
                schema: "clinical",
                table: "patient_vital_signs",
                columns: new[] { "tenant_id", "branch_id", "patient_id", "recorded_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_patient_vital_signs_tenant_id_patient_id",
                schema: "clinical",
                table: "patient_vital_signs",
                columns: new[] { "tenant_id", "patient_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patient_vital_signs",
                schema: "clinical");
        }
    }
}
