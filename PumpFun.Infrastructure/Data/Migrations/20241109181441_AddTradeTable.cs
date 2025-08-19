using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PumpFun.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Signature = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TokenAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TraderPublicKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TxType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewTokenBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VTokensInBondingCurve = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VSolInBondingCurve = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarketCapSol = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Signature);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trades");
        }
    }
}
