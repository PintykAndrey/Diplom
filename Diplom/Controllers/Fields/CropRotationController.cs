using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models.Fields;
using Diplom.Models.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers.Fields
{
    public class CropRotationController : BaseController
    {
        public CropRotationController(ApplicationDbContext context) : base(context) { }

        [HttpGet]
        public IActionResult CropRotation()
        {
            if (!CanViewSection(SharedDataSection.Fields)) return Forbid();

            var fields = _context.Fields
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .OrderBy(x => x.Name)
                .ToList();

            var crops = _context.EncyclopediaItems
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Seed")
                .OrderBy(x => x.Name)
                .ToList();

            var currentYear = DateTime.Now.Year;
            var years = Enumerable.Range(currentYear - 5, 11).ToList();

            var rotationLogs = _context.CropRotationLogs
                .Include(x => x.Field)
                .Include(x => x.Crop)
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .ToList();

            var seedTypeIds = _context.FieldWorkLogMaterials
                .Where(x => x.ArchivedAt == null && x.Category == "Seeds" && x.SeedTypeId.HasValue && x.FieldWorkLog != null && x.FieldWorkLog.OwnerUserId == EffectiveOwnerUserId)
                .Select(x => x.SeedTypeId!.Value)
                .Distinct()
                .ToList();

            var seedTypes = _context.EncyclopediaItems
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && seedTypeIds.Contains(x.Id))
                .ToDictionary(x => x.Id, x => x.Name);

            var logsByFieldYear = _context.FieldWorkLogs
                .Include(x => x.Operation)
                .Include(x => x.Materials)
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null && x.FieldId > 0 && x.PlanFrom.HasValue)
                .ToList()
                .Where(plan => IsSowingOperation(plan.Operation?.Name))
                .SelectMany(plan => (plan.Materials ?? new List<FieldWorkLogMaterialModel>())
                    .Where(material => material.ArchivedAt == null && material.Category == "Seeds" && material.SeedTypeId.HasValue)
                    .Select(material => new
                    {
                        FieldId = plan.FieldId,
                        Year = plan.PlanFrom!.Value.Year,
                        GrainName = seedTypes.TryGetValue(material.SeedTypeId!.Value, out var name) ? name : string.Empty,
                        GrainRateFact = plan.AreaHectares
                    }))
                .Where(x => !string.IsNullOrWhiteSpace(x.GrainName))
                .ToList();

            ViewBag.RotationLogs = rotationLogs;
            ViewBag.Crops = crops;
            ViewBag.LogsByFieldYear = logsByFieldYear;
            ViewBag.CanEdit = CanEditSection(SharedDataSection.Fields);

            var model = new CropRotationViewModel
            {
                Fields = fields,
                Crops = crops,
                Years = years
            };

            return View("~/Views/Fields/CropRotation.cshtml", model);
        }

        private bool IsSowingOperation(string? operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                return false;

            var name = operationName.Trim().ToLowerInvariant();
            return name == "sowing" || name == "seeding";
        }

        [HttpPost]
        public IActionResult SaveCropRotation(int[] FieldIds, int[] Years, int[] Crops)
        {
            if (!CanEditSection(SharedDataSection.Fields)) return ForbidSharedEdit();

            if (FieldIds == null || Years == null || Crops == null)
                return RedirectToAction("CropRotation");

            for (int i = 0; i < Crops.Length; i++)
            {
                int fieldId = FieldIds[i];
                int year = Years[i];
                int cropId = Crops[i];

                if (cropId == 0) continue;

                if (!_context.Fields.Any(f => f.Id == fieldId && f.OwnerUserId == EffectiveOwnerUserId))
                    continue;

                var oldLogs = _context.CropRotationLogs
                                .Where(r => r.OwnerUserId == EffectiveOwnerUserId && r.FieldId == fieldId && r.Year == year)
                                .ToList();
                if (oldLogs.Any())
                {
                    _context.CropRotationLogs.RemoveRange(oldLogs);
                }

                _context.CropRotationLogs.Add(new CropRotationLog
                {
                    FieldId = fieldId,
                    CropId = cropId,
                    Year = year,
                    OwnerUserId = EffectiveOwnerUserId,
                    LastModified = DateTime.UtcNow
                });
            }

            _context.SaveChanges();
            return RedirectToAction("CropRotation");
        }
    }
}
