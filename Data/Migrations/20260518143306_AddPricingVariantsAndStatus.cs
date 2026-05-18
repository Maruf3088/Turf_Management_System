using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turf_management_system.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingVariantsAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PricingVariant",
                table: "TurfSlots",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "EveningPricePerHour",
                table: "Turfs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MorningPricePerHour",
                table: "Turfs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricingVariant",
                table: "TurfSlots");

            migrationBuilder.DropColumn(
                name: "EveningPricePerHour",
                table: "Turfs");

            migrationBuilder.DropColumn(
                name: "MorningPricePerHour",
                table: "Turfs");
        }
    }
}
