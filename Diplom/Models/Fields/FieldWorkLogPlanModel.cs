using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Diplom.Models.Identity;

namespace Diplom.Models.Fields
{
    public class FieldWorkLogPlanModel : IOwnedEntity
    {
        public int Id { get; set; }

        public string OwnerUserId { get; set; }

        public int? OperationId { get; set; }

        public Diplom.Models.Tools.EncyclopediaItem? Operation { get; set; }

        public DateTime? PlanFrom { get; set; }

        public DateTime? PlanTo { get; set; }

        public int FieldId { get; set; }

        public FieldEntity? Field { get; set; }

        public double? AreaHectares { get; set; }

        public int? MechanicId { get; set; }

        public Diplom.Models.OperatorModel? Mechanic { get; set; }

        public double? FuelRate { get; set; }

        public double? FuelTotal { get; set; }

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public DateTime? ArchivedAt { get; set; }

        public List<FieldWorkLogPlanMaterialModel>? Materials { get; set; }

        public List<FieldWorkLogPlanAggregateModel>? Aggregates { get; set; }
    }

    public class FieldWorkLogPlanMaterialModel
    {
        public int Id { get; set; }

        public int FieldWorkLogPlanId { get; set; }

        public FieldWorkLogPlanModel? FieldWorkLogPlan { get; set; }

        public string Category { get; set; } = string.Empty; 

        public int? SeedTypeId { get; set; }

        public Diplom.Models.Tools.EncyclopediaItem? SeedType { get; set; }

        public int? MaterialId { get; set; }

        [NotMapped]
        public string? MaterialName { get; set; }

        public double? Rate { get; set; }

        public double? Total { get; set; }

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public DateTime? ArchivedAt { get; set; }
    }

    public class FieldWorkLogPlanAggregateModel
    {
        public int Id { get; set; }

        public int FieldWorkLogPlanId { get; set; }

        public FieldWorkLogPlanModel? FieldWorkLogPlan { get; set; }

        public Diplom.Models.EquipmentType? EquipmentType { get; set; }

        public int? EquipmentId { get; set; }

        public Diplom.Models.EquipmentModel? Equipment { get; set; }

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public DateTime? ArchivedAt { get; set; }
    }
}
