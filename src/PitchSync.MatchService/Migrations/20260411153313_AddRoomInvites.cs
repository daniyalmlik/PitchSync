using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PitchSync.MatchService.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoomInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HomeTeam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AwayTeam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InvitedUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InvitedDisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InvitedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvitedByDisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomInvites_MatchRooms_MatchRoomId",
                        column: x => x.MatchRoomId,
                        principalTable: "MatchRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomInvites_InvitedUserId_Status",
                table: "RoomInvites",
                columns: new[] { "InvitedUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomInvites_MatchRoomId",
                table: "RoomInvites",
                column: "MatchRoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomInvites");
        }
    }
}
