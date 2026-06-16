using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diplom.Models.Identity;
using Microsoft.AspNetCore.Http;

namespace Diplom.Models
{
    public class EquipmentModel : IOwnedEntity
    {
        public int Id { get; set; }

        public string OwnerUserId { get; set; }

        [Required]
        public string Name { get; set; }

        public EquipmentType Type { get; set; }

        public int? Year { get; set; }

        public double? WorkingHours { get; set; }

        public string? Operator { get; set; }

        public List<string> PhotoPaths { get; set; } = new();

        [NotMapped]
        public List<IFormFile>? PhotoUploads { get; set; }

        [NotMapped]
        public List<string>? PhotosToDelete { get; set; }
    }

    public enum EquipmentType
    {
        Tractor = 1,
        Combine = 2,
        Implement = 3
    }
}