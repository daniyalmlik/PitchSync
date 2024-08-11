using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PitchSync.MatchService.Migrations
{
    /// <inheritdoc />
    public partial class InitialMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HomeTeam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AwayTeam = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Competition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    KickoffTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HomeScore = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AwayScore = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    InviteCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatchEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostedByDisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Minute = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Team = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PlayerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SecondaryPlayerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchEvents_MatchRooms_MatchRoomId",
                        column: x => x.MatchRoomId,
                        principalTable: "MatchRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerLineups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Team = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShirtNumber = table.Column<int>(type: "int", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsStarting = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AddedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerLineups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerLineups_MatchRooms_MatchRoomId",
                        column: x => x.MatchRoomId,
                        principalTable: "MatchRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Team = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerRatings_MatchRooms_MatchRoomId",
                        column: x => x.MatchRoomId,
                        principalTable: "MatchRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomParticipants_MatchRooms_MatchRoomId",
                        column: x => x.MatchRoomId,
                        principalTable: "MatchRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_MatchRoomId_CreatedAt",
                table: "MatchEvents",
                columns: new[] { "MatchRoomId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_MatchRoomId_Minute",
                table: "MatchEvents",
                columns: new[] { "MatchRoomId", "Minute" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerLineups_MatchRoomId",
                table: "PlayerLineups",
                column: "MatchRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_MatchRoomId_PlayerName_Team_UserId",
                table: "PlayerRatings",
                columns: new[] { "MatchRoomId", "PlayerName", "Team", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipants_MatchRoomId_UserId",
                table: "RoomParticipants",
                columns: new[] { "MatchRoomId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchEvents");

            migrationBuilder.DropTable(
                name: "PlayerLineups");

            migrationBuilder.DropTable(
                name: "PlayerRatings");

            migrationBuilder.DropTable(
                name: "RoomParticipants");

            migrationBuilder.DropTable(
                name: "MatchRooms");
        }
    }
}
