using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "UserProducts",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "UserProducts");
        }
    }
}
