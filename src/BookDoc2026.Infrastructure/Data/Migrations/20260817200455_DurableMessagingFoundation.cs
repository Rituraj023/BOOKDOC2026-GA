using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DurableMessagingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "last_error",
                schema: "worker",
                table: "outbox_message",
                newName: "last_error_code");

            migrationBuilder.EnsureSchema(
                name: "communication");

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                schema: "worker",
                table: "outbox_message",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "operation_id",
                schema: "worker",
                table: "outbox_message",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [worker].[outbox_message]
                SET [operation_id] = NEWID(),
                    [correlation_id] = CONCAT(N'migration-', CONVERT(nvarchar(36), NEWID())),
                    [last_error_code] = LEFT([last_error_code], 120);
                """);

            migrationBuilder.AlterColumn<string>(
                name: "correlation_id",
                schema: "worker",
                table: "outbox_message",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "last_error_code",
                schema: "worker",
                table: "outbox_message",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "operation_id",
                schema: "worker",
                table: "outbox_message",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "message_delivery_attempt",
                schema: "communication",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    outbox_message_id = table.Column<long>(type: "bigint", nullable: false),
                    operation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    attempt_number = table.Column<int>(type: "int", nullable: false),
                    channel = table.Column<int>(type: "int", nullable: false),
                    template_key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    template_version = table.Column<int>(type: "int", nullable: false),
                    provider_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    recipient_hint = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    provider_message_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    error_code = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_delivery_attempt", x => x.id);
                    table.CheckConstraint("ck_message_delivery_attempt_number", "[attempt_number] > 0");
                    table.ForeignKey(
                        name: "FK_message_delivery_attempt_branch_tenant_id_branch_id",
                        columns: x => new { x.tenant_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_message_delivery_attempt_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "platform",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "message_template",
                schema: "communication",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    organization_id = table.Column<long>(type: "bigint", nullable: true),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    key = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    template_version = table.Column<int>(type: "int", nullable: false),
                    channel = table.Column<int>(type: "int", nullable: false),
                    culture = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    content_kind = table.Column<int>(type: "int", nullable: false),
                    subject_template = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    body_template = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    published_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_template", x => x.id);
                    table.UniqueConstraint("AK_message_template_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_message_template_scope", "[branch_id] IS NULL OR [organization_id] IS NOT NULL");
                    table.CheckConstraint("ck_message_template_version", "[template_version] > 0");
                    table.ForeignKey(
                        name: "FK_message_template_branch_tenant_id_organization_id_branch_id",
                        columns: x => new { x.tenant_id, x.organization_id, x.branch_id },
                        principalSchema: "org",
                        principalTable: "branch",
                        principalColumns: new[] { "tenant_id", "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_message_template_organization_tenant_id_organization_id",
                        columns: x => new { x.tenant_id, x.organization_id },
                        principalSchema: "org",
                        principalTable: "organization",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_message_template_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "platform",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_operation_id",
                schema: "worker",
                table: "outbox_message",
                column: "operation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_message_delivery_attempt_tenant_id_branch_id_created_utc",
                schema: "communication",
                table: "message_delivery_attempt",
                columns: new[] { "tenant_id", "branch_id", "created_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_message_delivery_attempt_tenant_id_operation_id_channel_attempt_number",
                schema: "communication",
                table: "message_delivery_attempt",
                columns: new[] { "tenant_id", "operation_id", "channel", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_message_delivery_attempt_tenant_id_status_created_utc",
                schema: "communication",
                table: "message_delivery_attempt",
                columns: new[] { "tenant_id", "status", "created_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_message_template_tenant_id_key_channel_culture_status",
                schema: "communication",
                table: "message_template",
                columns: new[] { "tenant_id", "key", "channel", "culture", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_message_template_branch_scope",
                schema: "communication",
                table: "message_template",
                columns: new[] { "tenant_id", "organization_id", "branch_id", "key", "channel", "culture", "template_version" },
                unique: true,
                filter: "[organization_id] IS NOT NULL AND [branch_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_message_template_organization_scope",
                schema: "communication",
                table: "message_template",
                columns: new[] { "tenant_id", "organization_id", "key", "channel", "culture", "template_version" },
                unique: true,
                filter: "[organization_id] IS NOT NULL AND [branch_id] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_message_template_tenant_scope",
                schema: "communication",
                table: "message_template",
                columns: new[] { "tenant_id", "key", "channel", "culture", "template_version" },
                unique: true,
                filter: "[organization_id] IS NULL AND [branch_id] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "message_delivery_attempt",
                schema: "communication");

            migrationBuilder.DropTable(
                name: "message_template",
                schema: "communication");

            migrationBuilder.DropIndex(
                name: "IX_outbox_message_operation_id",
                schema: "worker",
                table: "outbox_message");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                schema: "worker",
                table: "outbox_message");

            migrationBuilder.DropColumn(
                name: "operation_id",
                schema: "worker",
                table: "outbox_message");

            migrationBuilder.AlterColumn<string>(
                name: "last_error_code",
                schema: "worker",
                table: "outbox_message",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "last_error_code",
                schema: "worker",
                table: "outbox_message",
                newName: "last_error");
        }
    }
}
