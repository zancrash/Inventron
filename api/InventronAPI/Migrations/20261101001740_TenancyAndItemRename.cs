using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventronAPI.Migrations
{
    /// <summary>
    /// Renames Products to Items in place (keeping existing rows), adds tenancy columns to Items and Suppliers,
    /// converts money to decimal, and moves Products.Quantity into opening-balance InventoryTransactions.
    /// Hand-written: the scaffolder generated DROP TABLE Products + CREATE TABLE Items, which loses data.
    /// </summary>
    public partial class TenancyAndItemRename : Migration
    {
        // Existing rows predate tenancy and are assigned to the development tenant (Tenancy:DevTenantId).
        private const string DevTenantId = "a4e5b80b-634d-4c3e-84b2-17b2f8f62191";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Products -> Items
            migrationBuilder.DropPrimaryKey(name: "PK_Products", table: "Products");
            migrationBuilder.RenameTable(name: "Products", newName: "Items");
            migrationBuilder.AddPrimaryKey(name: "PK_Items", table: "Items", column: "Id");

            migrationBuilder.RenameColumn(name: "SKU", table: "Items", newName: "Sku");
            migrationBuilder.RenameColumn(name: "Cost", table: "Items", newName: "StandardCost");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Items",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Items",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "StandardCost",
                table: "Items",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "SellingPrice",
                table: "Items",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            AddBackfilledColumn<Guid>(migrationBuilder, "Items", "TenantId", "uniqueidentifier", $"'{DevTenantId}'");
            AddBackfilledColumn<DateTime>(migrationBuilder, "Items", "CreatedAtUtc", "datetime2", "SYSUTCDATETIME()");
            AddBackfilledColumn<int>(migrationBuilder, "Items", "Type", "int", "4"); // E_ItemType.Purchased
            AddBackfilledColumn<string>(migrationBuilder, "Items", "UnitOfMeasure", "nvarchar(20)", "N'Each'", maxLength: 20);
            AddBackfilledColumn<bool>(migrationBuilder, "Items", "IsActive", "bit", "1");

            migrationBuilder.AddColumn<DateTime>(name: "UpdatedAtUtc", table: "Items", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<int>(name: "LeadTimeDays", table: "Items", type: "int", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "SafetyStock", table: "Items", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "MinOrderQty", table: "Items", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "LotSize", table: "Items", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<int>(name: "PreferredSupplierId", table: "Items", type: "int", nullable: true);
            migrationBuilder.AddColumn<byte[]>(name: "RowVersion", table: "Items", type: "rowversion", rowVersion: true, nullable: false);

            // Suppliers
            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Suppliers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Suppliers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "Suppliers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            AddBackfilledColumn<Guid>(migrationBuilder, "Suppliers", "TenantId", "uniqueidentifier", $"'{DevTenantId}'");
            AddBackfilledColumn<DateTime>(migrationBuilder, "Suppliers", "CreatedAtUtc", "datetime2", "SYSUTCDATETIME()");
            AddBackfilledColumn<bool>(migrationBuilder, "Suppliers", "IsActive", "bit", "1");

            migrationBuilder.AddColumn<DateTime>(name: "UpdatedAtUtc", table: "Suppliers", type: "datetime2", nullable: true);

            // Inventory ledger
            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Carry each item's stored quantity into the ledger as an opening Adjustment (E_InventoryTransactionType.Adjustment = 3).
            DeferredSql(migrationBuilder, """
                INSERT INTO [InventoryTransactions] ([ItemId], [TransactionType], [Quantity], [Reference], [Notes], [TenantId], [CreatedAtUtc])
                SELECT [Id], 3, [Quantity], N'Opening balance', N'Migrated from Products.Quantity', [TenantId], SYSUTCDATETIME()
                FROM [Items]
                WHERE [Quantity] IS NOT NULL AND [Quantity] <> 0;
                """);

            migrationBuilder.DropColumn(name: "Quantity", table: "Items");

            // Indexes and foreign keys
            migrationBuilder.CreateIndex(
                name: "IX_Items_TenantId_Sku",
                table: "Items",
                columns: new[] { "TenantId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_PreferredSupplierId",
                table: "Items",
                column: "PreferredSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId",
                table: "Suppliers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ItemId",
                table: "InventoryTransactions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_TenantId_ItemId",
                table: "InventoryTransactions",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Suppliers_PreferredSupplierId",
                table: "Items",
                column: "PreferredSupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Items_Suppliers_PreferredSupplierId", table: "Items");

            migrationBuilder.DropIndex(name: "IX_Items_TenantId_Sku", table: "Items");
            migrationBuilder.DropIndex(name: "IX_Items_PreferredSupplierId", table: "Items");
            migrationBuilder.DropIndex(name: "IX_Suppliers_TenantId", table: "Suppliers");

            // Fold the ledger back into a single quantity per item. Fractional quantities are truncated to int.
            migrationBuilder.AddColumn<int>(name: "Quantity", table: "Items", type: "int", nullable: true);

            DeferredSql(migrationBuilder, """
                UPDATE i SET [Quantity] = t.[OnHand]
                FROM [Items] i
                JOIN (SELECT [ItemId], CAST(SUM([Quantity]) AS int) AS [OnHand] FROM [InventoryTransactions] GROUP BY [ItemId]) t
                    ON t.[ItemId] = i.[Id];
                """);

            migrationBuilder.DropTable(name: "InventoryTransactions");

            // Suppliers
            migrationBuilder.DropColumn(name: "CreatedAtUtc", table: "Suppliers");
            migrationBuilder.DropColumn(name: "IsActive", table: "Suppliers");
            migrationBuilder.DropColumn(name: "TenantId", table: "Suppliers");
            migrationBuilder.DropColumn(name: "UpdatedAtUtc", table: "Suppliers");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            // Items -> Products
            migrationBuilder.DropColumn(name: "TenantId", table: "Items");
            migrationBuilder.DropColumn(name: "CreatedAtUtc", table: "Items");
            migrationBuilder.DropColumn(name: "UpdatedAtUtc", table: "Items");
            migrationBuilder.DropColumn(name: "Type", table: "Items");
            migrationBuilder.DropColumn(name: "UnitOfMeasure", table: "Items");
            migrationBuilder.DropColumn(name: "LeadTimeDays", table: "Items");
            migrationBuilder.DropColumn(name: "SafetyStock", table: "Items");
            migrationBuilder.DropColumn(name: "MinOrderQty", table: "Items");
            migrationBuilder.DropColumn(name: "LotSize", table: "Items");
            migrationBuilder.DropColumn(name: "PreferredSupplierId", table: "Items");
            migrationBuilder.DropColumn(name: "IsActive", table: "Items");
            migrationBuilder.DropColumn(name: "RowVersion", table: "Items");

            migrationBuilder.Sql("UPDATE [Items] SET [SellingPrice] = 0 WHERE [SellingPrice] IS NULL;");

            migrationBuilder.AlterColumn<double>(
                name: "SellingPrice",
                table: "Items",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "StandardCost",
                table: "Items",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.RenameColumn(name: "StandardCost", table: "Items", newName: "Cost");
            migrationBuilder.RenameColumn(name: "Sku", table: "Items", newName: "SKU");

            migrationBuilder.DropPrimaryKey(name: "PK_Items", table: "Items");
            migrationBuilder.RenameTable(name: "Items", newName: "Products");
            migrationBuilder.AddPrimaryKey(name: "PK_Products", table: "Products", column: "Id");
        }

        /// <summary>
        /// Adds a NOT NULL column to a table that already has rows: add it nullable, back-fill, then tighten.
        /// Avoids leaving a DEFAULT constraint behind (in particular, raw inserts must never default a TenantId).
        /// </summary>
        private static void AddBackfilledColumn<T>(MigrationBuilder migrationBuilder, string table, string name, string type, string valueSql, int? maxLength = null)
        {
            migrationBuilder.AddColumn<T>(name: name, table: table, type: type, maxLength: maxLength, nullable: true);

            DeferredSql(migrationBuilder, $"UPDATE [{table}] SET [{name}] = {valueSql};");

            migrationBuilder.AlterColumn<T>(
                name: name,
                table: table,
                type: type,
                maxLength: maxLength,
                nullable: false,
                oldClrType: typeof(T),
                oldType: type,
                oldMaxLength: maxLength,
                oldNullable: true);
        }

        /// <summary>
        /// Runs SQL that references a column added earlier in this migration. Generated scripts put the whole
        /// migration in one batch, and SQL Server compiles a batch before running it, so the statement is wrapped
        /// in EXEC to compile it only when it runs.
        /// </summary>
        private static void DeferredSql(MigrationBuilder migrationBuilder, string sql)
        {
            migrationBuilder.Sql($"EXEC(N'{sql.Replace("'", "''")}');");
        }
    }
}
