using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MessagingDeliveryPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 5,
                column: "claim_value",
                value: "Messaging.Deliveries.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 6,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 7,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 8,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 9,
                column: "claim_value",
                value: "Patients.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 10,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 11,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 12,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 13,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 14,
                column: "claim_value",
                value: "Scheduling.Availability.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 15,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 16,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 17,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 18,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 19,
                column: "claim_value",
                value: "Stakeholders.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 20,
                column: "claim_value",
                value: "Tenants.Approve");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 21,
                column: "claim_value",
                value: "Tenants.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 22,
                column: "claim_value",
                value: "Users.BranchAdministrators.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 23,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Users.Manage", 1L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 24,
                column: "claim_value",
                value: "Branches.Configuration.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 25,
                column: "claim_value",
                value: "Branches.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 26,
                column: "claim_value",
                value: "Catalog.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 27,
                column: "claim_value",
                value: "Catalog.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 28,
                column: "claim_value",
                value: "Messaging.Deliveries.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 29,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 30,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 31,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 32,
                column: "claim_value",
                value: "Patients.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 33,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 34,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 35,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 36,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 37,
                column: "claim_value",
                value: "Scheduling.Availability.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 38,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 39,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 40,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_claim",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 41, "bookdoc:permission", "Stakeholders.Manage", 2L },
                    { 42, "bookdoc:permission", "Stakeholders.View", 2L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 42);

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 5,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 6,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 7,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 8,
                column: "claim_value",
                value: "Patients.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 9,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 10,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 11,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 12,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 13,
                column: "claim_value",
                value: "Scheduling.Availability.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 14,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 15,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 16,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 17,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 18,
                column: "claim_value",
                value: "Stakeholders.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 19,
                column: "claim_value",
                value: "Tenants.Approve");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 20,
                column: "claim_value",
                value: "Tenants.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 21,
                column: "claim_value",
                value: "Users.BranchAdministrators.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 22,
                column: "claim_value",
                value: "Users.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 23,
                columns: new[] { "claim_value", "role_id" },
                values: new object[] { "Branches.Configuration.Manage", 2L });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 24,
                column: "claim_value",
                value: "Branches.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 25,
                column: "claim_value",
                value: "Catalog.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 26,
                column: "claim_value",
                value: "Catalog.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 27,
                column: "claim_value",
                value: "Patients.Register");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 28,
                column: "claim_value",
                value: "Patients.Search");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 29,
                column: "claim_value",
                value: "Patients.Update");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 30,
                column: "claim_value",
                value: "Patients.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 31,
                column: "claim_value",
                value: "Resources.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 32,
                column: "claim_value",
                value: "Resources.Status.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 33,
                column: "claim_value",
                value: "Resources.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 34,
                column: "claim_value",
                value: "Scheduling.Availability.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 35,
                column: "claim_value",
                value: "Scheduling.Availability.View");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 36,
                column: "claim_value",
                value: "Scheduling.Holds.Create");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 37,
                column: "claim_value",
                value: "Scheduling.Holds.Release");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 38,
                column: "claim_value",
                value: "Stakeholders.Documents.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 39,
                column: "claim_value",
                value: "Stakeholders.Manage");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "role_claim",
                keyColumn: "id",
                keyValue: 40,
                column: "claim_value",
                value: "Stakeholders.View");
        }
    }
}
