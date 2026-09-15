using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WordDuel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseBodyJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BoardSeed = table.Column<int>(type: "integer", nullable: false),
                    BoardStateFlat = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: false),
                    TilesDrawnCount = table.Column<int>(type: "integer", nullable: false),
                    CurrentTurnSeat = table.Column<int>(type: "integer", nullable: false),
                    ConsecutivePasses = table.Column<int>(type: "integer", nullable: false),
                    EndReason = table.Column<int>(type: "integer", nullable: false),
                    WinnerSeat = table.Column<int>(type: "integer", nullable: true),
                    MoveSequenceCounter = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "moves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Seat = table.Column<int>(type: "integer", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    StartRow = table.Column<int>(type: "integer", nullable: true),
                    StartCol = table.Column<int>(type: "integer", nullable: true),
                    Direction = table.Column<int>(type: "integer", nullable: true),
                    TilesSubmitted = table.Column<string>(type: "text", nullable: true),
                    WordsFormedJson = table.Column<string>(type: "text", nullable: true),
                    PointsScored = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_moves_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Seat = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RackFlat = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    HasResigned = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_players_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_records_MatchId_PlayerId_Endpoint_IdempotencyKey",
                table: "idempotency_records",
                columns: new[] { "MatchId", "PlayerId", "Endpoint", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_moves_MatchId_SequenceNumber",
                table: "moves",
                columns: new[] { "MatchId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_MatchId_Seat",
                table: "players",
                columns: new[] { "MatchId", "Seat" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropTable(
                name: "moves");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "matches");
        }
    }
}
