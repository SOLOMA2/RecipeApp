using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeManager.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Carbohydrates",
                table: "Recipes",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Fat",
                table: "Recipes",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Protein",
                table: "Recipes",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Carbohydrates",
                table: "Ingredients",
                type: "float(8)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Fat",
                table: "Ingredients",
                type: "float(8)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Protein",
                table: "Ingredients",
                type: "float(8)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Carbohydrates",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Fat",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Protein",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Carbohydrates",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "Fat",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "Protein",
                table: "Ingredients");
        }
    }
}
