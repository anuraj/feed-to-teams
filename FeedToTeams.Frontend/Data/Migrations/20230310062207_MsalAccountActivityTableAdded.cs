using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedToTeams.Frontend.Data.Migrations
{
    public partial class MsalAccountActivityTableAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MsalAccountActivities",
                columns: table => new
                {
                    AccountCacheKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccountIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountObjectId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountTenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserPrincipalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastActivity = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FailedToAcquireToken = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MsalAccountActivities", x => x.AccountCacheKey);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MsalAccountActivities");
        }
    }
}
