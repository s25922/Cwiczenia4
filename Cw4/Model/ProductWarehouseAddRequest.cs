namespace Cw4.Model
{
    public class ProductWarehouseAddRequest
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}