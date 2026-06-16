using System;
using System.ComponentModel.DataAnnotations.Schema;
using Diplom.Models.Identity;

namespace Diplom.Models.Fields
{

    public class CropRotationLog : IOwnedEntity
    {
        public int Id { get; set; }

        public string OwnerUserId { get; set; }

        public int FieldId { get; set; }
        public int CropId { get; set; }
        public int Year { get; set; }
        public DateTime LastModified { get; set; }

        [ForeignKey("FieldId")]
        public FieldEntity Field { get; set; }

        [ForeignKey("CropId")]
        public Diplom.Models.Tools.EncyclopediaItem Crop { get; set; }

        public DateTime? ArchivedAt { get; set; }
    }
}