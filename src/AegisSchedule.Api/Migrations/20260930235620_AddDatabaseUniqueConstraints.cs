using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisSchedule.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_UniversityId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseOfferings_CourseId",
                table: "CourseOfferings");

            migrationBuilder.DropIndex(
                name: "IX_ActivityGroups_ActivityId",
                table: "ActivityGroups");

            migrationBuilder.CreateIndex(
                name: "IX_Terms_Semester_Year",
                table: "Terms",
                columns: new[] { "Semester", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_UniversityId_Code",
                table: "Courses",
                columns: new[] { "UniversityId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferings_CourseId_TermId",
                table: "CourseOfferings",
                columns: new[] { "CourseId", "TermId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityGroups_ActivityId_Name",
                table: "ActivityGroups",
                columns: new[] { "ActivityId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Terms_Semester_Year",
                table: "Terms");

            migrationBuilder.DropIndex(
                name: "IX_Courses_UniversityId_Code",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseOfferings_CourseId_TermId",
                table: "CourseOfferings");

            migrationBuilder.DropIndex(
                name: "IX_ActivityGroups_ActivityId_Name",
                table: "ActivityGroups");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_UniversityId",
                table: "Courses",
                column: "UniversityId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferings_CourseId",
                table: "CourseOfferings",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityGroups_ActivityId",
                table: "ActivityGroups",
                column: "ActivityId");
        }
    }
}
