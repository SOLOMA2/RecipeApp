using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.Migrations
{
    /// <inheritdoc />
    public partial class AddLikesAndRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                table: "Recipes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Recipes",
                type: "float(3)",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
                table: "Recipes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LikesCount",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "RatingCount",
                table: "Recipes");
        }
    }
}
