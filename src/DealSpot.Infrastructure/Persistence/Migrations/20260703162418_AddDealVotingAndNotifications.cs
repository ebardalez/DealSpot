using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealSpot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDealVotingAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "down_vote_count",
                table: "deals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "up_vote_count",
                table: "deals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "deal_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deal_votes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pending_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    matched_keyword = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_sent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pending_alerts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_preferences_user_id_tenant_id",
                table: "notification_preferences",
                columns: new[] { "user_id", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deal_votes_deal_id",
                table: "deal_votes",
                column: "deal_id");

            migrationBuilder.CreateIndex(
                name: "ix_deal_votes_deal_id_user_id",
                table: "deal_votes",
                columns: new[] { "deal_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pending_alerts_is_sent",
                table: "pending_alerts",
                column: "is_sent");

            migrationBuilder.CreateIndex(
                name: "ix_pending_alerts_user_id_is_sent",
                table: "pending_alerts",
                columns: new[] { "user_id", "is_sent" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deal_votes");

            migrationBuilder.DropTable(
                name: "pending_alerts");

            migrationBuilder.DropIndex(
                name: "ix_notification_preferences_user_id_tenant_id",
                table: "notification_preferences");

            migrationBuilder.DropColumn(
                name: "down_vote_count",
                table: "deals");

            migrationBuilder.DropColumn(
                name: "up_vote_count",
                table: "deals");
        }
    }
}
