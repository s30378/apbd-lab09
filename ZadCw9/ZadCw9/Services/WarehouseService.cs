using System.Data;
using Microsoft.Data.SqlClient;
using ZadCw9.Models.DTOs;

namespace ZadCw9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly string _connectionString;

    public WarehouseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<int> AddProductAsync(AddProductDto request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Ilość musi być większa niż 0.");

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand();

        await conn.OpenAsync();
        cmd.Connection = conn;
        await using var transaction = await conn.BeginTransactionAsync();
        cmd.Transaction = (SqlTransaction)transaction;
        try
        {
            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
            cmd.Parameters.AddWithValue("@IdProduct", request.IdProduct);

            if (await cmd.ExecuteScalarAsync() is null)
                throw new ArgumentException("Produkt nie istenieje.");

            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            cmd.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);

            if (await cmd.ExecuteScalarAsync() is null)
                throw new ArgumentException("Magazyn nie istenieje.");

            cmd.Parameters.Clear();
            cmd.CommandText = @"
                SELECT TOP 1 o.IdOrder, p.Price
                FROM [Order] o
                JOIN Product p ON o.IdProduct = p.IdProduct
                LEFT JOIN Product_Warehouse pw ON o.IdOrder = pw.IdOrder
                WHERE o.IdProduct = @IdProduct
                  AND o.Amount = @Amount
                  AND o.CreatedAt < @CreatedAt
                  AND pw.IdProductWarehouse IS NULL
                ORDER BY o.CreatedAt";

            cmd.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            cmd.Parameters.AddWithValue("@Amount", request.Amount);
            cmd.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

            int idOrder;
            decimal productPrice;
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                    throw new InvalidOperationException("Nie znaleziono zamówienia.");

                idOrder = reader.GetInt32(0);
                productPrice = reader.GetDecimal(1);
            }

            cmd.Parameters.Clear();
            cmd.CommandText = @"
                UPDATE [Order]
                SET FulfilledAt = @Now
                WHERE IdOrder = @IdOrder";

            cmd.Parameters.AddWithValue("@Now", DateTime.Now);
            cmd.Parameters.AddWithValue("@IdOrder", idOrder);
            await cmd.ExecuteNonQueryAsync();

            cmd.Parameters.Clear();
            cmd.CommandText = @"
                INSERT INTO Product_Warehouse
                    (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                VALUES
                    (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            cmd.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
            cmd.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            cmd.Parameters.AddWithValue("@IdOrder", idOrder);
            cmd.Parameters.AddWithValue("@Amount", request.Amount);
            cmd.Parameters.AddWithValue("@Price", productPrice * request.Amount);
            cmd.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

            var newId = (int)await cmd.ExecuteScalarAsync();
            await transaction.CommitAsync();
            return newId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> AddProductProcedureAsync(AddProductDto request)
    {
        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand("AddProductToWarehouse", conn);
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        cmd.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        cmd.Parameters.AddWithValue("@Amount", request.Amount);
        cmd.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        await conn.OpenAsync();

        try
        {
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Błąd procedury: " + ex.Message);
        }
    }
}