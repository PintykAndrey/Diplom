using Diplom.Models.Identity;

namespace Diplom.Models.Warehouses
{
    public class MaterialLogModel : IOwnedEntity
    {
        public int Id { get; set; }

        public string OwnerUserId { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string Name { get; set; }   
        public string Unit { get; set; }  

        public double? Quantity { get; set; }
        public double? Price { get; set; }

        public double? Sum => Quantity * Price;

        public string Category { get; set; } 

        public int? TypeId { get; set; }

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public DateTime? ArchivedAt { get; set; }
    }
}
