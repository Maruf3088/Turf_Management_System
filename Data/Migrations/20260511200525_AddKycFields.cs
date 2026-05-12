using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turf_management_system.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKycFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "TurfOwners");

            migrationBuilder.AddColumn<string>(
                name: "AdminComments",
                table: "TurfOwners",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalIdNumber",
                table: "TurfOwners",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NidBackImagePath",
                table: "TurfOwners",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NidFrontImagePath",
                table: "TurfOwners",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "TurfOwners",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradeLicenseImagePath",
                table: "TurfOwners",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtilityBillImagePath",
                table: "TurfOwners",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "TurfOwners",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminComments",
                table: "TurfOwners");

            migrationBuilder.DropColumn(
                name: "NationalIdNumber",
                table: "TurfOwners");

            migrationBuilder.DropColumn(
                name: "NidBackImagePath",
                table: "TurfOwners");

            migrationBuilder.DropColumn(
                name: "NidFrontImagePath",
                table: "TurfOwners");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "TurfOwners");

            migrationBuilder.DropColumn(
                name: "TradeLicenseImagePath",
                table: "TurfOwners");

            migrationBuilder.DropColumn(
                name: "UtilityBillImagePath",
                table: "TurfOwners");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "TurfOwners");

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "TurfOwners",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
