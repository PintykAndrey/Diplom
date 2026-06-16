using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models;
using Diplom.Models.Identity;
using Diplom.Models.Warehouses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Diplom.Controllers.Equipment
{
    public class EquipmentJournalController : BaseController
    {
        public EquipmentJournalController(ApplicationDbContext context) : base(context) { }

        [HttpGet]
        public IActionResult Index()
        {
            if (!CanViewSection(SharedDataSection.Equipment)) return Forbid();

            ViewBag.Equipments = _context.Equipments
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId)
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.SpareParts = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "SpareParts" && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();

            ViewBag.Lubricants = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Lubricants" && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();

            var model = _context.EquipmentJournals
                .Include(x => x.Equipment)
                .Include(x => x.JournalMaterials)
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();

            return View("~/Views/Equipment/EquipmentJournal.cshtml", model);
        }

        private bool HasSufficientStock(int? materialId, double required)
        {
            if (!materialId.HasValue || required <= 0)
                return true;

            var material = _context.MaterialLogs.FirstOrDefault(x => x.Id == materialId.Value && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);
            if (material == null)
                return false;

            var available = material.Quantity ?? 0;
            return available >= required;
        }

        private bool HasSufficientStockForUpdate(
            EquipmentJournalModel.EquipmentJournalMaterialModel existing,
            int? newMaterialId,
            double newQuantity)
        {
            if (!newMaterialId.HasValue || newQuantity <= 0)
                return true;

            var material = _context.MaterialLogs.FirstOrDefault(x => x.Id == newMaterialId.Value && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);
            if (material == null)
                return false;

            var stock = material.Quantity ?? 0;
            var oldQty = existing.Quantity ?? 0;

            if (existing.MaterialId == newMaterialId)
            {
                
                return (stock + oldQty) >= newQuantity;
            }

            
            return stock >= newQuantity;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(List<EquipmentJournalModel> models)
        {
            if (!CanEditSection(SharedDataSection.Equipment)) return ForbidSharedEdit();

            if (models == null || models.Count == 0)
                return RedirectToAction("Index");

            if (!HasSufficientEquipmentStock(models))
            {
                TempData["EquipmentStockError"] = "Not enough material in warehouse.";
                return RedirectToAction("Index");
            }

            for (int i = 0; i < models.Count; i++)
            {
                var posted = models[i];

                posted.EquipmentId = ParseInt(Request.Form[$"models[{i}].EquipmentId"]) ?? 0;
                posted.WorkType = Request.Form[$"models[{i}].WorkType"].FirstOrDefault() ?? string.Empty;
                posted.WorkingHours = ParseDouble(Request.Form[$"models[{i}].WorkingHours"]);
                posted.Date = ParseFormDate($"models[{i}].Date");

                if (posted.EquipmentId == 0)
                    continue;

                if (!_context.Equipments.Any(x => x.Id == posted.EquipmentId && x.OwnerUserId == EffectiveOwnerUserId))
                    continue;

                if (posted.Id == 0)
                {
                    posted.LastModified = DateTime.UtcNow;
                    posted.OwnerUserId = EffectiveOwnerUserId;
                    posted.JournalMaterials = new List<EquipmentJournalModel.EquipmentJournalMaterialModel>();

                    _context.EquipmentJournals.Add(posted);
                    _context.SaveChanges();

                    ApplyPostedMaterials(i, posted, existingMaterials: null);

                    posted.Materials = BuildMaterialsDisplay(posted.Id);

                    posted.LastModified = DateTime.UtcNow;
                    _context.Entry(posted).State = EntityState.Modified;
                }
                else
                {
                    var existing = _context.EquipmentJournals
                        .Include(x => x.JournalMaterials)
                        .FirstOrDefault(x => x.Id == posted.Id && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);

                    if (existing == null)
                        continue;

                    existing.EquipmentId = posted.EquipmentId;
                    existing.WorkType = posted.WorkType;
                    existing.WorkingHours = posted.WorkingHours;
                    existing.Date = posted.Date;

                    ApplyPostedMaterials(i, existing, existing.JournalMaterials);

                    existing.Materials = BuildMaterialsDisplay(existing.Id);

                    existing.LastModified = DateTime.UtcNow;
                    _context.Entry(existing).State = EntityState.Modified;
                    _context.SaveChanges();
                }
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        private bool HasSufficientEquipmentStock(List<EquipmentJournalModel> models)
        {
            var requiredByMaterial = new Dictionary<int, double>();
            var returnedByMaterial = new Dictionary<int, double>();

            for (int i = 0; i < models.Count; i++)
            {
                var journalId = ParseInt(Request.Form[$"models[{i}].Id"]) ?? 0;
                var existingRows = journalId > 0
                    ? _context.EquipmentJournalMaterials
                        .Where(x => x.EquipmentJournalId == journalId && x.ArchivedAt == null && x.EquipmentJournal != null && x.EquipmentJournal.OwnerUserId == EffectiveOwnerUserId)
                        .ToList()
                    : new List<EquipmentJournalModel.EquipmentJournalMaterialModel>();

                foreach (var existingRow in existingRows)
                {
                    if (existingRow.MaterialId.HasValue && existingRow.Quantity.HasValue)
                    {
                        returnedByMaterial[existingRow.MaterialId.Value] = returnedByMaterial.GetValueOrDefault(existingRow.MaterialId.Value) + existingRow.Quantity.Value;
                    }
                }

                foreach (var row in ReadPostedMaterialRows(i))
                {
                    if (!row.MaterialId.HasValue || !row.Quantity.HasValue || row.Quantity.Value <= 0)
                        continue;

                    requiredByMaterial[row.MaterialId.Value] = requiredByMaterial.GetValueOrDefault(row.MaterialId.Value) + row.Quantity.Value;
                }
            }

            foreach (var item in requiredByMaterial)
            {
                var available = _context.MaterialLogs
                    .Where(x => x.Id == item.Key && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                    .Select(x => x.Quantity ?? 0)
                    .FirstOrDefault();

                available += returnedByMaterial.GetValueOrDefault(item.Key);

                if (available < item.Value)
                    return false;
            }

            return true;
        }

        private void ApplyPostedMaterials(int modelIndex, EquipmentJournalModel journal,
            List<EquipmentJournalModel.EquipmentJournalMaterialModel>? existingMaterials)
        {
            existingMaterials ??= new List<EquipmentJournalModel.EquipmentJournalMaterialModel>();

            var postedRows = ReadPostedMaterialRows(modelIndex);

            var keepIds = postedRows
                .Where(x => x.RowId.HasValue && x.RowId.Value > 0)
                .Select(x => x.RowId!.Value)
                .ToHashSet();

            foreach (var toArchive in existingMaterials.Where(x => x.ArchivedAt == null && !keepIds.Contains(x.Id)).ToList())
            {
                ReturnMaterialToWarehouse(toArchive);
                toArchive.ArchivedAt = DateTime.UtcNow;
                toArchive.LastModified = DateTime.UtcNow;
                _context.Entry(toArchive).State = EntityState.Modified;
            }

            foreach (var row in postedRows)
            {
                if (!row.MaterialId.HasValue || row.MaterialId.Value == 0)
                    continue;

                if (row.Quantity.HasValue && row.Quantity.Value < 0)
                    continue;

                if (row.RowId.HasValue && row.RowId.Value > 0)
                {
                    var existing = existingMaterials.FirstOrDefault(x => x.Id == row.RowId.Value);
                    if (existing == null)
                        continue;

                    AdjustWarehouseForChange(existing, row.MaterialId, row.MaterialCategory, row.Quantity);

                    existing.MaterialId = row.MaterialId;
                    existing.MaterialCategory = row.MaterialCategory;
                    existing.Quantity = row.Quantity;
                    existing.LastModified = DateTime.UtcNow;
                    existing.ArchivedAt = null;

                    _context.Entry(existing).State = EntityState.Modified;
                }
                else
                {
                    var newRow = new EquipmentJournalModel.EquipmentJournalMaterialModel
                    {
                        EquipmentJournalId = journal.Id,
                        MaterialId = row.MaterialId,
                        MaterialCategory = row.MaterialCategory,
                        Quantity = row.Quantity,
                        LastModified = DateTime.UtcNow,
                        ArchivedAt = null
                    };

                    DeductMaterialFromWarehouse(newRow);

                    _context.EquipmentJournalMaterials.Add(newRow);
                }
            }
        }

        private void AdjustWarehouseForChange(
            EquipmentJournalModel.EquipmentJournalMaterialModel existing,
            int? newMaterialId,
            string? newCategory,
            double? newQuantity)
        {
            if (existing.ArchivedAt != null)
            {
                existing.MaterialId = newMaterialId;
                existing.MaterialCategory = newCategory;
                existing.Quantity = newQuantity;
                existing.LastModified = DateTime.UtcNow;
                existing.ArchivedAt = null;
                DeductMaterialFromWarehouse(existing);
                return;
            }

            if (existing.MaterialId != newMaterialId || existing.MaterialCategory != newCategory)
            {
                ReturnMaterialToWarehouse(existing);

                existing.MaterialId = newMaterialId;
                existing.MaterialCategory = newCategory;
                existing.Quantity = newQuantity;

                DeductMaterialFromWarehouse(existing);
                return;
            }

            var oldQty = existing.Quantity ?? 0;
            var newQty = newQuantity ?? 0;
            var diff = newQty - oldQty;

            if (diff > 0)
            {
                DeductMaterialFromWarehouse(existing, diff);
            }
            else if (diff < 0)
            {
                ReturnMaterialToWarehouse(existing, -diff);
            }
        }

        private void DeductMaterialFromWarehouse(EquipmentJournalModel.EquipmentJournalMaterialModel materialRow, double? quantityOverride = null)
        {
            if (!materialRow.MaterialId.HasValue)
                return;

            var qty = quantityOverride ?? materialRow.Quantity;
            if (!qty.HasValue || qty.Value <= 0)
                return;

            var material = _context.MaterialLogs.FirstOrDefault(x => x.Id == materialRow.MaterialId.Value && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);
            if (material == null)
                return;

            var available = material.Quantity ?? 0;
            if (available < qty.Value)
                return;

            material.Quantity = available - qty.Value;
            material.LastModified = DateTime.UtcNow;

            _context.InventoryHistoryModels.Add(new InventoryHistoryModel
            {
                ChangeDate = DateTime.UtcNow,
                OwnerUserId = EffectiveOwnerUserId,
                Name = material.Name,
                Type = materialRow.MaterialCategory ?? material.Category,
                Unit = material.Unit ?? string.Empty,
                QuantityChange = -(decimal)qty.Value,
                Price = (decimal)(material.Price ?? 0),
                Balance = (decimal)(material.Quantity ?? 0),
                Notes = BuildEquipmentJournalNotes(materialRow.EquipmentJournalId, isReturn: false)
            });
        }

        private void ReturnMaterialToWarehouse(EquipmentJournalModel.EquipmentJournalMaterialModel materialRow, double? quantityOverride = null)
        {
            if (!materialRow.MaterialId.HasValue)
                return;

            var qty = quantityOverride ?? materialRow.Quantity;
            if (!qty.HasValue || qty.Value <= 0)
                return;

            var material = _context.MaterialLogs.FirstOrDefault(x => x.Id == materialRow.MaterialId.Value && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);
            if (material == null)
                return;

            material.Quantity = (material.Quantity ?? 0) + qty.Value;
            material.LastModified = DateTime.UtcNow;

            _context.InventoryHistoryModels.Add(new InventoryHistoryModel
            {
                ChangeDate = DateTime.UtcNow,
                OwnerUserId = EffectiveOwnerUserId,
                Name = material.Name,
                Type = materialRow.MaterialCategory ?? material.Category,
                Unit = material.Unit ?? string.Empty,
                QuantityChange = (decimal)qty.Value,
                Price = (decimal)(material.Price ?? 0),
                Balance = (decimal)(material.Quantity ?? 0),
                Notes = BuildEquipmentJournalNotes(materialRow.EquipmentJournalId, isReturn: true)
            });
        }

        private string BuildEquipmentJournalNotes(int journalId, bool isReturn)
        {
            var journal = _context.EquipmentJournals
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == journalId && x.OwnerUserId == EffectiveOwnerUserId);

            var workType = journal?.WorkType ?? "";
            var equipmentId = journal != null ? journal.EquipmentId : 0;
            var equipmentName = _context.Equipments
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == equipmentId && x.OwnerUserId == EffectiveOwnerUserId)
                ?.Name ?? "";

            if (string.IsNullOrWhiteSpace(equipmentName))
                equipmentName = "Unknown equipment";

            var prefix = string.IsNullOrWhiteSpace(workType)
                ? "Equipment Journal"
                : $"Equipment Journal({workType})";

            var action = isReturn ? "Return for" : "Used for";
            return $"{prefix}: {action} {equipmentName}";
        }

        private string BuildMaterialsDisplay(int journalId)
        {
            var rows = _context.EquipmentJournalMaterials
                .Where(x => x.EquipmentJournalId == journalId && x.ArchivedAt == null)
                .ToList();

            if (rows.Count == 0)
                return string.Empty;

            var parts = new List<string>();
            foreach (var row in rows)
            {
                if (!row.MaterialId.HasValue || row.MaterialId.Value == 0)
                    continue;

                var mat = _context.MaterialLogs.FirstOrDefault(x => x.Id == row.MaterialId.Value && x.OwnerUserId == EffectiveOwnerUserId);
                var name = mat?.Name ?? row.MaterialId.Value.ToString();
                var unit = mat?.Unit ?? string.Empty;

                var qty = row.Quantity.HasValue ? row.Quantity.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                if (!string.IsNullOrEmpty(qty))
                    parts.Add($"{name} {qty} {unit}".Trim());
                else
                    parts.Add(name);
            }

            return string.Join(", ", parts);
        }

        private sealed class PostedMaterialRow
        {
            public int? RowId { get; set; }
            public int? MaterialId { get; set; }
            public string? MaterialCategory { get; set; }
            public double? Quantity { get; set; }
        }

        private List<PostedMaterialRow> ReadPostedMaterialRows(int modelIndex)
        {
            var rows = new List<PostedMaterialRow>();

            for (int m = 0; ; m++)
            {
                var rowIdRaw = Request.Form[$"models[{modelIndex}].MaterialRowIds_List[{m}]"];
                var materialIdRaw = Request.Form[$"models[{modelIndex}].MaterialIds_List[{m}]"];
                var categoryRaw = Request.Form[$"models[{modelIndex}].MaterialCategories_List[{m}]"];
                var qtyRaw = Request.Form[$"models[{modelIndex}].Quantities_List[{m}]"];

                if (string.IsNullOrEmpty(rowIdRaw) && string.IsNullOrEmpty(materialIdRaw) && string.IsNullOrEmpty(categoryRaw) && string.IsNullOrEmpty(qtyRaw))
                    break;

                rows.Add(new PostedMaterialRow
                {
                    RowId = ParseInt(rowIdRaw),
                    MaterialId = ParseInt(materialIdRaw),
                    MaterialCategory = categoryRaw.ToString(),
                    Quantity = ParseDouble(qtyRaw)
                });
            }

            return rows;
        }

        private double? ParseDouble(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            if (double.TryParse(input.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value))
                return value;

            return null;
        }

        private int? ParseInt(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            return int.TryParse(input, out var value) ? value : null;
        }

        private DateTime? ParseFormDate(string key)
        {
            var value = Request.Form[key];
            return DateTime.TryParse(value, out var dt) ? DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc) : null;
        }
    }
}
