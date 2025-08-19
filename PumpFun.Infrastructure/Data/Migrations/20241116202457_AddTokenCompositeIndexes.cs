using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PumpFun.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Token_Analysis_CreatedAt",
                table: "Tokens",
                columns: new[] { "AnalysisCompleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Token_MarketCap_CreatedAt",
                table: "Tokens",
                columns: new[] { "MarketCapSol", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Token_Analysis_CreatedAt",
                table: "Tokens");

            migrationBuilder.DropIndex(
                name: "IX_Token_MarketCap_CreatedAt",
                table: "Tokens");
        }
    }
}
