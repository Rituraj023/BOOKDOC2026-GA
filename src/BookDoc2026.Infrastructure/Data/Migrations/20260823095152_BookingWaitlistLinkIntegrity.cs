using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookDoc2026.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BookingWaitlistLinkIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_booking_booking_waitlist_tenant_id_waitlist_entry_id",
                schema: "scheduling",
                table: "booking",
                columns: new[] { "tenant_id", "waitlist_entry_id" },
                principalSchema: "scheduling",
                principalTable: "booking_waitlist",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_booking_waitlist_booking_tenant_id_promoted_booking_id",
                schema: "scheduling",
                table: "booking_waitlist",
                columns: new[] { "tenant_id", "promoted_booking_id" },
                principalSchema: "scheduling",
                principalTable: "booking",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_booking_booking_waitlist_tenant_id_waitlist_entry_id",
                schema: "scheduling",
                table: "booking");

            migrationBuilder.DropForeignKey(
                name: "FK_booking_waitlist_booking_tenant_id_promoted_booking_id",
                schema: "scheduling",
                table: "booking_waitlist");
        }
    }
}
