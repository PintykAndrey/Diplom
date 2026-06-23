using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models.Fields;
using Diplom.Models.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers.Tools
{
    public class ArchiveController : BaseController
    {
        public ArchiveController(ApplicationDbContext context)
            : base(context)
        {
        }

        [HttpGet]
        public IActionResult Archive()
        {
            if (!CanViewSection(SharedDataSection.Fields)) return Forbid();

            var archivedFields = _context.Fields
                .Where(f => f.OwnerUserId == EffectiveOwnerUserId && f.ArchivedAt != null)
                .OrderByDescending(f => f.ArchivedAt)
                .ToList();

            ViewBag.CanEdit = CanEditSection(SharedDataSection.Fields);
            return View("~/Views/Tools/Archive.cshtml", archivedFields);
        }

        [HttpGet]
        public async Task<IActionResult> GetArchivedFieldDetails(int id)
        {
            if (!CanViewSection(SharedDataSection.Fields)) return Forbid();

            var field = await _context.Fields.FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == EffectiveOwnerUserId);
            if (field == null || field.ArchivedAt == null) return NotFound();

            var planLogs = await _context.FieldWorkLogPlans
                .Where(w => w.OwnerUserId == EffectiveOwnerUserId && w.FieldId == id)
                .Include(w => w.Operation)
                .Include(w => w.Mechanic)
                .Include(w => w.Materials)
                .Include(w => w.Aggregates)!
                    .ThenInclude(a => a.Equipment)
                .ToListAsync();

            var factLogs = await _context.FieldWorkLogs
                .Where(w => w.OwnerUserId == EffectiveOwnerUserId && w.FieldId == id)
                .Include(w => w.Operation)
                .Include(w => w.Mechanic)
                .Include(w => w.Materials)
                .Include(w => w.Aggregates)!
                    .ThenInclude(a => a.Equipment)
                .ToListAsync();

            var inspectionNotes = await _context.FieldSituationLogs
                .Where(n => n.OwnerUserId == EffectiveOwnerUserId && n.FieldId == id)
                .ToListAsync();

            var cropRotations = await _context.CropRotationLogs
                .Where(c => c.OwnerUserId == EffectiveOwnerUserId && c.FieldId == id)
                .Include(c => c.Crop)
                .ToListAsync();

            var materialIds = planLogs
                .SelectMany(w => w.Materials ?? new List<FieldWorkLogPlanMaterialModel>())
                .Select(m => m.MaterialId)
                .Concat(factLogs.SelectMany(w => w.Materials ?? new List<FieldWorkLogMaterialModel>()).Select(m => m.MaterialId))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            var materialNames = await _context.MaterialLogs
                .Where(m => m.OwnerUserId == EffectiveOwnerUserId && materialIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.Name);

            var data = new
            {
                field = new
                {
                    id = field.Id,
                    name = field.Name,
                    areaHectares = field.AreaHectares,
                    perimeterMeters = field.PerimeterMeters,
                    archivedAt = field.ArchivedAt?.ToString("yyyy-MM-dd HH:mm")
                },
                workLogs = planLogs.Select(w => new
                {
                    type = "Plan",
                    operation = w.Operation?.Name,
                    dateFrom = w.PlanFrom?.ToString("yyyy-MM-dd"),
                    dateTo = w.PlanTo?.ToString("yyyy-MM-dd"),
                    mechanic = w.Mechanic != null ? (w.Mechanic.LastName + " " + w.Mechanic.FirstName + " " + w.Mechanic.Surname).Trim() : null,
                    areaHectares = w.AreaHectares,
                    fuelRate = w.FuelRate,
                    fuelTotal = w.FuelTotal,
                    materials = (w.Materials ?? new List<FieldWorkLogPlanMaterialModel>()).Select(m => new
                    {
                        category = m.Category,
                        name = m.MaterialId.HasValue && materialNames.TryGetValue(m.MaterialId.Value, out var materialName) ? materialName : null,
                        rate = m.Rate,
                        total = m.Total
                    }),
                    aggregates = (w.Aggregates ?? new List<FieldWorkLogPlanAggregateModel>()).Select(a => new
                    {
                        equipmentType = a.EquipmentType.ToString(),
                        equipment = a.Equipment?.Name
                    })
                }).Concat(factLogs.Select(w => new
                {
                    type = "Fact",
                    operation = w.Operation?.Name,
                    dateFrom = w.PlanFrom?.ToString("yyyy-MM-dd"),
                    dateTo = w.PlanTo?.ToString("yyyy-MM-dd"),
                    mechanic = w.Mechanic != null ? (w.Mechanic.LastName + " " + w.Mechanic.FirstName + " " + w.Mechanic.Surname).Trim() : null,
                    areaHectares = w.AreaHectares,
                    fuelRate = w.FuelRate,
                    fuelTotal = w.FuelTotal,
                    materials = (w.Materials ?? new List<FieldWorkLogMaterialModel>()).Select(m => new
                    {
                        category = m.Category,
                        name = m.MaterialId.HasValue && materialNames.TryGetValue(m.MaterialId.Value, out var materialName) ? materialName : null,
                        rate = m.Rate,
                        total = m.Total
                    }),
                    aggregates = (w.Aggregates ?? new List<FieldWorkLogAggregateModel>()).Select(a => new
                    {
                        equipmentType = a.EquipmentType.ToString(),
                        equipment = a.Equipment?.Name
                    })
                })).OrderBy(x => x.dateFrom),
                inspectionNotes = inspectionNotes.Select((n, index) => new
                {
                    date = n.Date?.ToString("yyyy-MM-dd"),
                    description = n.Description,
                    photos = n.PhotoPaths
                }),
                cropRotations = cropRotations.Select((c, index) => new
                {
                    year = c.Year,
                    crop = c.Crop?.Name
                })
            };

            return Json(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreField(int id)
        {
            if (!CanEditSection(SharedDataSection.Fields)) return ForbidSharedEdit();

            var field = await _context.Fields.FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == EffectiveOwnerUserId);
            if (field == null || field.ArchivedAt == null) return NotFound();

            field.ArchivedAt = null;

            var workLogs = _context.FieldWorkLogPlans.Where(w => w.OwnerUserId == EffectiveOwnerUserId && w.FieldId == id && w.ArchivedAt != null);
            foreach (var log in workLogs)
            {
                log.ArchivedAt = null;
            }

            var situationLogs = _context.FieldSituationLogs.Where(s => s.OwnerUserId == EffectiveOwnerUserId && s.FieldId == id && s.ArchivedAt != null);
            foreach (var log in situationLogs)
            {
                log.ArchivedAt = null;
            }

            var cropRotations = _context.CropRotationLogs.Where(c => c.OwnerUserId == EffectiveOwnerUserId && c.FieldId == id && c.ArchivedAt != null);
            foreach (var rotation in cropRotations)
            {
                rotation.ArchivedAt = null;
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentlyDeleteField(int id)
        {
            if (!CanEditSection(SharedDataSection.Fields)) return ForbidSharedEdit();

            var field = await _context.Fields.FirstOrDefaultAsync(f => f.Id == id && f.OwnerUserId == EffectiveOwnerUserId);
            if (field == null || field.ArchivedAt == null) return NotFound();

            var workLogs = _context.FieldWorkLogPlans.Where(w => w.OwnerUserId == EffectiveOwnerUserId && w.FieldId == id);
            _context.FieldWorkLogPlans.RemoveRange(workLogs);

            var situationLogs = _context.FieldSituationLogs.Where(s => s.OwnerUserId == EffectiveOwnerUserId && s.FieldId == id);
            _context.FieldSituationLogs.RemoveRange(situationLogs);

            var cropRotations = _context.CropRotationLogs.Where(c => c.OwnerUserId == EffectiveOwnerUserId && c.FieldId == id);
            _context.CropRotationLogs.RemoveRange(cropRotations);

            _context.Fields.Remove(field);

            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public IActionResult GetArchivedCount()
        {
            if (!CanViewSection(SharedDataSection.Fields)) return Forbid();

            var count = _context.Fields.Count(f => f.OwnerUserId == EffectiveOwnerUserId && f.ArchivedAt != null);
            return Json(new { count = count });
        }
    }
}
