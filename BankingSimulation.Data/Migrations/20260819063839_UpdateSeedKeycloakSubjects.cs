using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingSimulation.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedKeycloakSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1L,
                column: "KeycloakSubject",
                value: "335bf9c2-af59-45d0-a96a-9d67361b5ae8");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2L,
                column: "KeycloakSubject",
                value: "c0c8c433-231a-4464-adae-61517e8765db");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3L,
                column: "KeycloakSubject",
                value: "a1122118-1905-4180-a2f9-870d5c0477ae");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1L,
                column: "KeycloakSubject",
                value: "seed-admin-subject");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2L,
                column: "KeycloakSubject",
                value: "seed-staff-subject");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3L,
                column: "KeycloakSubject",
                value: "seed-customer-subject");
        }
    }
}
