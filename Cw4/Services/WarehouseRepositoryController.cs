using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Cw4.Model;
using Cw4.Repository;

namespace Cw4.Services
{
    [Route("api/warehouse")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseRepository _warehouseRepository;

        public WarehouseController(IWarehouseRepository warehouseRepository)
        {
            _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
        }

        [HttpGet("getAllProductWarehouses")]
        public async Task<IActionResult> GetAllProductWarehousesAsync()
        {
            try
            {
                var productWarehouses = await _warehouseRepository.GetAllProductWarehousesAsync();
                return Ok(productWarehouses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        
        [HttpPost("addProductUsingProcedure")]
        public async Task<IActionResult> AddProductToWarehouseUsingProcedure([FromBody] ProductWarehouseAddRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }

            try
            {
                var newIndex = await _warehouseRepository.AddProductToWarehouseUsingProcedureAsync(request.ProductId, request.WarehouseId, request.Amount, request.CreatedAt);
                if (newIndex >= 0)
                {
                    return Ok(new { Message = "Product successfully added to the warehouse using stored procedure.", RowIndex = newIndex });
                }
                return BadRequest("Failed to add product using the stored procedure.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // Metoda POST bez użycia procedury składowanej
        [HttpPost("addProductWithoutProcedure")]
        public async Task<IActionResult> AddProductToWarehouseWithoutProcedure([FromBody] ProductWarehouseAddRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }

            try
            {
                // Wywołanie repozytorium bez użycia procedury składowanej
                var newIndex = await _warehouseRepository.AddProductToWarehouseAsync(request.ProductId, request.WarehouseId, request.Amount, request.CreatedAt);
                if (newIndex >= 0)
                {
                    return Ok(new { Message = "Product successfully added to the warehouse using stored procedure.", RowIndex = newIndex });
                }
                return BadRequest("Failed to add product using the stored procedure.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
