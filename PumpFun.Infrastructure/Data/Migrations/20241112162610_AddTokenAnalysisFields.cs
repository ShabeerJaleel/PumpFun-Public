using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PumpFun.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenAnalysisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Analysis",
                table: "Tokens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AnalysisCompleted",
                table: "Tokens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnalysisTimestamp",
                table: "Tokens",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Analysis",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "AnalysisCompleted",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "AnalysisTimestamp",
                table: "Tokens");
        }
    }
}
