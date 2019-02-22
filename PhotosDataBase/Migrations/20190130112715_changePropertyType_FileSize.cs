using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotosDataBase.Migrations
{
    public partial class changePropertyType_FileSize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "FileSize",
                table: "PhotoFiles",
                nullable: false,
                oldClrType: typeof(int));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "FileSize",
                table: "PhotoFiles",
                nullable: false,
                oldClrType: typeof(long));
        }
    }
}
