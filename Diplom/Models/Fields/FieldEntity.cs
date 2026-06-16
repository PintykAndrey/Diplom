using System.ComponentModel.DataAnnotations;
using Diplom.Models.Identity;

namespace Diplom.Models.Fields
{
    public class FieldEntity : IOwnedEntity
    {
        public int Id { get; set; }

        public string OwnerUserId { get; set; }

        [Required]
        public string Name { get; set; }

        public double AreaHectares { get; set; }

        public double PerimeterMeters { get; set; }

        public string Geometry { get; set; }

        public System.DateTime? ArchivedAt { get; set; }
    }
}