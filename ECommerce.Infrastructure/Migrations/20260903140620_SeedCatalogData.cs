using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 3, "Electronic devices and accessories", "Electronics" },
                    { 4, "Clothing and fashion products", "Clothing" },
                    { 5, "Books and learning materials", "Books" },
                    { 6, "Products for home and kitchen", "Home & Kitchen" },
                    { 7, "Sports and fitness products", "Sports" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "Description", "IsActive", "Name", "Price" },
                values: new object[,]
                {
                    { 3, 3, "Bluetooth over-ear wireless headphones", true, "Wireless Headphones", 2499.00m },
                    { 4, 3, "Fitness tracking smart watch", true, "Smart Watch", 3999.00m },
                    { 5, 3, "RGB mechanical keyboard for computers", true, "Mechanical Keyboard", 2999.00m },
                    { 6, 3, "Ergonomic wireless mouse", true, "Wireless Mouse", 1299.00m },
                    { 7, 4, "Comfortable regular-fit cotton T-shirt", true, "Cotton T-Shirt", 799.00m },
                    { 8, 4, "Classic slim-fit denim jeans", true, "Slim Fit Jeans", 1799.00m },
                    { 9, 4, "Lightweight running shoes", true, "Running Shoes", 2499.00m },
                    { 10, 5, "A practical guide to writing clean software", true, "Clean Code", 699.00m },
                    { 11, 5, "Programming fundamentals and C# concepts", true, "C# Programming Guide", 899.00m },
                    { 12, 5, "Guide to building modern web applications", true, "ASP.NET Core Development", 1099.00m },
                    { 13, 6, "Automatic coffee maker for home", true, "Coffee Maker", 3499.00m },
                    { 14, 6, "Insulated stainless steel water bottle", true, "Stainless Steel Water Bottle", 899.00m },
                    { 15, 6, "Reusable containers for kitchen storage", true, "Kitchen Storage Set", 1299.00m },
                    { 16, 7, "Non-slip exercise and yoga mat", true, "Yoga Mat", 999.00m },
                    { 17, 7, "Durable backpack for gym and sports", true, "Gym Backpack", 1599.00m }
                });

            migrationBuilder.InsertData(
                table: "Inventories",
                columns: new[] { "Id", "ProductId", "Quantity", "ReorderLevel" },
                values: new object[,]
                {
                    { 3, 3, 25, 5 },
                    { 4, 4, 15, 5 },
                    { 5, 5, 3, 5 },
                    { 6, 6, 20, 5 },
                    { 7, 7, 30, 5 },
                    { 8, 8, 18, 5 },
                    { 9, 9, 12, 5 },
                    { 10, 10, 10, 3 },
                    { 11, 11, 8, 3 },
                    { 12, 12, 6, 3 },
                    { 13, 13, 7, 3 },
                    { 14, 14, 20, 5 },
                    { 15, 15, 9, 3 },
                    { 16, 16, 1, 5 },
                    { 17, 17, 14, 5 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
