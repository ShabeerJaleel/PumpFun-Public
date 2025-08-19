using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PumpFun.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTokenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Token_AnalysisCompleted",
                table: "Tokens",
                column: "AnalysisCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_Token_CreatedAt",
                table: "Tokens",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Token_MarketCap",
                table: "Tokens",
                column: "MarketCap");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Token_AnalysisCompleted",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Token_CreatedAt",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Token_MarketCap",
                table: "Tokens");
        }
    }
}
