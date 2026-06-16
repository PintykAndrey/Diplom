using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using Diplom.Models.Identity;

namespace Diplom.Models.Fields
{
    public class FieldSituationLogModel : IOwnedEntity
    {
        public int Id { get; set; }

        public string OwnerUserId { get; set; }

        public int FieldId { get; set; }

        public FieldEntity? Field { get; set; }

        public DateTime? Date { get; set; }

        public string? Description { get; set; }

        public List<string> PhotoPaths { get; set; } = new();

        [NotMapped]
        public List<IFormFile>? PhotoUploads { get; set; }

        public string? Recommendations { get; set; }

        public DateTime? LastModified { get; set; }

        [NotMapped]
        public List<string>? PhotosToDelete { get; set; }

        public DateTime? ArchivedAt { get; set; }
    }
}