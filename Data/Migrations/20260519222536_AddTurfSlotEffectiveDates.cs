using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turf_management_system.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTurfSlotEffectiveDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payment_TransactionId_Unique",
                table: "Payments");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveFromDate",
                table: "TurfSlots",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveToDate",
                table: "TurfSlots",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_TransactionId",
                table: "Payments",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payment_TransactionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "EffectiveFromDate",
                table: "TurfSlots");

            migrationBuilder.DropColumn(
                name: "EffectiveToDate",
                table: "TurfSlots");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_TransactionId_Unique",
                table: "Payments",
                column: "TransactionId",
                unique: true);
        }
    }
}
