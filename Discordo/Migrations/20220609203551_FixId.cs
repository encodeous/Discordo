using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Discordo.Migrations
{
    public partial class FixId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeAdded",
                table: "Activities",
                newName: "TimeKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeKey",
                table: "Activities",
                newName: "TimeAdded");
        }
    }
}
