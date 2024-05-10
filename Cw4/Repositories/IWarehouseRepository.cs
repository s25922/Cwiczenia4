using System.Data.SqlClient;
using Cw4.Model;

namespace Cw4.Repository
{
    public interface IWarehouseRepository
    {

        Task<IEnumerable<ProductWarehouse>> GetAllProductWarehousesAsync();
        Task<bool> AddProductToWarehouseAsync(int productId, int warehouseId, int amount, DateTime createdAt);
        
        Task<bool> AddProductToWarehouseUsingProcedureAsync(int productId, int warehouseId, int amount, DateTime createdAt);

    }
}