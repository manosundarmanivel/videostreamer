using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingVideoAPI.Migrations
{
    /// <inheritdoc />
    public partial class Ini : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BlobUrl",
                table: "TrainingVideos",
                newName: "BlobFileName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BlobFileName",
                table: "TrainingVideos",
                newName: "BlobUrl");
        }
    }
}
