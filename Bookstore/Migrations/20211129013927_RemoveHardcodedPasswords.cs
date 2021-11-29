using Microsoft.EntityFrameworkCore.Migrations;

namespace Bookstore.Migrations
{
    public partial class RemoveHardcodedPasswords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Admin", "PasswordHash", "PasswordSalt", "Username" },
                values: new object[] { 1L, true, "XJjYQeBxEbIPuv9qpD5B+X4CH9OnqPSr5tTJLR7CKZg=", "nI4y6QEqFGuxfQ1Gv0U1Ww==", "admin" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Admin", "PasswordHash", "PasswordSalt", "Username" },
                values: new object[] { 2L, false, "eUn0bepndUfU8tSaH3GEivTkS57UXa4jNaojzjK5TsA=", "zc04r0J5DF5gNcv3klxF4A==", "toast" });
        }
    }
}
