using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagIndexer.Models;
using RagIndexer.Options;

namespace RagIndexer.Services;

/// <summary>
/// Extracts rows from AdventureWorks tables and converts them into text documents.
/// </summary>
public class SqlExtractorService : ISqlExtractorService
{
    private readonly string _connectionString;
    private readonly int _maxRows;
    private readonly ILogger<SqlExtractorService> _logger;

    public SqlExtractorService(
        IOptions<AzureSqlOptions>  sqlOptions,
        IOptions<IndexerOptions>   indexerOptions,
        ILogger<SqlExtractorService> logger)
    {
        _connectionString = sqlOptions.Value.ConnectionString;
        _maxRows          = indexerOptions.Value.MaxRowsPerTable;
        _logger           = logger;
    }

    // ─────────────────────────────────────────────────────────────
    //  Public entry point — returns ALL documents across all tables
    // ─────────────────────────────────────────────────────────────

    public async IAsyncEnumerable<IndexDocument> ExtractAllAsync()
    {
        _logger.LogInformation("Extracting Products...");
        await foreach (var doc in ExtractProductsAsync())
            yield return doc;

        _logger.LogInformation("Extracting Customers...");
        await foreach (var doc in ExtractCustomersAsync())
            yield return doc;

        _logger.LogInformation("Extracting SalesOrderHeaders...");
        await foreach (var doc in ExtractSalesOrderHeadersAsync())
            yield return doc;

        _logger.LogInformation("Extracting SalesOrderDetails...");
        await foreach (var doc in ExtractSalesOrderDetailsAsync())
            yield return doc;
    }

    // ─────────────────────────────────────────────────────────────
    //  SalesLT.Product
    // ─────────────────────────────────────────────────────────────

    private async IAsyncEnumerable<IndexDocument> ExtractProductsAsync()
    {
        const string sql = @"
            SELECT TOP (@maxRows)
                p.ProductID,
                p.Name,
                p.ProductNumber,
                ISNULL(p.Color, 'N/A')          AS Color,
                p.ListPrice,
                p.StandardCost,
                ISNULL(cat.Name, 'N/A')         AS Subcategory,
                ISNULL(parentCat.Name, 'N/A')   AS Category
            FROM SalesLT.Product p
            LEFT JOIN SalesLT.ProductCategory cat
                ON p.ProductCategoryID = cat.ProductCategoryID
            LEFT JOIN SalesLT.ProductCategory parentCat
                ON cat.ParentProductCategoryID = parentCat.ProductCategoryID
            WHERE p.ListPrice > 0
            ORDER BY p.ProductID";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@maxRows", _maxRows);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id       = reader.GetInt32(0);
            var name     = reader.GetString(1);
            var number   = reader.GetString(2);
            var color    = reader.GetString(3);
            var price    = reader.GetDecimal(4);
            var cost     = reader.GetDecimal(5);
            var sub      = reader.GetString(6);
            var category = reader.GetString(7);

            var content =
                $"Product {id}: {name} ({number}) is a {color} product in the " +
                $"{category} > {sub} category. List price: ${price:F2}. Standard cost: ${cost:F2}.";

            yield return new IndexDocument
            {
                Id       = $"product-{id}",
                Content  = content,
                Source   = "Product",
                SourceId = id.ToString()
            };
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  SalesLT.Customer
    // ─────────────────────────────────────────────────────────────

    private async IAsyncEnumerable<IndexDocument> ExtractCustomersAsync()
    {
        const string sql = @"
            SELECT TOP (@maxRows)
                c.CustomerID,
                CONCAT(c.FirstName, ' ', c.LastName)    AS CustomerName,
                ISNULL(c.CompanyName, 'N/A')            AS CompanyName,
                ISNULL(c.EmailAddress, 'N/A')           AS EmailAddress
            FROM SalesLT.Customer c
            ORDER BY c.CustomerID";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@maxRows", _maxRows);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id          = reader.GetInt32(0);
            var name        = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
            var company     = reader.GetString(2);
            var email       = reader.GetString(3);

            var content =
                $"Customer {id}: {name}, company: {company}, email: {email}.";

            yield return new IndexDocument
            {
                Id       = $"customer-{id}",
                Content  = content,
                Source   = "Customer",
                SourceId = id.ToString()
            };
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  SalesLT.SalesOrderHeader
    // ─────────────────────────────────────────────────────────────

    private async IAsyncEnumerable<IndexDocument> ExtractSalesOrderHeadersAsync()
    {
        const string sql = @"
            SELECT TOP (@maxRows)
                soh.SalesOrderID,
                soh.OrderDate,
                soh.CustomerID,
                soh.TotalDue,
                soh.OnlineOrderFlag,
                ISNULL(soh.ShipMethod, 'Unknown')   AS ShipMethod
            FROM SalesLT.SalesOrderHeader soh
            ORDER BY soh.OrderDate DESC";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@maxRows", _maxRows);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id        = reader.GetInt32(0);
            var date      = reader.GetDateTime(1).ToString("yyyy-MM-dd");
            var custId    = reader.GetInt32(2);
            var total     = reader.GetDecimal(3);
            var isOnline   = reader.GetBoolean(4) ? "online" : "in-store";
            var shipMethod = reader.GetString(5);

            var content =
                $"Sales order {id} was placed on {date} by CustomerID {custId} " +
                $"via {isOnline}. Ship method: {shipMethod}. Total due: ${total:F2}.";

            yield return new IndexDocument
            {
                Id       = $"order-{id}",
                Content  = content,
                Source   = "SalesOrderHeader",
                SourceId = id.ToString()
            };
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  SalesLT.SalesOrderDetail
    // ─────────────────────────────────────────────────────────────

    private async IAsyncEnumerable<IndexDocument> ExtractSalesOrderDetailsAsync()
    {
        const string sql = @"
            SELECT TOP (@maxRows)
                sod.SalesOrderDetailID,
                sod.SalesOrderID,
                sod.OrderQty,
                sod.UnitPrice,
                sod.LineTotal,
                p.Name          AS ProductName,
                sod.ProductID
            FROM SalesLT.SalesOrderDetail sod
            JOIN SalesLT.Product p ON sod.ProductID = p.ProductID
            ORDER BY sod.SalesOrderID DESC";

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@maxRows", _maxRows);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var detailId    = reader.GetInt32(0);
            var orderId     = reader.GetInt32(1);
            var qty         = reader.GetInt16(2);
            var unitPrice   = reader.GetDecimal(3);
            var lineTotal   = reader.GetDecimal(4);
            var productName = reader.GetString(5);
            var productId   = reader.GetInt32(6);

            var content =
                $"Order {orderId} includes {qty} unit(s) of '{productName}' " +
                $"(ProductID {productId}) at ${unitPrice:F2} each. " +
                $"Line total: ${lineTotal:F2}.";

            yield return new IndexDocument
            {
                Id       = $"orderdetail-{detailId}",
                Content  = content,
                Source   = "SalesOrderDetail",
                SourceId = detailId.ToString()
            };
        }
    }
}
