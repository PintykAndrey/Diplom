using System;
using System.Collections.Generic;
using Diplom.Models.Identity;

namespace Diplom.Models
{
    public class EquipmentJournalModel : IOwnedEntity
    {
        public int Id { get; set; }

        public string OwnerUserId { get; set; }

        public int EquipmentId { get; set; }

        public string WorkType { get; set; } = string.Empty;

        public string Materials { get; set; } = string.Empty;

        public DateTime? Date { get; set; }

        public double? WorkingHours { get; set; }

        public DateTime LastModified { get; set; }

        public DateTime? ArchivedAt { get; set; }

        public EquipmentModel? Equipment { get; set; }

        public List<EquipmentJournalMaterialModel>? JournalMaterials { get; set; }

        public class EquipmentJournalMaterialModel
        {
            public int Id { get; set; }

            public int EquipmentJournalId { get; set; }

            public int? MaterialId { get; set; }

            public string? MaterialCategory { get; set; }

            public double? Quantity { get; set; }

            public EquipmentJournalModel? EquipmentJournal { get; set; }

            public DateTime LastModified { get; set; }

            public DateTime? ArchivedAt { get; set; }
        }
    }
}
