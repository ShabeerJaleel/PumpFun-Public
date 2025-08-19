using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PumpFun.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameMarketCapToMarketCapSol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MarketCap",
                table: "Tokens",
                newName: "MarketCapSol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MarketCapSol",
                table: "Tokens",
                newName: "MarketCap");
        }
    }
}
