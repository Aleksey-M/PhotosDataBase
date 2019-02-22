using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotosDataBase.Migrations
{
    public partial class addedProperty_CameraModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CameraModel",
                table: "PhotoFiles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CameraModel",
                table: "PhotoFiles");
        }
    }
}
