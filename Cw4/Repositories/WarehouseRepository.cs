using System.Data.SqlClient;

namespace Cw4.Repository
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly string _connectionString;
        public WarehouseRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }


        public Task<List<Warehouse>> GetWarehouse(string orderBy)
        {

        }
    }
}
