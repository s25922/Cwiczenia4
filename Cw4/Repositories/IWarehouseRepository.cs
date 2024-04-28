using System.Data.SqlClient;

namespace Cw4.Warehouse
{
    public interface IWarehouseRepository
    {
        public Task<List<Warehouse>> GetWarehouse(string orderBy);

        
    }
}