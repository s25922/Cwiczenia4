using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Cw3.Animal;


namespace Cw3.Animal
{
    [Route("api/animals")]
    [ApiController]
    public class WarehouseRepositoryController : ControllerBase
    {
        private readonly IWarehouseRepository _animalRepository;

        public WarehouseRepositoryController(IWarehouseRepository warehouseRepository)
        {
            _animalRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAnimals(string orderBy = "name")
        {
            var animals = await _animalRepository.GetAnimals(orderBy);
            return Ok(animals);
        }

    }
}
