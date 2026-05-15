using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAdvisor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransactionDirection",
                table: "UserTransactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionDirection",
                table: "UserTransactions");
        }
    }
}
