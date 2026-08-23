using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BillingInvoicePaymentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "financial_document_snapshot",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<int>(type: "int", nullable: false),
                    source_id = table.Column<long>(type: "bigint", nullable: false),
                    document_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    schema_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    payload_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_document_snapshot", x => x.id);
                    table.CheckConstraint("ck_billing_document_kind", "[kind] BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_financial_document_snapshot_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_id = table.Column<long>(type: "bigint", nullable: true),
                    contract_id = table.Column<long>(type: "bigint", nullable: true),
                    request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    request_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    invoice_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    discount_total = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    tax_total = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    allocated_amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    calculation_policy_version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    issued_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    issued_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice", x => x.id);
                    table.UniqueConstraint("AK_invoice_tenant_id_branch_id_id", x => new { x.tenant_id, x.branch_id, x.id });
                    table.UniqueConstraint("AK_invoice_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_billing_invoice_amounts", "[subtotal] > 0 AND [discount_total] >= 0 AND [tax_total] >= 0 AND [total] > 0 AND [allocated_amount] >= 0 AND [allocated_amount] <= [total]");
                    table.CheckConstraint("ck_billing_invoice_status", "[status] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_invoice_booking_tenant_id_booking_id",
                        columns: x => new { x.tenant_id, x.booking_id },
                        principalSchema: "scheduling",
                        principalTable: "booking",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_contract_tenant_id_contract_id",
                        columns: x => new { x.tenant_id, x.contract_id },
                        principalSchema: "contract",
                        principalTable: "contract",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    patient_id = table.Column<long>(type: "bigint", nullable: false),
                    request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    request_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    receipt_number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    allocated_amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    confirmed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    received_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment", x => x.id);
                    table.UniqueConstraint("AK_payment_tenant_id_branch_id_id", x => new { x.tenant_id, x.branch_id, x.id });
                    table.UniqueConstraint("AK_payment_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_billing_payment_amounts", "[amount] > 0 AND [allocated_amount] >= 0 AND [allocated_amount] <= [amount]");
                    table.CheckConstraint("ck_billing_payment_status", "[status] BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_payment_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_patient_tenant_id_patient_id",
                        columns: x => new { x.tenant_id, x.patient_id },
                        principalSchema: "patient",
                        principalTable: "patient",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_line",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    service_code_snapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    description_snapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_line", x => x.id);
                    table.CheckConstraint("ck_billing_invoice_line", "[quantity] BETWEEN 1 AND 1000 AND [unit_price] >= 0 AND [line_total] >= 0");
                    table.ForeignKey(
                        name: "FK_invoice_line_invoice_tenant_id_invoice_id",
                        columns: x => new { x.tenant_id, x.invoice_id },
                        principalSchema: "billing",
                        principalTable: "invoice",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_line_service_tenant_id_service_id",
                        columns: x => new { x.tenant_id, x.service_id },
                        principalSchema: "catalog",
                        principalTable: "service",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocation",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    request_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    allocated_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    allocated_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_allocation", x => x.id);
                    table.CheckConstraint("ck_billing_payment_allocation_amount", "[amount] > 0");
                    table.ForeignKey(
                        name: "FK_payment_allocation_invoice_tenant_id_branch_id_invoice_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.invoice_id },
                        principalSchema: "billing",
                        principalTable: "invoice",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_allocation_payment_tenant_id_branch_id_payment_id",
                        columns: x => new { x.tenant_id, x.branch_id, x.payment_id },
                        principalSchema: "billing",
                        principalTable: "payment",
                        principalColumns: new[] { "tenant_id", "branch_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_tender",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    method = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: false),
                    external_reference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    narration = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_tender", x => x.id);
                    table.CheckConstraint("ck_billing_payment_tender_amount", "[amount] > 0");
                    table.CheckConstraint("ck_billing_payment_tender_method", "[method] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_payment_tender_payment_tenant_id_payment_id",
                        columns: x => new { x.tenant_id, x.payment_id },
                        principalSchema: "billing",
                        principalTable: "payment",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 109, "bookdoc:permission", "Billing.Invoices.Issue", 1L },
                    { 110, "bookdoc:permission", "Billing.Invoices.View", 1L },
                    { 111, "bookdoc:permission", "Billing.Payments.Allocate", 1L },
                    { 112, "bookdoc:permission", "Billing.Payments.Receive", 1L },
                    { 113, "bookdoc:permission", "Billing.Payments.View", 1L },
                    { 114, "bookdoc:permission", "Billing.Invoices.Issue", 2L },
                    { 115, "bookdoc:permission", "Billing.Invoices.View", 2L },
                    { 116, "bookdoc:permission", "Billing.Payments.Allocate", 2L },
                    { 117, "bookdoc:permission", "Billing.Payments.Receive", 2L },
                    { 118, "bookdoc:permission", "Billing.Payments.View", 2L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_document_snapshot_tenant_id_branch_id_document_number",
                schema: "billing",
                table: "financial_document_snapshot",
                columns: new[] { "tenant_id", "branch_id", "document_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_document_snapshot_tenant_id_kind_source_id",
                schema: "billing",
                table: "financial_document_snapshot",
                columns: new[] { "tenant_id", "kind", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tenant_id_booking_id",
                schema: "billing",
                table: "invoice",
                columns: new[] { "tenant_id", "booking_id" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tenant_id_branch_id_invoice_number",
                schema: "billing",
                table: "invoice",
                columns: new[] { "tenant_id", "branch_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tenant_id_branch_id_patient_id_status_issued_utc",
                schema: "billing",
                table: "invoice",
                columns: new[] { "tenant_id", "branch_id", "patient_id", "status", "issued_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tenant_id_contract_id",
                schema: "billing",
                table: "invoice",
                columns: new[] { "tenant_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tenant_id_patient_id",
                schema: "billing",
                table: "invoice",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tenant_id_request_id",
                schema: "billing",
                table: "invoice",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_tenant_id_invoice_id",
                schema: "billing",
                table: "invoice_line",
                columns: new[] { "tenant_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_tenant_id_service_id",
                schema: "billing",
                table: "invoice_line",
                columns: new[] { "tenant_id", "service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_tenant_id_branch_id_patient_id_confirmed_utc",
                schema: "billing",
                table: "payment",
                columns: new[] { "tenant_id", "branch_id", "patient_id", "confirmed_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_tenant_id_branch_id_receipt_number",
                schema: "billing",
                table: "payment",
                columns: new[] { "tenant_id", "branch_id", "receipt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_tenant_id_patient_id",
                schema: "billing",
                table: "payment",
                columns: new[] { "tenant_id", "patient_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_tenant_id_request_id",
                schema: "billing",
                table: "payment",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocation_tenant_id_branch_id_invoice_id",
                schema: "billing",
                table: "payment_allocation",
                columns: new[] { "tenant_id", "branch_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocation_tenant_id_branch_id_payment_id",
                schema: "billing",
                table: "payment_allocation",
                columns: new[] { "tenant_id", "branch_id", "payment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocation_tenant_id_payment_id_invoice_id",
                schema: "billing",
                table: "payment_allocation",
                columns: new[] { "tenant_id", "payment_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocation_tenant_id_request_id",
                schema: "billing",
                table: "payment_allocation",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_tender_tenant_id_payment_id",
                schema: "billing",
                table: "payment_tender",
                columns: new[] { "tenant_id", "payment_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_document_snapshot",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoice_line",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "payment_allocation",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "payment_tender",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "invoice",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "payment",
                schema: "billing");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 118);
        }
    }
}
