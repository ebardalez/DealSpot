using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealSpot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "price_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    original_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_price_snapshots_deal_id_recorded_at_utc",
                table: "price_snapshots",
                columns: new[] { "deal_id", "recorded_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_snapshots");
        }
    }
}
