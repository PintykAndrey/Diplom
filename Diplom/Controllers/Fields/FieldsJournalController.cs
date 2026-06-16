using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models.Fields;
using Diplom.Models.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Diplom.Controllers.Fields
{
    public class FieldsJournalController : BaseController
    {
        public FieldsJournalController(ApplicationDbContext context) : base(context) { }

        [HttpGet]
        public IActionResult FieldsJournal(int? id)
        {
            if (!CanViewSection(SharedDataSection.Fields)) return Forbid();

            var fields = _context.Fields
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .OrderBy(x => x.Name)
                .ToList();

            var selectedField = id.HasValue
                ? fields.FirstOrDefault(x => x.Id == id.Value)
                : fields.FirstOrDefault();

            var workLogs = selectedField == null
                ? new List<FieldWorkLogPlanModel>()
                : _context.FieldWorkLogPlans
                    .Include(x => x.Operation)
                    .Include(x => x.Materials)
                    .Include(x => x.Aggregates)
                    .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.FieldId == selectedField.Id && x.ArchivedAt == null)
                    .OrderByDescending(x => x.LastModified)
                    .ToList();

            var model = new FieldDetailsViewModel
            {
                Fields = fields,
                SelectedField = selectedField ?? new FieldEntity(),
                WorkLogs = workLogs
            };

            return View("~/Views/Fields/FieldsJournal.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetFieldData(int id)
        {
            if (!CanViewSection(SharedDataSection.Fields)) return Forbid();

            var field = _context.Fields.FirstOrDefault(x => x.Id == id && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);
            if (field == null)
                return NotFound();

            var materialNames = _context.MaterialLogs
                .AsNoTracking()
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId)
                .ToDictionary(x => x.Id, x => x.Name);

            var equipmentNames = _context.Equipments
                .AsNoTracking()
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId)
                .ToDictionary(x => x.Id, x => x.Name);

            var workLogs = _context.FieldWorkLogs
                .Include(x => x.Operation)
                .Include(x => x.Materials)
                .Include(x => x.Aggregates)
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.FieldId == id && x.ArchivedAt == null)
                .OrderByDescending(x => x.PlanFrom ?? x.LastModified)
                .ToList()
                .Select(x => new
                {
                    operation = x.Operation?.Name,
                    factFrom = x.PlanFrom?.ToString("yyyy-MM-dd"),
                    factTo = x.PlanTo?.ToString("yyyy-MM-dd"),
                    materials = new
                    {
                        grain = string.Join("<br>", (x.Materials ?? new())
                            .Where(m => m.ArchivedAt == null && m.Category == "Seeds" && m.MaterialId.HasValue)
                            .Select(m => materialNames.GetValueOrDefault(m.MaterialId!.Value, ""))),
                        grainRate = string.Join("<br>", (x.Materials ?? new())
                            .Where(m => m.ArchivedAt == null && m.Category == "Seeds")
                            .Select(m => m.Rate?.ToString() ?? "")),
                        fertilizers = (x.Materials ?? new())
                            .Where(m => m.ArchivedAt == null && m.Category == "Fertilizers" && m.MaterialId.HasValue)
                            .Select(m => materialNames.GetValueOrDefault(m.MaterialId!.Value, ""))
                            .ToList(),
                        fertilizerRates = (x.Materials ?? new())
                            .Where(m => m.ArchivedAt == null && m.Category == "Fertilizers")
                            .Select(m => m.Rate?.ToString() ?? "")
                            .ToList(),
                        pesticides = (x.Materials ?? new())
                            .Where(m => m.ArchivedAt == null && m.Category == "Pesticides" && m.MaterialId.HasValue)
                            .Select(m => materialNames.GetValueOrDefault(m.MaterialId!.Value, ""))
                            .ToList(),
                        pesticideRates = (x.Materials ?? new())
                            .Where(m => m.ArchivedAt == null && m.Category == "Pesticides")
                            .Select(m => m.Rate?.ToString() ?? "")
                            .ToList()
                    },
                    aggregate = string.Join("<br>", (x.Aggregates ?? new())
                        .Where(a => a.ArchivedAt == null && a.EquipmentId.HasValue)
                        .Select(a => equipmentNames.GetValueOrDefault(a.EquipmentId!.Value, "")))
                })
                .ToList();

            var inspectionNotes = _context.FieldSituationLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.FieldId == id && x.ArchivedAt == null)
                .OrderByDescending(x => x.Date)
                .Select(x => new
                {
                    date = x.Date.HasValue ? x.Date.Value.ToString("yyyy-MM-dd") : "",
                    description = x.Description,
                    recommendations = x.Recommendations,
                    photos = x.PhotoPaths
                })
                .ToList();

            return Json(new
            {
                field = new
                {
                    field.Name,
                    field.AreaHectares,
                    field.PerimeterMeters
                },
                workLogs,
                inspectionNotes
            });
        }

        [HttpGet]
        public IActionResult GetFieldGeometry(int id)
        {
            if (!CanViewSection(SharedDataSection.Fields)) return Forbid();

            var geometry = _context.Fields
                .Where(x => x.Id == id && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .Select(x => x.Geometry)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(geometry))
                return Json(new { coordinates = (object?)null });

            return Content(geometry, "application/json");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveField(int id, string name, double areaHectares, double perimeterMeters)
        {
            if (!CanEditSection(SharedDataSection.Fields)) return ForbidSharedEdit();

            var field = _context.Fields.FirstOrDefault(x => x.Id == id && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);
            if (field == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest();

            field.Name = name.Trim();
            field.AreaHectares = areaHectares;
            field.PerimeterMeters = perimeterMeters;
            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteField(int id)
        {
            if (!CanEditSection(SharedDataSection.Fields)) return ForbidSharedEdit();

            var field = _context.Fields.FirstOrDefault(x => x.Id == id && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);
            if (field == null)
                return NotFound();

            field.ArchivedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return Ok();
        }
    }
}
