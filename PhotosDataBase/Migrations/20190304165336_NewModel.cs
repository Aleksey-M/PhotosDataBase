using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace PhotosDataBase.Migrations
{
    public partial class NewModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "PhotoFiles",
                newName: "PhotoFileInfoId");

            migrationBuilder.AddColumn<int>(
                name: "ImportedDirectoryId",
                table: "PhotoFiles",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ImportedDirectory",
                columns: table => new
                {
                    ImportedDirectoryId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DirectoryPath = table.Column<string>(nullable: true),
                    ImportStart = table.Column<DateTime>(nullable: false),
                    ImportFinish = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedDirectory", x => x.ImportedDirectoryId);
                });

            migrationBuilder.CreateTable(
                name: "ImportExceptionInfo",
                columns: table => new
                {
                    ImportExceptionInfoId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    FileNameFull = table.Column<string>(nullable: true),
                    FileName = table.Column<string>(nullable: true),
                    ExceptionDateTime = table.Column<DateTime>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    StackTrace = table.Column<string>(nullable: true),
                    ImportedDirectoryId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportExceptionInfo", x => x.ImportExceptionInfoId);
                    table.ForeignKey(
                        name: "FK_ImportExceptionInfo_ImportedDirectory_ImportedDirectoryId",
                        column: x => x.ImportedDirectoryId,
                        principalTable: "ImportedDirectory",
                        principalColumn: "ImportedDirectoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoFiles_ImportedDirectoryId",
                table: "PhotoFiles",
                column: "ImportedDirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportExceptionInfo_ImportedDirectoryId",
                table: "ImportExceptionInfo",
                column: "ImportedDirectoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhotoFiles_ImportedDirectory_ImportedDirectoryId",
                table: "PhotoFiles",
                column: "ImportedDirectoryId",
                principalTable: "ImportedDirectory",
                principalColumn: "ImportedDirectoryId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhotoFiles_ImportedDirectory_ImportedDirectoryId",
                table: "PhotoFiles");

            migrationBuilder.DropTable(
                name: "ImportExceptionInfo");

            migrationBuilder.DropTable(
                name: "ImportedDirectory");

            migrationBuilder.DropIndex(
                name: "IX_PhotoFiles_ImportedDirectoryId",
                table: "PhotoFiles");

            migrationBuilder.DropColumn(
                name: "ImportedDirectoryId",
                table: "PhotoFiles");

            migrationBuilder.RenameColumn(
                name: "PhotoFileInfoId",
                table: "PhotoFiles",
                newName: "Id");
        }
    }
}
