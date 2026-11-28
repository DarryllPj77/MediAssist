using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediAssist.Web.Data.Migrations.Workflow
{
    /// <inheritdoc />
    public partial class AddRecordVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RunnerRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Authorizations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "RunnerRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Authorizations");
        }
    }
}
