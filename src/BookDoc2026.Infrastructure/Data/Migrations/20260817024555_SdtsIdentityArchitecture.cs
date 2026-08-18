using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class SdtsIdentityArchitecture : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "branch_permission_grant",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "branch_user",
            schema: "identity");

        migrationBuilder.DropIndex(
            name: "IX_branch_tenant_id_organization_id",
            schema: "org",
            table: "branch");

        migrationBuilder.AddUniqueConstraint(
            name: "AK_branch_tenant_id_organization_id_id",
            schema: "org",
            table: "branch",
            columns: new[] { "tenant_id", "organization_id", "id" });

        migrationBuilder.CreateTable(
            name: "role",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                is_platform_role = table.Column<bool>(type: "bit", nullable: false),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                normalized_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "user",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                stakeholder_id = table.Column<long>(type: "bigint", nullable: true),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                whats_app_number_confirmed = table.Column<bool>(type: "bit", nullable: false),
                display_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                refresh_token_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                refresh_token_expires_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                refresh_token_scope_id = table.Column<long>(type: "bigint", nullable: true),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                normalized_user_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                normalized_email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                email_confirmed = table.Column<bool>(type: "bit", nullable: false),
                password_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                security_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                phone_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                phone_number_confirmed = table.Column<bool>(type: "bit", nullable: false),
                two_factor_enabled = table.Column<bool>(type: "bit", nullable: false),
                lockout_end = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                lockout_enabled = table.Column<bool>(type: "bit", nullable: false),
                access_failed_count = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "role_claim",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                role_id = table.Column<long>(type: "bigint", nullable: false),
                claim_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                claim_value = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_claim", x => x.id);
                table.ForeignKey(
                    name: "FK_role_claim_role_role_id",
                    column: x => x.role_id,
                    principalSchema: "identity",
                    principalTable: "role",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "user_claim",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                user_id = table.Column<long>(type: "bigint", nullable: false),
                claim_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                claim_value = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_claim", x => x.id);
                table.ForeignKey(
                    name: "FK_user_claim_user_user_id",
                    column: x => x.user_id,
                    principalSchema: "identity",
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "user_login",
            schema: "identity",
            columns: table => new
            {
                login_provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                provider_key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                provider_display_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                user_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_login", x => new { x.login_provider, x.provider_key });
                table.ForeignKey(
                    name: "FK_user_login_user_user_id",
                    column: x => x.user_id,
                    principalSchema: "identity",
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "user_role",
            schema: "identity",
            columns: table => new
            {
                user_id = table.Column<long>(type: "bigint", nullable: false),
                role_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_role", x => new { x.user_id, x.role_id });
                table.ForeignKey(
                    name: "FK_user_role_role_role_id",
                    column: x => x.role_id,
                    principalSchema: "identity",
                    principalTable: "role",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_user_role_user_user_id",
                    column: x => x.user_id,
                    principalSchema: "identity",
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "user_scope",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                user_id = table.Column<long>(type: "bigint", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false),
                organization_id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: true),
                is_default = table.Column<bool>(type: "bit", nullable: false),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_scope", x => x.id);
                table.ForeignKey(
                    name: "FK_user_scope_branch_tenant_id_organization_id_branch_id",
                    columns: x => new { x.tenant_id, x.organization_id, x.branch_id },
                    principalSchema: "org",
                    principalTable: "branch",
                    principalColumns: new[] { "tenant_id", "organization_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_user_scope_organization_tenant_id_organization_id",
                    columns: x => new { x.tenant_id, x.organization_id },
                    principalSchema: "org",
                    principalTable: "organization",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_user_scope_tenant_tenant_id",
                    column: x => x.tenant_id,
                    principalSchema: "platform",
                    principalTable: "tenant",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_user_scope_user_user_id",
                    column: x => x.user_id,
                    principalSchema: "identity",
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "user_token",
            schema: "identity",
            columns: table => new
            {
                user_id = table.Column<long>(type: "bigint", nullable: false),
                login_provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                is_blocked = table.Column<bool>(type: "bit", nullable: false),
                value = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_token", x => new { x.user_id, x.login_provider, x.name });
                table.ForeignKey(
                    name: "FK_user_token_user_user_id",
                    column: x => x.user_id,
                    principalSchema: "identity",
                    principalTable: "user",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            schema: "identity",
            table: "role",
            columns: new[] { "id", "concurrency_stamp", "is_active", "is_platform_role", "name", "normalized_name" },
            values: new object[,]
            {
                { 1L, "bookdoc-platform-operator-v1", true, true, "PlatformOperator", "PLATFORMOPERATOR" },
                { 2L, "bookdoc-clinic-administrator-v1", true, false, "ClinicAdministrator", "CLINICADMINISTRATOR" }
            });

        migrationBuilder.InsertData(
            schema: "identity",
            table: "role_claim",
            columns: new[] { "id", "claim_type", "claim_value", "role_id" },
            values: new object[,]
            {
                { 1, "bookdoc:permission", "Branches.Configuration.Manage", 1L },
                { 2, "bookdoc:permission", "Branches.View", 1L },
                { 3, "bookdoc:permission", "Catalog.Manage", 1L },
                { 4, "bookdoc:permission", "Catalog.View", 1L },
                { 5, "bookdoc:permission", "Patients.Register", 1L },
                { 6, "bookdoc:permission", "Patients.Search", 1L },
                { 7, "bookdoc:permission", "Patients.Update", 1L },
                { 8, "bookdoc:permission", "Patients.View", 1L },
                { 9, "bookdoc:permission", "Resources.Manage", 1L },
                { 10, "bookdoc:permission", "Resources.Status.Manage", 1L },
                { 11, "bookdoc:permission", "Resources.View", 1L },
                { 12, "bookdoc:permission", "Scheduling.Availability.Manage", 1L },
                { 13, "bookdoc:permission", "Scheduling.Availability.View", 1L },
                { 14, "bookdoc:permission", "Scheduling.Holds.Create", 1L },
                { 15, "bookdoc:permission", "Scheduling.Holds.Release", 1L },
                { 16, "bookdoc:permission", "Stakeholders.Documents.Manage", 1L },
                { 17, "bookdoc:permission", "Stakeholders.Manage", 1L },
                { 18, "bookdoc:permission", "Stakeholders.View", 1L },
                { 19, "bookdoc:permission", "Tenants.Approve", 1L },
                { 20, "bookdoc:permission", "Tenants.Register", 1L },
                { 21, "bookdoc:permission", "Users.BranchAdministrators.Manage", 1L },
                { 22, "bookdoc:permission", "Users.Manage", 1L },
                { 23, "bookdoc:permission", "Branches.Configuration.Manage", 2L },
                { 24, "bookdoc:permission", "Branches.View", 2L },
                { 25, "bookdoc:permission", "Catalog.Manage", 2L },
                { 26, "bookdoc:permission", "Catalog.View", 2L },
                { 27, "bookdoc:permission", "Patients.Register", 2L },
                { 28, "bookdoc:permission", "Patients.Search", 2L },
                { 29, "bookdoc:permission", "Patients.Update", 2L },
                { 30, "bookdoc:permission", "Patients.View", 2L },
                { 31, "bookdoc:permission", "Resources.Manage", 2L },
                { 32, "bookdoc:permission", "Resources.Status.Manage", 2L },
                { 33, "bookdoc:permission", "Resources.View", 2L },
                { 34, "bookdoc:permission", "Scheduling.Availability.Manage", 2L },
                { 35, "bookdoc:permission", "Scheduling.Availability.View", 2L },
                { 36, "bookdoc:permission", "Scheduling.Holds.Create", 2L },
                { 37, "bookdoc:permission", "Scheduling.Holds.Release", 2L },
                { 38, "bookdoc:permission", "Stakeholders.Documents.Manage", 2L },
                { 39, "bookdoc:permission", "Stakeholders.Manage", 2L },
                { 40, "bookdoc:permission", "Stakeholders.View", 2L }
            });

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            schema: "identity",
            table: "role",
            column: "normalized_name",
            unique: true,
            filter: "[normalized_name] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_role_claim_role_id",
            schema: "identity",
            table: "role_claim",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            schema: "identity",
            table: "user",
            column: "normalized_email");

        migrationBuilder.CreateIndex(
            name: "IX_user_refresh_token_hash",
            schema: "identity",
            table: "user",
            column: "refresh_token_hash",
            unique: true,
            filter: "[refresh_token_hash] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            schema: "identity",
            table: "user",
            column: "normalized_user_name",
            unique: true,
            filter: "[normalized_user_name] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_user_claim_user_id",
            schema: "identity",
            table: "user_claim",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_user_login_user_id",
            schema: "identity",
            table: "user_login",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_user_role_role_id",
            schema: "identity",
            table: "user_role",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "IX_user_scope_tenant_id_organization_id_branch_id",
            schema: "identity",
            table: "user_scope",
            columns: new[] { "tenant_id", "organization_id", "branch_id" });

        migrationBuilder.CreateIndex(
            name: "IX_user_scope_user_id_is_default",
            schema: "identity",
            table: "user_scope",
            columns: new[] { "user_id", "is_default" },
            unique: true,
            filter: "[is_default] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_user_scope_user_id_tenant_id_organization_id_branch_id",
            schema: "identity",
            table: "user_scope",
            columns: new[] { "user_id", "tenant_id", "organization_id", "branch_id" },
            unique: true,
            filter: "[branch_id] IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "role_claim",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "user_claim",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "user_login",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "user_role",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "user_scope",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "user_token",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "role",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "user",
            schema: "identity");

        migrationBuilder.DropUniqueConstraint(
            name: "AK_branch_tenant_id_organization_id_id",
            schema: "org",
            table: "branch");

        migrationBuilder.CreateTable(
            name: "branch_user",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                display_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                role_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                subject_id = table.Column<long>(type: "bigint", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_branch_user", x => x.id);
                table.UniqueConstraint("AK_branch_user_tenant_id_branch_id_id", x => new { x.tenant_id, x.branch_id, x.id });
                table.ForeignKey(
                    name: "FK_branch_user_branch_tenant_id_branch_id",
                    columns: x => new { x.tenant_id, x.branch_id },
                    principalSchema: "org",
                    principalTable: "branch",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "branch_permission_grant",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: false),
                branch_user_id = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                granted_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                permission = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_branch_permission_grant", x => x.id);
                table.ForeignKey(
                    name: "FK_branch_permission_grant_branch_user_tenant_id_branch_id_branch_user_id",
                    columns: x => new { x.tenant_id, x.branch_id, x.branch_user_id },
                    principalSchema: "identity",
                    principalTable: "branch_user",
                    principalColumns: new[] { "tenant_id", "branch_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_branch_tenant_id_organization_id",
            schema: "org",
            table: "branch",
            columns: new[] { "tenant_id", "organization_id" });

        migrationBuilder.CreateIndex(
            name: "IX_branch_permission_grant_tenant_id_branch_id_branch_user_id_permission",
            schema: "identity",
            table: "branch_permission_grant",
            columns: new[] { "tenant_id", "branch_id", "branch_user_id", "permission" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_branch_user_tenant_id_branch_id_subject_id",
            schema: "identity",
            table: "branch_user",
            columns: new[] { "tenant_id", "branch_id", "subject_id" },
            unique: true);
    }
}
