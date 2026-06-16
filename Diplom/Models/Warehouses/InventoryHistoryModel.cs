using Diplom.Models.Identity;

namespace Diplom.Models.Warehouses
{
    public class InventoryHistoryModel : IOwnedEntity
    {
        public int Id { get; set; }

        public string OwnerUserId { get; set; }

        public DateTime ChangeDate { get; set; }

        public string Name { get; set; }

        public string Unit { get; set; }

        public decimal QuantityChange { get; set; }

        public decimal Price { get; set; }

        public decimal Total => QuantityChange * Price;

        public decimal Balance { get; set; }

        public string Type { get; set; }

        public string Notes { get; set; }
    }
}
