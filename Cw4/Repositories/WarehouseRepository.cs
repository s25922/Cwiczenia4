using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Cw4.Model;

namespace Cw4.Repository
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly string _connectionString;

        // Constructor to initialize the connection string
        public WarehouseRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }

        // Path: /your_project/repository/WarehouseRepository.cs
        public async Task<IEnumerable<ProductWarehouse>> GetAllProductWarehousesAsync()
        {
            var productWarehouses = new List<ProductWarehouse>();

            using var con = new SqlConnection(_connectionString);
            using var com = new SqlCommand("SELECT * FROM Product_Warehouse", con);

            try
            {
                await con.OpenAsync();
                using var reader = await com.ExecuteReaderAsync();
        
                while (await reader.ReadAsync())
                {
                    // Adjust the field mapping according to your ProductWarehouse model
                    var productWarehouse = new ProductWarehouse
                    {
                        IdProductWarehouse = reader.GetInt32(reader.GetOrdinal("IdProductWarehouse")),
                        IdWarehouse = reader.GetInt32(reader.GetOrdinal("IdWarehouse")),
                        IdProduct = reader.GetInt32(reader.GetOrdinal("IdProduct")),
                        IdOrder = reader.GetInt32(reader.GetOrdinal("IdOrder")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        Amount = reader.GetInt32(reader.GetOrdinal("Amount")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    };
                    productWarehouses.Add(productWarehouse);
                }
            }
            catch (Exception ex)
            {
                // Log the exception message (replace with your logging system)
                Console.WriteLine($"Error: {ex.Message}");
            }

            return productWarehouses;
        }

        
// Method to add a product to the warehouse using the stored procedure and return the new row index
        public async Task<int> AddProductToWarehouseUsingProcedureAsync(int productId, int warehouseId, int amount, DateTime createdAt)
        {
            using var con = new SqlConnection(_connectionString);
            using var com = new SqlCommand("AddProductToWarehouse", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Adding parameters to the stored procedure
            com.Parameters.AddWithValue("@IdProduct", productId);
            com.Parameters.AddWithValue("@IdWarehouse", warehouseId);
            com.Parameters.AddWithValue("@Amount", amount);
            com.Parameters.AddWithValue("@CreatedAt", createdAt);

            try
            {
                await con.OpenAsync();
                var result = await com.ExecuteScalarAsync(); // Execute and fetch the result
                if (result != null && int.TryParse(result.ToString(), out int newIndex))
                {
                    return newIndex; // Return the new row index
                }
                return -1; // Return -1 if no index was returned
            }
            catch (Exception)
            {
                return -1; // Return -1 upon error
            }
        }

        public async Task<int> AddProductToWarehouseAsync(int productId, int warehouseId, int amount,
            DateTime createdAt)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            using var tran = con.BeginTransaction();

            try
            {
                // Sprawdzenie, czy produkt istnieje
                var productExists = await CheckIfProductExists(con, tran, productId);
                if (!productExists)
                {
                    throw new InvalidOperationException("Product does not exist.");
                }

                // Sprawdzenie, czy magazyn istnieje
                var warehouseExists = await CheckIfWarehouseExists(con, tran, warehouseId);
                if (!warehouseExists)
                {
                    throw new InvalidOperationException("Warehouse does not exist.");
                }

                // Sprawdzenie, czy istnieje zamówienie i czy może być zrealizowane
                var orderId = await CheckIfOrderExistsAndCanBeFulfilled(con, tran, productId, amount, createdAt);
                if (orderId == null)
                {
                    throw new InvalidOperationException("No suitable order found or it's already fulfilled.");
                }

                // Aktualizacja zamówienia
                await UpdateOrderFulfilledAt(con, tran, orderId.Value);
                
                // Sprawdź cenę
                var productPrice = await GetProductPrice(con, tran, productId);
                if (productPrice == null)
                {
                    throw new InvalidOperationException("Product does not have price.");
                }
                float price = Convert.ToSingle(productPrice);
                
                // Dodanie rekordu do Product_Warehouse
                var result = await InsertProductWarehouse(con, tran, productId, orderId.Value, warehouseId, amount, price,
                    createdAt);

                tran.Commit();
                return result;
            }
            catch (Exception ex)
            {
                tran.Rollback();
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        private async Task<bool> CheckIfProductExists(SqlConnection con, SqlTransaction tran, int productId)
        {
            var cmd = new SqlCommand("SELECT COUNT(1) FROM Product WHERE IdProduct = @IdProduct", con, tran);
            cmd.Parameters.AddWithValue("@IdProduct", productId);
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        private async Task<bool> CheckIfWarehouseExists(SqlConnection con, SqlTransaction tran, int warehouseId)
        {
            var cmd = new SqlCommand("SELECT COUNT(1) FROM Warehouse WHERE IdWarehouse = @IdWarehouse", con, tran);
            cmd.Parameters.AddWithValue("@IdWarehouse", warehouseId);
            return (int)await cmd.ExecuteScalarAsync() > 0;
        }

        private async Task<int?> CheckIfOrderExistsAndCanBeFulfilled(SqlConnection con, SqlTransaction tran,
            int productId, int amount, DateTime createdAt)
        {
            var cmd = new SqlCommand(
                "SELECT IdOrder FROM [Order] WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt AND IdOrder NOT IN (SELECT IdOrder FROM Product_Warehouse)",
                con, tran);
            cmd.Parameters.AddWithValue("@IdProduct", productId);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
            var result = await cmd.ExecuteScalarAsync();
            return result != null ? (int?)result : null;
        }

        private async Task<bool> UpdateOrderFulfilledAt(SqlConnection con, SqlTransaction tran, int orderId)
        {
            var cmd = new SqlCommand("UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder", con, tran);
            cmd.Parameters.AddWithValue("@IdOrder", orderId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private async Task<int> InsertProductWarehouse(SqlConnection con, SqlTransaction tran, int productId,
            int orderId, int warehouseId, int amount, float price, DateTime createdAt)
        {
            var cmd = new SqlCommand(
                @"INSERT INTO Product_Warehouse (IdProduct, IdOrder, IdWarehouse, Amount, Price, CreatedAt) 
          VALUES (@IdProduct, @IdOrder, @IdWarehouse, @Amount, @Price, @CreatedAt);
          SELECT CAST(SCOPE_IDENTITY() as int);",
                con, tran);
            cmd.Parameters.AddWithValue("@IdProduct", productId);
            cmd.Parameters.AddWithValue("@IdOrder", orderId);
            cmd.Parameters.AddWithValue("@IdWarehouse", warehouseId);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@Price", price);
            cmd.Parameters.AddWithValue("@CreatedAt", createdAt);

            var result = await cmd.ExecuteScalarAsync();
            if (result != null && int.TryParse(result.ToString(), out int newId))
            {
                return newId; // Returns the new index
            }
            return -1; // Return -1 if there was an error
        }

        
        private async Task<float?> GetProductPrice(SqlConnection con, SqlTransaction tran, int productId)
        {
            var cmd = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @IdProduct", con, tran);
            cmd.Parameters.AddWithValue("@IdProduct", productId);

            var result = await cmd.ExecuteScalarAsync();
            if (result != DBNull.Value)
            {
                return Convert.ToSingle(result);
            }
            return null;
        }
    }
}