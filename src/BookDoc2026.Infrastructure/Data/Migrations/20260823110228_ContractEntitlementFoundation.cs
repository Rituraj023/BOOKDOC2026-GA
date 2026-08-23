using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContractEntitlementFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "contract");

            migrationBuilder.CreateTable(
                name: "contract",
                schema: "contract",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_number = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    contract_type_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<byte>(type: "tinyint", nullable: false),
                    package_price = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    rule_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract", x => x.id);
                    table.UniqueConstraint("AK_contract_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_contract_status", "[status] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_contract_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entitlement",
                schema: "contract",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    resource_category_id = table.Column<long>(type: "bigint", nullable: true),
                    total_units = table.Column<int>(type: "int", nullable: false),
                    reserved_units = table.Column<int>(type: "int", nullable: false),
                    consumed_units = table.Column<int>(type: "int", nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    rule_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entitlement", x => x.id);
                    table.UniqueConstraint("AK_entitlement_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_contract_entitlement_units", "[total_units] >= 1 AND [reserved_units] >= 0 AND [consumed_units] >= 0 AND [reserved_units] + [consumed_units] <= [total_units]");
                    table.ForeignKey(
                        name: "FK_entitlement_contract_tenant_id_contract_id",
                        columns: x => new { x.tenant_id, x.contract_id },
                        principalSchema: "contract",
                        principalTable: "contract",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_entitlement_resource_category_tenant_id_resource_category_id",
                        columns: x => new { x.tenant_id, x.resource_category_id },
                        principalSchema: "resource",
                        principalTable: "resource_category",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_entitlement_service_tenant_id_service_id",
                        columns: x => new { x.tenant_id, x.service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entitlement_reservation",
                schema: "contract",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    entitlement_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    request_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    units = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<byte>(type: "tinyint", nullable: false),
                    reserved_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    consumed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    released_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    release_reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entitlement_reservation", x => x.id);
                    table.CheckConstraint("ck_entitlement_reservation_status", "[status] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_entitlement_reservation_booking_tenant_id_booking_id",
                        columns: x => new { x.tenant_id, x.booking_id },
                        principalSchema: "scheduling",
                        principalTable: "booking",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_entitlement_reservation_contract_tenant_id_contract_id",
                        columns: x => new { x.tenant_id, x.contract_id },
                        principalSchema: "contract",
                        principalTable: "contract",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_entitlement_reservation_entitlement_tenant_id_entitlement_id",
                        columns: x => new { x.tenant_id, x.entitlement_id },
                        principalSchema: "contract",
                        principalTable: "entitlement",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 83, "bookdoc:permission", "Contracts.Entitlements.Consume", 1L },
                    { 84, "bookdoc:permission", "Contracts.Entitlements.Release", 1L },
                    { 85, "bookdoc:permission", "Contracts.Entitlements.Reserve", 1L },
                    { 86, "bookdoc:permission", "Contracts.Manage", 1L },
                    { 87, "bookdoc:permission", "Contracts.View", 1L },
                    { 88, "bookdoc:permission", "Contracts.Entitlements.Consume", 2L },
                    { 89, "bookdoc:permission", "Contracts.Entitlements.Release", 2L },
                    { 90, "bookdoc:permission", "Contracts.Entitlements.Reserve", 2L },
                    { 91, "bookdoc:permission", "Contracts.Manage", 2L },
                    { 92, "bookdoc:permission", "Contracts.View", 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_contract_tenant_id_branch_id_contract_number",
                schema: "contract",
                table: "contract",
                columns: new[] { "tenant_id", "branch_id", "contract_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contract_tenant_id_branch_id_patient_id_status_valid_to",
                schema: "contract",
                table: "contract",
                columns: new[] { "tenant_id", "branch_id", "patient_id", "status", "valid_to" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_tenant_id_patient_id",
                schema: "contract",
                table: "contract",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_entitlement_tenant_id_contract_id_service_id",
                schema: "contract",
                table: "entitlement",
                columns: new[] { "tenant_id", "contract_id", "service_id" },
                unique: true,
                filter: "[resource_category_id] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_entitlement_tenant_id_contract_id_service_id_resource_category_id",
                schema: "contract",
                table: "entitlement",
                columns: new[] { "tenant_id", "contract_id", "service_id", "resource_category_id" },
                unique: true,
                filter: "[resource_category_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_entitlement_tenant_id_resource_category_id",
                schema: "contract",
                table: "entitlement",
                columns: new[] { "tenant_id", "resource_category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_entitlement_tenant_id_service_id",
                schema: "contract",
                table: "entitlement",
                columns: new[] { "tenant_id", "service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_entitlement_reservation_tenant_id_booking_id",
                schema: "contract",
                table: "entitlement_reservation",
                columns: new[] { "tenant_id", "booking_id" });

            migrationBuilder.CreateIndex(
                name: "IX_entitlement_reservation_tenant_id_contract_id",
                schema: "contract",
                table: "entitlement_reservation",
                columns: new[] { "tenant_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_entitlement_reservation_tenant_id_entitlement_id_booking_id",
                schema: "contract",
                table: "entitlement_reservation",
                columns: new[] { "tenant_id", "entitlement_id", "booking_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entitlement_reservation_tenant_id_request_id",
                schema: "contract",
                table: "entitlement_reservation",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entitlement_reservation",
                schema: "contract");

            migrationBuilder.DropTable(
                name: "entitlement",
                schema: "contract");

            migrationBuilder.DropTable(
                name: "contract",
                schema: "contract");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 92);
        }
    }
}
