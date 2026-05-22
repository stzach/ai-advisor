using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserAge",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserAge",
                table: "AspNetUsers");
        }
    }
}
