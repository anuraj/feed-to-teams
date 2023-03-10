using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedToTeams.Frontend.Data.Migrations
{
    public partial class LatestPublishedFeedTableAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublishedFeeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeedModelId = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LatestPublishedUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishedFeeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublishedFeeds_Feeds_FeedModelId",
                        column: x => x.FeedModelId,
                        principalTable: "Feeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublishedFeeds_FeedModelId",
                table: "PublishedFeeds",
                column: "FeedModelId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublishedFeeds");
        }
    }
}
