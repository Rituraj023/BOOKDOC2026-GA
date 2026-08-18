using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookDoc2026.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class NumericKeyBaseline : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "stakeholder");

        migrationBuilder.EnsureSchema(
            name: "audit");

        migrationBuilder.EnsureSchema(
            name: "scheduling");

        migrationBuilder.EnsureSchema(
            name: "resource");

        migrationBuilder.EnsureSchema(
            name: "org");

        migrationBuilder.EnsureSchema(
            name: "identity");

        migrationBuilder.EnsureSchema(
            name: "worker");

        migrationBuilder.EnsureSchema(
            name: "patient");

        migrationBuilder.EnsureSchema(
            name: "catalog");

        migrationBuilder.EnsureSchema(
            name: "platform");

        migrationBuilder.CreateTable(
            name: "audit_event",
            schema: "audit",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: true),
                branch_id = table.Column<long>(type: "bigint", nullable: true),
                actor_id = table.Column<long>(type: "bigint", nullable: false),
                action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                entity_type = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                entity_id = table.Column<long>(type: "bigint", nullable: false),
                data_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_audit_event", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_message",
            schema: "worker",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: true),
                branch_id = table.Column<long>(type: "bigint", nullable: true),
                message_type = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                attempt_count = table.Column<int>(type: "int", nullable: false),
                next_attempt_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                lease_owner = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                lease_until_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                last_error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_message", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "resource_category",
            schema: "resource",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                parent_category_id = table.Column<long>(type: "bigint", nullable: true),
                code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                kind = table.Column<int>(type: "int", nullable: false),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resource_category", x => x.id);
                table.UniqueConstraint("AK_resource_category_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_resource_category_kind", "[kind] BETWEEN 1 AND 7");
                table.ForeignKey(
                    name: "FK_resource_category_resource_category_tenant_id_parent_category_id",
                    columns: x => new { x.tenant_id, x.parent_category_id },
                    principalSchema: "resource",
                    principalTable: "resource_category",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "service",
            schema: "catalog",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                default_duration_minutes = table.Column<int>(type: "int", nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_service", x => x.id);
                table.UniqueConstraint("AK_service_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_catalog_service_duration", "[default_duration_minutes] BETWEEN 5 AND 1440");
                table.CheckConstraint("ck_catalog_service_status", "[status] BETWEEN 1 AND 2");
            });

        migrationBuilder.CreateTable(
            name: "stakeholder",
            schema: "stakeholder",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                type = table.Column<int>(type: "int", nullable: false),
                display_name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stakeholder", x => x.id);
                table.UniqueConstraint("AK_stakeholder_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_stakeholder_status", "[status] BETWEEN 1 AND 2");
                table.CheckConstraint("ck_stakeholder_type", "[type] BETWEEN 1 AND 2");
            });

        migrationBuilder.CreateTable(
            name: "tenant",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                legal_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tenant", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tenant_application",
            schema: "platform",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                legal_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                contact_email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                first_branch_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                first_branch_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                submission_source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                reviewed_by_actor_id = table.Column<long>(type: "bigint", nullable: true),
                reviewed_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tenant_application", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "service_resource_requirement",
            schema: "catalog",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                service_id = table.Column<long>(type: "bigint", nullable: false),
                category_id = table.Column<long>(type: "bigint", nullable: false),
                role_code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                quantity = table.Column<int>(type: "int", nullable: false),
                is_optional = table.Column<bool>(type: "bit", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_service_resource_requirement", x => x.id);
                table.CheckConstraint("ck_service_resource_requirement_quantity", "[quantity] BETWEEN 1 AND 1000");
                table.ForeignKey(
                    name: "FK_service_resource_requirement_resource_category_tenant_id_category_id",
                    columns: x => new { x.tenant_id, x.category_id },
                    principalSchema: "resource",
                    principalTable: "resource_category",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_service_resource_requirement_service_tenant_id_service_id",
                    columns: x => new { x.tenant_id, x.service_id },
                    principalSchema: "catalog",
                    principalTable: "service",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "address",
            schema: "stakeholder",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                stakeholder_id = table.Column<long>(type: "bigint", nullable: false),
                address_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                state_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                postal_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                country_code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                is_primary = table.Column<bool>(type: "bit", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_address", x => x.id);
                table.CheckConstraint("ck_stakeholder_address_india_postal_code", "[country_code] = 'IN' AND LEN([postal_code]) = 6 AND [postal_code] NOT LIKE '%[^0-9]%'");
                table.ForeignKey(
                    name: "FK_address_stakeholder_tenant_id_stakeholder_id",
                    columns: x => new { x.tenant_id, x.stakeholder_id },
                    principalSchema: "stakeholder",
                    principalTable: "stakeholder",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "contact_point",
            schema: "stakeholder",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                stakeholder_id = table.Column<long>(type: "bigint", nullable: false),
                type = table.Column<int>(type: "int", nullable: false),
                value = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                normalized_value = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                is_primary = table.Column<bool>(type: "bit", nullable: false),
                is_verified = table.Column<bool>(type: "bit", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_contact_point", x => x.id);
                table.CheckConstraint("ck_stakeholder_contact_type", "[type] BETWEEN 1 AND 2");
                table.ForeignKey(
                    name: "FK_contact_point_stakeholder_tenant_id_stakeholder_id",
                    columns: x => new { x.tenant_id, x.stakeholder_id },
                    principalSchema: "stakeholder",
                    principalTable: "stakeholder",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "corporate",
            schema: "stakeholder",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                stakeholder_id = table.Column<long>(type: "bigint", nullable: false),
                legal_name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                trade_name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                registration_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_corporate", x => x.id);
                table.ForeignKey(
                    name: "FK_corporate_stakeholder_tenant_id_stakeholder_id",
                    columns: x => new { x.tenant_id, x.stakeholder_id },
                    principalSchema: "stakeholder",
                    principalTable: "stakeholder",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "document_reference",
            schema: "stakeholder",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                stakeholder_id = table.Column<long>(type: "bigint", nullable: false),
                document_type_id = table.Column<long>(type: "bigint", nullable: false),
                file_id = table.Column<long>(type: "bigint", nullable: false),
                reference_number = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                issued_on = table.Column<DateOnly>(type: "date", nullable: true),
                expires_on = table.Column<DateOnly>(type: "date", nullable: true),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_reference", x => x.id);
                table.ForeignKey(
                    name: "FK_document_reference_stakeholder_tenant_id_stakeholder_id",
                    columns: x => new { x.tenant_id, x.stakeholder_id },
                    principalSchema: "stakeholder",
                    principalTable: "stakeholder",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "identifier",
            schema: "stakeholder",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                stakeholder_id = table.Column<long>(type: "bigint", nullable: false),
                type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                normalized_value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                issuer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_identifier", x => x.id);
                table.ForeignKey(
                    name: "FK_identifier_stakeholder_tenant_id_stakeholder_id",
                    columns: x => new { x.tenant_id, x.stakeholder_id },
                    principalSchema: "stakeholder",
                    principalTable: "stakeholder",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "person",
            schema: "stakeholder",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                stakeholder_id = table.Column<long>(type: "bigint", nullable: false),
                honorific = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                given_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                middle_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                family_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                normalized_search_name = table.Column<string>(type: "nvarchar(302)", maxLength: 302, nullable: false),
                date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                is_date_of_birth_estimated = table.Column<bool>(type: "bit", nullable: false),
                administrative_sex = table.Column<int>(type: "int", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_person", x => x.id);
                table.CheckConstraint("ck_stakeholder_person_sex", "[administrative_sex] BETWEEN 0 AND 3");
                table.ForeignKey(
                    name: "FK_person_stakeholder_tenant_id_stakeholder_id",
                    columns: x => new { x.tenant_id, x.stakeholder_id },
                    principalSchema: "stakeholder",
                    principalTable: "stakeholder",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "organization",
            schema: "org",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_organization", x => x.id);
                table.UniqueConstraint("AK_organization_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "FK_organization_tenant_tenant_id",
                    column: x => x.tenant_id,
                    principalSchema: "platform",
                    principalTable: "tenant",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "branch",
            schema: "org",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                organization_id = table.Column<long>(type: "bigint", nullable: false),
                code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                time_zone_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_branch", x => x.id);
                table.UniqueConstraint("AK_branch_tenant_id_id", x => new { x.tenant_id, x.id });
                table.ForeignKey(
                    name: "FK_branch_organization_tenant_id_organization_id",
                    columns: x => new { x.tenant_id, x.organization_id },
                    principalSchema: "org",
                    principalTable: "organization",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "bookable_resource",
            schema: "resource",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: false),
                category_id = table.Column<long>(type: "bigint", nullable: false),
                code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                capacity_mode = table.Column<int>(type: "int", nullable: false),
                capacity = table.Column<int>(type: "int", nullable: false),
                time_zone_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                external_reference_id = table.Column<long>(type: "bigint", nullable: true),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                operational_status = table.Column<int>(type: "int", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bookable_resource", x => x.id);
                table.UniqueConstraint("AK_bookable_resource_tenant_id_branch_id_id", x => new { x.tenant_id, x.branch_id, x.id });
                table.UniqueConstraint("AK_bookable_resource_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_bookable_resource_capacity", "[capacity] BETWEEN 1 AND 1000");
                table.CheckConstraint("ck_bookable_resource_capacity_mode", "[capacity_mode] BETWEEN 1 AND 2");
                table.CheckConstraint("ck_bookable_resource_exclusive_capacity", "[capacity_mode] <> 1 OR [capacity] = 1");
                table.CheckConstraint("ck_bookable_resource_status", "[operational_status] BETWEEN 1 AND 5");
                table.ForeignKey(
                    name: "FK_bookable_resource_branch_tenant_id_branch_id",
                    columns: x => new { x.tenant_id, x.branch_id },
                    principalSchema: "org",
                    principalTable: "branch",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_bookable_resource_resource_category_tenant_id_category_id",
                    columns: x => new { x.tenant_id, x.category_id },
                    principalSchema: "resource",
                    principalTable: "resource_category",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "branch_configuration",
            schema: "org",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: false),
                logo_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                invoice_prefix = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                receipt_prefix = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                email_sender = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                whats_app_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                communication_verification_status = table.Column<int>(type: "int", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_branch_configuration", x => x.id);
                table.ForeignKey(
                    name: "FK_branch_configuration_branch_tenant_id_branch_id",
                    columns: x => new { x.tenant_id, x.branch_id },
                    principalSchema: "org",
                    principalTable: "branch",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "branch_user",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: false),
                subject_id = table.Column<long>(type: "bigint", nullable: false),
                display_name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                role_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
            name: "patient",
            schema: "patient",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                registration_branch_id = table.Column<long>(type: "bigint", nullable: false),
                registration_request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                registration_payload_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                stakeholder_id = table.Column<long>(type: "bigint", nullable: false),
                patient_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                blood_group = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_patient", x => x.id);
                table.UniqueConstraint("AK_patient_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_patient_status", "[status] BETWEEN 1 AND 5");
                table.ForeignKey(
                    name: "FK_patient_branch_tenant_id_registration_branch_id",
                    columns: x => new { x.tenant_id, x.registration_branch_id },
                    principalSchema: "org",
                    principalTable: "branch",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_patient_stakeholder_tenant_id_stakeholder_id",
                    columns: x => new { x.tenant_id, x.stakeholder_id },
                    principalSchema: "stakeholder",
                    principalTable: "stakeholder",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "availability_exception",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: false),
                resource_id = table.Column<long>(type: "bigint", nullable: false),
                start_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                end_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                kind = table.Column<int>(type: "int", nullable: false),
                capacity_override = table.Column<int>(type: "int", nullable: true),
                reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_availability_exception", x => x.id);
                table.CheckConstraint("ck_availability_exception_interval", "[start_utc] < [end_utc]");
                table.CheckConstraint("ck_availability_exception_kind", "[kind] BETWEEN 1 AND 2");
                table.ForeignKey(
                    name: "FK_availability_exception_bookable_resource_tenant_id_branch_id_resource_id",
                    columns: x => new { x.tenant_id, x.branch_id, x.resource_id },
                    principalSchema: "resource",
                    principalTable: "bookable_resource",
                    principalColumns: new[] { "tenant_id", "branch_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "availability_rule",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: false),
                resource_id = table.Column<long>(type: "bigint", nullable: false),
                service_id = table.Column<long>(type: "bigint", nullable: true),
                day_of_week = table.Column<int>(type: "int", nullable: false),
                local_start = table.Column<TimeOnly>(type: "time", nullable: false),
                local_end = table.Column<TimeOnly>(type: "time", nullable: false),
                effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                slot_interval_minutes = table.Column<int>(type: "int", nullable: false),
                capacity = table.Column<int>(type: "int", nullable: false),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_availability_rule", x => x.id);
                table.CheckConstraint("ck_availability_rule_capacity", "[capacity] BETWEEN 1 AND 1000");
                table.CheckConstraint("ck_availability_rule_interval", "[local_start] < [local_end]");
                table.CheckConstraint("ck_availability_rule_slot", "[slot_interval_minutes] BETWEEN 5 AND 1440");
                table.ForeignKey(
                    name: "FK_availability_rule_bookable_resource_tenant_id_branch_id_resource_id",
                    columns: x => new { x.tenant_id, x.branch_id, x.resource_id },
                    principalSchema: "resource",
                    principalTable: "bookable_resource",
                    principalColumns: new[] { "tenant_id", "branch_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_availability_rule_branch_tenant_id_branch_id",
                    columns: x => new { x.tenant_id, x.branch_id },
                    principalSchema: "org",
                    principalTable: "branch",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_availability_rule_service_tenant_id_service_id",
                    columns: x => new { x.tenant_id, x.service_id },
                    principalSchema: "catalog",
                    principalTable: "service",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "resource_capability",
            schema: "resource",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                resource_id = table.Column<long>(type: "bigint", nullable: false),
                service_id = table.Column<long>(type: "bigint", nullable: false),
                duration_override_minutes = table.Column<int>(type: "int", nullable: true),
                capacity_required = table.Column<int>(type: "int", nullable: false),
                is_active = table.Column<bool>(type: "bit", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resource_capability", x => x.id);
                table.CheckConstraint("ck_resource_capability_capacity", "[capacity_required] BETWEEN 1 AND 1000");
                table.CheckConstraint("ck_resource_capability_duration", "[duration_override_minutes] IS NULL OR [duration_override_minutes] BETWEEN 5 AND 1440");
                table.ForeignKey(
                    name: "FK_resource_capability_bookable_resource_tenant_id_resource_id",
                    columns: x => new { x.tenant_id, x.resource_id },
                    principalSchema: "resource",
                    principalTable: "bookable_resource",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_resource_capability_service_tenant_id_service_id",
                    columns: x => new { x.tenant_id, x.service_id },
                    principalSchema: "catalog",
                    principalTable: "service",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "resource_status_event",
            schema: "resource",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: false),
                resource_id = table.Column<long>(type: "bigint", nullable: false),
                from_status = table.Column<int>(type: "int", nullable: false),
                to_status = table.Column<int>(type: "int", nullable: false),
                reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                actor_id = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resource_status_event", x => x.id);
                table.CheckConstraint("ck_resource_status_event_from", "[from_status] BETWEEN 1 AND 5");
                table.CheckConstraint("ck_resource_status_event_to", "[to_status] BETWEEN 1 AND 5");
                table.ForeignKey(
                    name: "FK_resource_status_event_bookable_resource_tenant_id_branch_id_resource_id",
                    columns: x => new { x.tenant_id, x.branch_id, x.resource_id },
                    principalSchema: "resource",
                    principalTable: "bookable_resource",
                    principalColumns: new[] { "tenant_id", "branch_id", "id" },
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
                permission = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                granted_by_actor_id = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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

        migrationBuilder.CreateTable(
            name: "hold",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: false),
                patient_id = table.Column<long>(type: "bigint", nullable: false),
                service_id = table.Column<long>(type: "bigint", nullable: false),
                request_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                payload_hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                start_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                end_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                expires_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                status = table.Column<int>(type: "int", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_hold", x => x.id);
                table.UniqueConstraint("AK_hold_tenant_id_id", x => new { x.tenant_id, x.id });
                table.CheckConstraint("ck_scheduling_hold_interval", "[start_utc] < [end_utc]");
                table.CheckConstraint("ck_scheduling_hold_status", "[status] BETWEEN 1 AND 4");
                table.ForeignKey(
                    name: "FK_hold_branch_tenant_id_branch_id",
                    columns: x => new { x.tenant_id, x.branch_id },
                    principalSchema: "org",
                    principalTable: "branch",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_hold_patient_tenant_id_patient_id",
                    columns: x => new { x.tenant_id, x.patient_id },
                    principalSchema: "patient",
                    principalTable: "patient",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_hold_service_tenant_id_service_id",
                    columns: x => new { x.tenant_id, x.service_id },
                    principalSchema: "catalog",
                    principalTable: "service",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "resource_reservation",
            schema: "scheduling",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false),
                branch_id = table.Column<long>(type: "bigint", nullable: false),
                hold_id = table.Column<long>(type: "bigint", nullable: false),
                resource_id = table.Column<long>(type: "bigint", nullable: false),
                start_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                end_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                quantity = table.Column<int>(type: "int", nullable: false),
                created_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                modified_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                tenant_id = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resource_reservation", x => x.id);
                table.CheckConstraint("ck_resource_reservation_interval", "[start_utc] < [end_utc]");
                table.CheckConstraint("ck_resource_reservation_quantity", "[quantity] BETWEEN 1 AND 1000");
                table.ForeignKey(
                    name: "FK_resource_reservation_bookable_resource_tenant_id_branch_id_resource_id",
                    columns: x => new { x.tenant_id, x.branch_id, x.resource_id },
                    principalSchema: "resource",
                    principalTable: "bookable_resource",
                    principalColumns: new[] { "tenant_id", "branch_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_resource_reservation_hold_tenant_id_hold_id",
                    columns: x => new { x.tenant_id, x.hold_id },
                    principalSchema: "scheduling",
                    principalTable: "hold",
                    principalColumns: new[] { "tenant_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_address_tenant_id_stakeholder_id",
            schema: "stakeholder",
            table: "address",
            columns: new[] { "tenant_id", "stakeholder_id" },
            unique: true,
            filter: "[is_primary] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_address_tenant_id_stakeholder_id_address_type",
            schema: "stakeholder",
            table: "address",
            columns: new[] { "tenant_id", "stakeholder_id", "address_type" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_audit_event_entity_type_entity_id",
            schema: "audit",
            table: "audit_event",
            columns: new[] { "entity_type", "entity_id" });

        migrationBuilder.CreateIndex(
            name: "IX_audit_event_tenant_id_created_utc",
            schema: "audit",
            table: "audit_event",
            columns: new[] { "tenant_id", "created_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_availability_exception_tenant_id_branch_id_resource_id_start_utc_end_utc",
            schema: "scheduling",
            table: "availability_exception",
            columns: new[] { "tenant_id", "branch_id", "resource_id", "start_utc", "end_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_availability_rule_tenant_id_branch_id_resource_id_day_of_week_is_active",
            schema: "scheduling",
            table: "availability_rule",
            columns: new[] { "tenant_id", "branch_id", "resource_id", "day_of_week", "is_active" });

        migrationBuilder.CreateIndex(
            name: "IX_availability_rule_tenant_id_service_id",
            schema: "scheduling",
            table: "availability_rule",
            columns: new[] { "tenant_id", "service_id" });

        migrationBuilder.CreateIndex(
            name: "IX_bookable_resource_tenant_id_branch_id_category_id_is_active_operational_status",
            schema: "resource",
            table: "bookable_resource",
            columns: new[] { "tenant_id", "branch_id", "category_id", "is_active", "operational_status" });

        migrationBuilder.CreateIndex(
            name: "IX_bookable_resource_tenant_id_branch_id_code",
            schema: "resource",
            table: "bookable_resource",
            columns: new[] { "tenant_id", "branch_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_bookable_resource_tenant_id_category_id",
            schema: "resource",
            table: "bookable_resource",
            columns: new[] { "tenant_id", "category_id" });

        migrationBuilder.CreateIndex(
            name: "IX_branch_tenant_id_code",
            schema: "org",
            table: "branch",
            columns: new[] { "tenant_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_branch_tenant_id_organization_id",
            schema: "org",
            table: "branch",
            columns: new[] { "tenant_id", "organization_id" });

        migrationBuilder.CreateIndex(
            name: "IX_branch_configuration_branch_id",
            schema: "org",
            table: "branch_configuration",
            column: "branch_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_branch_configuration_tenant_id_branch_id",
            schema: "org",
            table: "branch_configuration",
            columns: new[] { "tenant_id", "branch_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_branch_configuration_tenant_id_invoice_prefix",
            schema: "org",
            table: "branch_configuration",
            columns: new[] { "tenant_id", "invoice_prefix" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_branch_configuration_tenant_id_receipt_prefix",
            schema: "org",
            table: "branch_configuration",
            columns: new[] { "tenant_id", "receipt_prefix" },
            unique: true);

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

        migrationBuilder.CreateIndex(
            name: "IX_contact_point_tenant_id_normalized_value",
            schema: "stakeholder",
            table: "contact_point",
            columns: new[] { "tenant_id", "normalized_value" });

        migrationBuilder.CreateIndex(
            name: "IX_contact_point_tenant_id_stakeholder_id_type",
            schema: "stakeholder",
            table: "contact_point",
            columns: new[] { "tenant_id", "stakeholder_id", "type" },
            unique: true,
            filter: "[is_primary] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_contact_point_tenant_id_stakeholder_id_type_normalized_value",
            schema: "stakeholder",
            table: "contact_point",
            columns: new[] { "tenant_id", "stakeholder_id", "type", "normalized_value" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_corporate_tenant_id_registration_number",
            schema: "stakeholder",
            table: "corporate",
            columns: new[] { "tenant_id", "registration_number" });

        migrationBuilder.CreateIndex(
            name: "IX_corporate_tenant_id_stakeholder_id",
            schema: "stakeholder",
            table: "corporate",
            columns: new[] { "tenant_id", "stakeholder_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_document_reference_tenant_id_file_id",
            schema: "stakeholder",
            table: "document_reference",
            columns: new[] { "tenant_id", "file_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_document_reference_tenant_id_stakeholder_id_document_type_id_file_id",
            schema: "stakeholder",
            table: "document_reference",
            columns: new[] { "tenant_id", "stakeholder_id", "document_type_id", "file_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_hold_tenant_id_branch_id_status_expires_utc",
            schema: "scheduling",
            table: "hold",
            columns: new[] { "tenant_id", "branch_id", "status", "expires_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_hold_tenant_id_patient_id",
            schema: "scheduling",
            table: "hold",
            columns: new[] { "tenant_id", "patient_id" });

        migrationBuilder.CreateIndex(
            name: "IX_hold_tenant_id_request_id",
            schema: "scheduling",
            table: "hold",
            columns: new[] { "tenant_id", "request_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_hold_tenant_id_service_id",
            schema: "scheduling",
            table: "hold",
            columns: new[] { "tenant_id", "service_id" });

        migrationBuilder.CreateIndex(
            name: "IX_identifier_tenant_id_stakeholder_id_is_active",
            schema: "stakeholder",
            table: "identifier",
            columns: new[] { "tenant_id", "stakeholder_id", "is_active" });

        migrationBuilder.CreateIndex(
            name: "IX_identifier_tenant_id_type_issuer_normalized_value",
            schema: "stakeholder",
            table: "identifier",
            columns: new[] { "tenant_id", "type", "issuer", "normalized_value" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_organization_tenant_id_name",
            schema: "org",
            table: "organization",
            columns: new[] { "tenant_id", "name" });

        migrationBuilder.CreateIndex(
            name: "IX_outbox_message_lease_until_utc_status",
            schema: "worker",
            table: "outbox_message",
            columns: new[] { "lease_until_utc", "status" });

        migrationBuilder.CreateIndex(
            name: "IX_outbox_message_status_next_attempt_utc",
            schema: "worker",
            table: "outbox_message",
            columns: new[] { "status", "next_attempt_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_patient_tenant_id_patient_number",
            schema: "patient",
            table: "patient",
            columns: new[] { "tenant_id", "patient_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_patient_tenant_id_registration_branch_id_created_utc",
            schema: "patient",
            table: "patient",
            columns: new[] { "tenant_id", "registration_branch_id", "created_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_patient_tenant_id_registration_request_id",
            schema: "patient",
            table: "patient",
            columns: new[] { "tenant_id", "registration_request_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_patient_tenant_id_stakeholder_id",
            schema: "patient",
            table: "patient",
            columns: new[] { "tenant_id", "stakeholder_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_person_tenant_id_normalized_search_name_date_of_birth",
            schema: "stakeholder",
            table: "person",
            columns: new[] { "tenant_id", "normalized_search_name", "date_of_birth" });

        migrationBuilder.CreateIndex(
            name: "IX_person_tenant_id_stakeholder_id",
            schema: "stakeholder",
            table: "person",
            columns: new[] { "tenant_id", "stakeholder_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_resource_capability_tenant_id_resource_id_service_id",
            schema: "resource",
            table: "resource_capability",
            columns: new[] { "tenant_id", "resource_id", "service_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_resource_capability_tenant_id_service_id_is_active",
            schema: "resource",
            table: "resource_capability",
            columns: new[] { "tenant_id", "service_id", "is_active" });

        migrationBuilder.CreateIndex(
            name: "IX_resource_category_tenant_id_code",
            schema: "resource",
            table: "resource_category",
            columns: new[] { "tenant_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_resource_category_tenant_id_kind_is_active",
            schema: "resource",
            table: "resource_category",
            columns: new[] { "tenant_id", "kind", "is_active" });

        migrationBuilder.CreateIndex(
            name: "IX_resource_category_tenant_id_parent_category_id",
            schema: "resource",
            table: "resource_category",
            columns: new[] { "tenant_id", "parent_category_id" });

        migrationBuilder.CreateIndex(
            name: "IX_resource_reservation_tenant_id_branch_id_resource_id",
            schema: "scheduling",
            table: "resource_reservation",
            columns: new[] { "tenant_id", "branch_id", "resource_id" });

        migrationBuilder.CreateIndex(
            name: "IX_resource_reservation_tenant_id_hold_id_resource_id",
            schema: "scheduling",
            table: "resource_reservation",
            columns: new[] { "tenant_id", "hold_id", "resource_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_resource_reservation_tenant_id_resource_id_start_utc_end_utc",
            schema: "scheduling",
            table: "resource_reservation",
            columns: new[] { "tenant_id", "resource_id", "start_utc", "end_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_resource_status_event_tenant_id_branch_id_resource_id",
            schema: "resource",
            table: "resource_status_event",
            columns: new[] { "tenant_id", "branch_id", "resource_id" });

        migrationBuilder.CreateIndex(
            name: "IX_resource_status_event_tenant_id_resource_id_created_utc",
            schema: "resource",
            table: "resource_status_event",
            columns: new[] { "tenant_id", "resource_id", "created_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_service_tenant_id_code",
            schema: "catalog",
            table: "service",
            columns: new[] { "tenant_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_service_tenant_id_status_name",
            schema: "catalog",
            table: "service",
            columns: new[] { "tenant_id", "status", "name" });

        migrationBuilder.CreateIndex(
            name: "IX_service_resource_requirement_tenant_id_category_id",
            schema: "catalog",
            table: "service_resource_requirement",
            columns: new[] { "tenant_id", "category_id" });

        migrationBuilder.CreateIndex(
            name: "IX_service_resource_requirement_tenant_id_service_id_category_id_role_code",
            schema: "catalog",
            table: "service_resource_requirement",
            columns: new[] { "tenant_id", "service_id", "category_id", "role_code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_stakeholder_tenant_id_type_status_display_name",
            schema: "stakeholder",
            table: "stakeholder",
            columns: new[] { "tenant_id", "type", "status", "display_name" });

        migrationBuilder.CreateIndex(
            name: "IX_tenant_slug",
            schema: "platform",
            table: "tenant",
            column: "slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_tenant_application_slug_status",
            schema: "platform",
            table: "tenant_application",
            columns: new[] { "slug", "status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "address",
            schema: "stakeholder");

        migrationBuilder.DropTable(
            name: "audit_event",
            schema: "audit");

        migrationBuilder.DropTable(
            name: "availability_exception",
            schema: "scheduling");

        migrationBuilder.DropTable(
            name: "availability_rule",
            schema: "scheduling");

        migrationBuilder.DropTable(
            name: "branch_configuration",
            schema: "org");

        migrationBuilder.DropTable(
            name: "branch_permission_grant",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "contact_point",
            schema: "stakeholder");

        migrationBuilder.DropTable(
            name: "corporate",
            schema: "stakeholder");

        migrationBuilder.DropTable(
            name: "document_reference",
            schema: "stakeholder");

        migrationBuilder.DropTable(
            name: "identifier",
            schema: "stakeholder");

        migrationBuilder.DropTable(
            name: "outbox_message",
            schema: "worker");

        migrationBuilder.DropTable(
            name: "person",
            schema: "stakeholder");

        migrationBuilder.DropTable(
            name: "resource_capability",
            schema: "resource");

        migrationBuilder.DropTable(
            name: "resource_reservation",
            schema: "scheduling");

        migrationBuilder.DropTable(
            name: "resource_status_event",
            schema: "resource");

        migrationBuilder.DropTable(
            name: "service_resource_requirement",
            schema: "catalog");

        migrationBuilder.DropTable(
            name: "tenant_application",
            schema: "platform");

        migrationBuilder.DropTable(
            name: "branch_user",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "hold",
            schema: "scheduling");

        migrationBuilder.DropTable(
            name: "bookable_resource",
            schema: "resource");

        migrationBuilder.DropTable(
            name: "patient",
            schema: "patient");

        migrationBuilder.DropTable(
            name: "service",
            schema: "catalog");

        migrationBuilder.DropTable(
            name: "resource_category",
            schema: "resource");

        migrationBuilder.DropTable(
            name: "branch",
            schema: "org");

        migrationBuilder.DropTable(
            name: "stakeholder",
            schema: "stakeholder");

        migrationBuilder.DropTable(
            name: "organization",
            schema: "org");

        migrationBuilder.DropTable(
            name: "tenant",
            schema: "platform");
    }
}
