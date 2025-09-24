using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PitchSync.MatchService.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "MatchRooms",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipants_UserId",
                table: "RoomParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRooms_CreatedByUserId",
                table: "MatchRooms",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRooms_IsPublic_Status",
                table: "MatchRooms",
                columns: new[] { "IsPublic", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomParticipants_UserId",
                table: "RoomParticipants");

            migrationBuilder.DropIndex(
                name: "IX_MatchRooms_CreatedByUserId",
                table: "MatchRooms");

            migrationBuilder.DropIndex(
                name: "IX_MatchRooms_IsPublic_Status",
                table: "MatchRooms");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "MatchRooms",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
