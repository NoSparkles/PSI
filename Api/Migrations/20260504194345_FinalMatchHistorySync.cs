using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations;

 /// <inheritdoc />
 public partial class FinalMatchHistorySync : Migration
 {
     /// <inheritdoc />
     protected override void Up(MigrationBuilder migrationBuilder)
     {
         
     }

     /// <inheritdoc />
     protected override void Down(MigrationBuilder migrationBuilder)
     {
         migrationBuilder.DropTable(
             name: "Games");

         migrationBuilder.DropTable(
             name: "MatchHistories");

         migrationBuilder.DropTable(
             name: "Tournaments");

         migrationBuilder.DropTable(
             name: "UserRound");

         migrationBuilder.DropColumn(
             name: "Discriminator",
             table: "Users");

         migrationBuilder.EnsureSchema(
             name: "public1");

         migrationBuilder.RenameTable(
             name: "Users",
             newName: "Users",
             newSchema: "public1");

         migrationBuilder.AlterColumn<string>(
             name: "PasswordHash",
             schema: "public1",
             table: "Users",
             type: "text",
             nullable: false,
             defaultValue: "",
             oldClrType: typeof(string),
             oldType: "text",
             oldNullable: true);

         migrationBuilder.AddColumn<int>(
             name: "Wins",
             schema: "public1",
             table: "Users",
             type: "integer",
             nullable: false,
             defaultValue: 0);

         migrationBuilder.CreateTable(
             name: "GameStats",
             schema: "public1",
             columns: table => new
             {
                 UserId = table.Column<Guid>(type: "uuid", nullable: false),
                 ConnectFourGamesPlayed = table.Column<int>(type: "integer", nullable: false),
                 ConnectFourWins = table.Column<int>(type: "integer", nullable: false),
                 RockPaperScissorsGamesPlayed = table.Column<int>(type: "integer", nullable: false),
                 RockPaperScissorsWins = table.Column<int>(type: "integer", nullable: false),
                 TicTacToeGamesPlayed = table.Column<int>(type: "integer", nullable: false),
                 TicTacToeWins = table.Column<int>(type: "integer", nullable: false),
                 TotalGamesPlayed = table.Column<int>(type: "integer", nullable: false),
                 TotalWins = table.Column<int>(type: "integer", nullable: false)
             },
             constraints: table => table.PrimaryKey("PK_GameStats", x => x.UserId));
     }
 }
