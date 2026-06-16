using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models.Fields;
using Diplom.Models.Identity;
using Diplom.Models.Warehouses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers.Fields
{
    public class FieldsController : BaseController
    {
        public FieldsController(ApplicationDbContext context) : base(context) { }

        public IActionResult FieldWorkLog()
        {
            return RedirectToAction("FieldWorkLogPlan", new { tab = "fact" });
        }

        [HttpGet]
        public IActionResult FieldWorkLogPlan(string tab = "fact")
        {
            if (!CanViewSection(SharedDataSection.Fields)) return Forbid();

            ViewBag.SelectedTab = tab;
            ViewBag.Fields = _context.Fields
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.Operations = _context.EncyclopediaItems
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Operation")
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.SeedTypes = _context.EncyclopediaItems
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Seed")
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.PesticideTypes = _context.EncyclopediaItems
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Pesticide")
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.FertilizerTypes = _context.EncyclopediaItems
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fertilizer")
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.Seeds = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Seeds" && x.ArchivedAt == null)
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.Pesticides = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Pesticides" && x.ArchivedAt == null)
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.Fertilizers = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fertilizers" && x.ArchivedAt == null)
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.Fuel = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fuel" && x.ArchivedAt == null)
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.Operators = _context.Operators
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToList();

            ViewBag.Equipments = _context.Equipments
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId)
                .OrderBy(x => x.Name)
                .ToList();

            ViewBag.FieldWorkLogs = _context.FieldWorkLogs
                .Include(x => x.Field)
                .Include(x => x.Operation)
                .Include(x => x.Mechanic)
                .Include(x => x.Materials)
                .Include(x => x.Aggregates)
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .OrderBy(x => x.Id)
                .ToList();

            var model = _context.FieldWorkLogPlans
                .Include(x => x.Field)
                .Include(x => x.Operation)
                .Include(x => x.Mechanic)
                .Include(x => x.Materials)
                .Include(x => x.Aggregates)
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .OrderBy(x => x.Id)
                .ToList();

            return View("~/Views/Fields/FieldWorkLogPlan.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveFieldWorkLogPlan(List<FieldWorkLogPlanModel> models)
        {
            if (!CanEditSection(SharedDataSection.Fields)) return ForbidSharedEdit();

            if (models == null || models.Count == 0)
                return RedirectToAction("FieldWorkLogPlan");

            for (int i = 0; i < models.Count; i++)
            {
                var posted = models[i];

                posted.OperationId = ParseInt(Request.Form[$"models[{i}].OperationId"]);
                posted.FieldId = ParseInt(Request.Form[$"models[{i}].FieldId"]) ?? 0;
                posted.AreaHectares = ParseDouble(Request.Form[$"models[{i}].AreaHectares"]);
                posted.MechanicId = ParseInt(Request.Form[$"models[{i}].MechanicId"]);

                posted.PlanFrom = ParseFormDate(Request.Form[$"models[{i}].PlanFrom"]);
                posted.PlanTo = ParseFormDate(Request.Form[$"models[{i}].PlanTo"]);

                posted.FuelRate = ParseDouble(Request.Form[$"models[{i}].FuelRate"]);
                posted.FuelTotal = ParseDouble(Request.Form[$"models[{i}].FuelTotal"]);

                if (posted.FieldId == 0)
                    continue;

                if (posted.Id == 0)
                {
                    posted.LastModified = DateTime.UtcNow;
                    posted.OwnerUserId = EffectiveOwnerUserId;
                    _context.FieldWorkLogPlans.Add(posted);
                    _context.SaveChanges();

                    ApplyPostedMaterials(i, posted.Id);
                    ApplyPostedAggregates(i, posted.Id);

                    posted.LastModified = DateTime.UtcNow;
                    _context.Entry(posted).State = EntityState.Modified;
                }
                else
                {
                    var existing = _context.FieldWorkLogPlans
                        .FirstOrDefault(x => x.Id == posted.Id && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);

                    if (existing == null)
                        continue;

                    existing.OperationId = posted.OperationId;
                    existing.FieldId = posted.FieldId;
                    existing.AreaHectares = posted.AreaHectares;
                    existing.PlanFrom = posted.PlanFrom;
                    existing.PlanTo = posted.PlanTo;
                    existing.MechanicId = posted.MechanicId;
                    existing.FuelRate = posted.FuelRate;
                    existing.FuelTotal = posted.FuelTotal;

                    ApplyPostedMaterials(i, existing.Id);
                    ApplyPostedAggregates(i, existing.Id);

                    existing.LastModified = DateTime.UtcNow;
                    _context.Entry(existing).State = EntityState.Modified;
                }
            }

            _context.SaveChanges();
            return RedirectToAction("FieldWorkLogPlan");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveFieldWorkLog(List<FieldWorkLogModel> factModels)
        {
            if (!CanEditSection(SharedDataSection.Fields)) return ForbidSharedEdit();

            var formIndexes = Request.Form.Keys
                .Where(x => x.StartsWith("factModels[") && x.EndsWith("].Id"))
                .Select(x =>
                {
                    var start = x.IndexOf('[') + 1;
                    var end = x.IndexOf(']');
                    return int.TryParse(x.Substring(start, end - start), out var value) ? value : (int?)null;
                })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (formIndexes.Count == 0)
                return RedirectToAction("FieldWorkLogPlan", new { tab = "fact" });

            if (!HasSufficientFactStock(formIndexes))
            {
                TempData["FactStockError"] = "Not enough materials or fuel in warehouse.";
                return RedirectToAction("FieldWorkLogPlan", new { tab = "fact" });
            }

            foreach (var i in formIndexes)
            {
                var posted = new FieldWorkLogModel
                {
                    Id = ParseInt(Request.Form[$"factModels[{i}].Id"]) ?? 0
                };

                posted.OperationId = ParseInt(Request.Form[$"factModels[{i}].OperationId"]);
                posted.FieldId = ParseInt(Request.Form[$"factModels[{i}].FieldId"]) ?? 0;
                posted.AreaHectares = ParseDouble(Request.Form[$"factModels[{i}].AreaHectares"]);
                posted.MechanicId = ParseInt(Request.Form[$"factModels[{i}].MechanicId"]);

                posted.PlanFrom = ParseFormDate(Request.Form[$"factModels[{i}].PlanFrom"]);
                posted.PlanTo = ParseFormDate(Request.Form[$"factModels[{i}].PlanTo"]);

                posted.FuelRate = ParseDouble(Request.Form[$"factModels[{i}].FuelRate"]);
                posted.FuelTotal = ParseDouble(Request.Form[$"factModels[{i}].FuelTotal"]);

                if (posted.FieldId == 0)
                    continue;

                if (posted.Id == 0)
                {
                    posted.LastModified = DateTime.UtcNow;
                    posted.OwnerUserId = EffectiveOwnerUserId;
                    _context.FieldWorkLogs.Add(posted);
                    _context.SaveChanges();

                    ApplyPostedLogMaterials(i, posted.Id, "factModels");
                    ApplyPostedLogAggregates(i, posted.Id, "factModels");
                    DeductFactFuelFromWarehouse(posted);

                    posted.LastModified = DateTime.UtcNow;
                    _context.Entry(posted).State = EntityState.Modified;
                }
                else
                {
                    var existing = _context.FieldWorkLogs
                        .FirstOrDefault(x => x.Id == posted.Id && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);

                    if (existing == null)
                        continue;

                    existing.OperationId = posted.OperationId;
                    existing.FieldId = posted.FieldId;
                    existing.AreaHectares = posted.AreaHectares;
                    existing.PlanFrom = posted.PlanFrom;
                    existing.PlanTo = posted.PlanTo;
                    existing.MechanicId = posted.MechanicId;
                    existing.FuelRate = posted.FuelRate;
                    AdjustFactFuelWarehouse(existing, posted.FuelTotal);
                    existing.FuelTotal = posted.FuelTotal;

                    ApplyPostedLogMaterials(i, existing.Id, "factModels");
                    ApplyPostedLogAggregates(i, existing.Id, "factModels");

                    existing.LastModified = DateTime.UtcNow;
                    _context.Entry(existing).State = EntityState.Modified;
                }
            }

            _context.SaveChanges();
            return RedirectToAction("FieldWorkLogPlan", new { tab = "fact" });
        }

        private void ApplyPostedLogMaterials(int modelIndex, int logId, string prefix, bool returnExistingBeforeDeduct = false)
        {
            var existing = _context.FieldWorkLogMaterials
                .Where(x => x.FieldWorkLogId == logId)
                .ToList();

            var postedRows = ReadPostedMaterialRows(modelIndex, prefix);
            var keepIds = postedRows.Where(x => x.RowId > 0).Select(x => x.RowId).ToHashSet();

            if (returnExistingBeforeDeduct)
            {
                foreach (var existingRow in existing.Where(x => x.ArchivedAt == null).ToList())
                {
                    ReturnFactMaterialToWarehouse(existingRow);
                }
            }

            foreach (var toArchive in existing.Where(x => x.ArchivedAt == null && !keepIds.Contains(x.Id)).ToList())
            {
                if (!returnExistingBeforeDeduct)
                    ReturnFactMaterialToWarehouse(toArchive);
                toArchive.ArchivedAt = DateTime.UtcNow;
                toArchive.LastModified = DateTime.UtcNow;
                _context.Entry(toArchive).State = EntityState.Modified;
            }

            foreach (var row in postedRows)
            {
                if (string.IsNullOrWhiteSpace(row.Category))
                    continue;

                if (row.RowId > 0)
                {
                    var ex = existing.FirstOrDefault(x => x.Id == row.RowId);
                    if (ex == null)
                        continue;

                    if (returnExistingBeforeDeduct)
                        DeductFactMaterialFromWarehouse(row.MaterialId, row.Category, row.Total, logId);
                    else
                        AdjustFactMaterialWarehouse(ex, row.MaterialId, row.Category, row.Total, logId);

                    ex.Category = row.Category;
                    ex.SeedTypeId = row.SeedTypeId;
                    ex.MaterialId = row.MaterialId;
                    ex.Rate = row.Rate;
                    ex.Total = row.Total;
                    ex.ArchivedAt = null;
                    ex.LastModified = DateTime.UtcNow;
                    _context.Entry(ex).State = EntityState.Modified;
                }
                else
                {
                    _context.FieldWorkLogMaterials.Add(new FieldWorkLogMaterialModel
                    {
                        FieldWorkLogId = logId,
                        Category = row.Category,
                        SeedTypeId = row.SeedTypeId,
                        MaterialId = row.MaterialId,
                        Rate = row.Rate,
                        Total = row.Total,
                        LastModified = DateTime.UtcNow,
                        ArchivedAt = null
                    });
                    DeductFactMaterialFromWarehouse(row.MaterialId, row.Category, row.Total, logId);
                }
            }
        }

        private bool HasSufficientFactStock(List<int> formIndexes)
        {
            var requiredByMaterial = new Dictionary<int, double>();
            var returnedByMaterial = new Dictionary<int, double>();
            double requiredFuel = 0;
            double returnedFuel = 0;

            foreach (var i in formIndexes)
            {
                var logId = ParseInt(Request.Form[$"factModels[{i}].Id"]) ?? 0;
                var existingRows = logId > 0
                    ? _context.FieldWorkLogMaterials.Where(x => x.FieldWorkLogId == logId && x.ArchivedAt == null && x.FieldWorkLog != null && x.FieldWorkLog.OwnerUserId == EffectiveOwnerUserId).ToList()
                    : new List<FieldWorkLogMaterialModel>();

                foreach (var existingRowToReturn in existingRows)
                {
                    if (existingRowToReturn.MaterialId.HasValue && existingRowToReturn.Total.HasValue)
                    {
                        returnedByMaterial[existingRowToReturn.MaterialId.Value] = returnedByMaterial.GetValueOrDefault(existingRowToReturn.MaterialId.Value) + existingRowToReturn.Total.Value;
                    }
                }

                foreach (var row in ReadPostedMaterialRows(i, "factModels"))
                {
                    if (!row.MaterialId.HasValue || !row.Total.HasValue || row.Total.Value <= 0)
                        continue;

                    requiredByMaterial[row.MaterialId.Value] = requiredByMaterial.GetValueOrDefault(row.MaterialId.Value) + row.Total.Value;
                }

                var postedFuelTotal = ParseDouble(Request.Form[$"factModels[{i}].FuelTotal"]);
                if (postedFuelTotal.HasValue && postedFuelTotal.Value > 0)
                {
                    requiredFuel += postedFuelTotal.Value;
                }

                if (logId > 0)
                    returnedFuel += _context.FieldWorkLogs.AsNoTracking().FirstOrDefault(x => x.Id == logId && x.OwnerUserId == EffectiveOwnerUserId)?.FuelTotal ?? 0;
            }

            foreach (var item in requiredByMaterial)
            {
                var available = GetAvailableMaterialQuantity(item.Key);

                available += returnedByMaterial.GetValueOrDefault(item.Key);

                if (available < item.Value)
                    return false;
            }

            var availableFuel = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fuel" && x.ArchivedAt == null)
                .Sum(x => x.Quantity ?? 0) + returnedFuel;

            if (availableFuel < requiredFuel)
                return false;

            return true;
        }

        private void DeductFactMaterialFromWarehouse(int? materialId, string category, double? quantity, int logId)
        {
            if (!materialId.HasValue || !quantity.HasValue || quantity.Value <= 0)
                return;

            var sourceMaterial = _context.MaterialLogs.FirstOrDefault(x => x.Id == materialId.Value && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);
            if (sourceMaterial == null)
                return;

            var remaining = quantity.Value;
            var materials = GetMatchingWarehouseMaterials(sourceMaterial)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Id)
                .ToList();

            foreach (var material in materials)
            {
                if (remaining <= 0)
                    break;

                var available = material.Quantity ?? 0;
                if (available <= 0)
                    continue;

                var used = Math.Min(available, remaining);
                material.Quantity = available - used;
                material.LastModified = DateTime.UtcNow;
                remaining -= used;

                _context.InventoryHistoryModels.Add(new InventoryHistoryModel
                {
                    ChangeDate = DateTime.UtcNow,
                    OwnerUserId = EffectiveOwnerUserId,
                    Name = material.Name,
                    Type = category,
                    Unit = material.Unit ?? string.Empty,
                    QuantityChange = -(decimal)used,
                    Price = (decimal)(material.Price ?? 0),
                    Balance = (decimal)(material.Quantity ?? 0),
                    Notes = BuildFieldWorkLogInventoryNotes(logId)
                });
            }
        }

        private void ReturnFactMaterialToWarehouse(FieldWorkLogMaterialModel materialRow, double? quantityOverride = null)
        {
            if (!materialRow.MaterialId.HasValue)
                return;

            var qty = quantityOverride ?? materialRow.Total;
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
                Type = materialRow.Category,
                Unit = material.Unit ?? string.Empty,
                QuantityChange = (decimal)qty.Value,
                Price = (decimal)(material.Price ?? 0),
                Balance = (decimal)(material.Quantity ?? 0),
                Notes = BuildFieldWorkLogInventoryReversalNotes(materialRow.FieldWorkLogId)
            });
        }

        private IQueryable<MaterialLogModel> GetMatchingWarehouseMaterials(MaterialLogModel sourceMaterial)
        {
            return _context.MaterialLogs
                .Where(x => x.ArchivedAt == null
                    && x.OwnerUserId == EffectiveOwnerUserId
                    && x.Category == sourceMaterial.Category
                    && x.TypeId == sourceMaterial.TypeId
                    && x.Name == sourceMaterial.Name);
        }

        private double GetAvailableMaterialQuantity(int materialId)
        {
            var sourceMaterial = _context.MaterialLogs.FirstOrDefault(x => x.Id == materialId && x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null);
            if (sourceMaterial == null)
                return 0;

            return GetMatchingWarehouseMaterials(sourceMaterial)
                .Sum(x => x.Quantity ?? 0);
        }

        private void AdjustFactMaterialWarehouse(FieldWorkLogMaterialModel existing, int? newMaterialId, string newCategory, double? newTotal, int logId)
        {
            if (existing.MaterialId != newMaterialId)
            {
                ReturnFactMaterialToWarehouse(existing);
                DeductFactMaterialFromWarehouse(newMaterialId, newCategory, newTotal, logId);
                return;
            }

            var oldQty = existing.Total ?? 0;
            var newQty = newTotal ?? 0;
            var diff = newQty - oldQty;

            if (diff > 0)
            {
                DeductFactMaterialFromWarehouse(newMaterialId, newCategory, diff, logId);
            }
            else if (diff < 0)
            {
                ReturnFactMaterialToWarehouse(existing, -diff);
            }
        }

        private void DeductFactFuelFromWarehouse(FieldWorkLogModel log)
        {
            if (!log.FuelTotal.HasValue || log.FuelTotal.Value <= 0)
                return;

            var remaining = log.FuelTotal.Value;
            var fuels = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fuel" && x.ArchivedAt == null)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Id)
                .ToList();

            foreach (var fuel in fuels)
            {
                if (remaining <= 0)
                    break;

                var available = fuel.Quantity ?? 0;
                if (available <= 0)
                    continue;

                var used = Math.Min(available, remaining);
                fuel.Quantity = available - used;
                fuel.LastModified = DateTime.UtcNow;
                remaining -= used;

                _context.InventoryHistoryModels.Add(new InventoryHistoryModel
                {
                    ChangeDate = DateTime.UtcNow,
                    OwnerUserId = EffectiveOwnerUserId,
                    Name = fuel.Name,
                    Type = fuel.Category,
                    Unit = fuel.Unit ?? string.Empty,
                    QuantityChange = -(decimal)used,
                    Price = (decimal)(fuel.Price ?? 0),
                    Balance = (decimal)(fuel.Quantity ?? 0),
                    Notes = BuildFieldWorkLogInventoryNotes(log.Id)
                });
            }
        }

        private void ReturnFactFuelToWarehouse(double? quantity, int logId)
        {
            if (!quantity.HasValue || quantity.Value <= 0)
                return;

            var fuel = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fuel" && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .FirstOrDefault();

            if (fuel == null)
                return;

            fuel.Quantity = (fuel.Quantity ?? 0) + quantity.Value;
            fuel.LastModified = DateTime.UtcNow;

            _context.InventoryHistoryModels.Add(new InventoryHistoryModel
            {
                ChangeDate = DateTime.UtcNow,
                OwnerUserId = EffectiveOwnerUserId,
                Name = fuel.Name,
                Type = fuel.Category,
                Unit = fuel.Unit ?? string.Empty,
                QuantityChange = (decimal)quantity.Value,
                Price = (decimal)(fuel.Price ?? 0),
                Balance = (decimal)(fuel.Quantity ?? 0),
                Notes = BuildFieldWorkLogInventoryNotes(logId)
            });
        }

        private void DeductFactFuelFromWarehouse(double? quantity, int logId)
        {
            if (!quantity.HasValue || quantity.Value <= 0)
                return;

            var remaining = quantity.Value;
            var fuels = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fuel" && x.ArchivedAt == null)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Id)
                .ToList();

            foreach (var fuel in fuels)
            {
                if (remaining <= 0)
                    break;

                var available = fuel.Quantity ?? 0;
                if (available <= 0)
                    continue;

                var used = Math.Min(available, remaining);
                fuel.Quantity = available - used;
                fuel.LastModified = DateTime.UtcNow;
                remaining -= used;

                _context.InventoryHistoryModels.Add(new InventoryHistoryModel
                {
                    ChangeDate = DateTime.UtcNow,
                    OwnerUserId = EffectiveOwnerUserId,
                    Name = fuel.Name,
                    Type = fuel.Category,
                    Unit = fuel.Unit ?? string.Empty,
                    QuantityChange = -(decimal)used,
                    Price = (decimal)(fuel.Price ?? 0),
                    Balance = (decimal)(fuel.Quantity ?? 0),
                    Notes = BuildFieldWorkLogInventoryNotes(logId)
                });
            }
        }

        private string BuildFieldWorkLogInventoryNotes(int logId)
        {
            var log = _context.FieldWorkLogs
                .Include(x => x.Operation)
                .Include(x => x.Field)
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == logId && x.OwnerUserId == EffectiveOwnerUserId);

            var operationName = log?.Operation?.Name ?? "";
            var fieldName = log?.Field?.Name ?? "";

            return $"Field Work Log ({operationName}): Used for {fieldName}";
        }

        private string BuildFieldWorkLogInventoryReversalNotes(int logId)
        {
            var log = _context.FieldWorkLogs
                .Include(x => x.Field)
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == logId && x.OwnerUserId == EffectiveOwnerUserId);

            var fieldName = log?.Field?.Name ?? "";

            return $"Reversal for field {fieldName}";
        }

        private void AdjustFactFuelWarehouse(FieldWorkLogModel existing, double? newFuelTotal)
        {
            var oldQty = existing.FuelTotal ?? 0;
            var newQty = newFuelTotal ?? 0;
            var diff = newQty - oldQty;

            if (diff > 0)
            {
                DeductFactFuelFromWarehouse(diff, existing.Id);
            }
            else if (diff < 0)
            {
                ReturnFactFuelToWarehouse(-diff, existing.Id);
            }
        }

        private void ApplyPostedLogAggregates(int modelIndex, int logId, string prefix)
        {
            var existing = _context.FieldWorkLogAggregates
                .Where(x => x.FieldWorkLogId == logId)
                .ToList();

            var postedRows = ReadPostedAggregateRows(modelIndex, prefix);
            var keepIds = postedRows.Where(x => x.RowId > 0).Select(x => x.RowId).ToHashSet();

            foreach (var toArchive in existing.Where(x => x.ArchivedAt == null && !keepIds.Contains(x.Id)).ToList())
            {
                toArchive.ArchivedAt = DateTime.UtcNow;
                toArchive.LastModified = DateTime.UtcNow;
                _context.Entry(toArchive).State = EntityState.Modified;
            }

            foreach (var row in postedRows)
            {
                if (row.EquipmentId == null && row.EquipmentType == null)
                    continue;

                if (row.RowId > 0)
                {
                    var ex = existing.FirstOrDefault(x => x.Id == row.RowId);
                    if (ex == null)
                        continue;

                    ex.EquipmentType = row.EquipmentType;
                    ex.EquipmentId = row.EquipmentId;
                    ex.ArchivedAt = null;
                    ex.LastModified = DateTime.UtcNow;
                    _context.Entry(ex).State = EntityState.Modified;
                }
                else
                {
                    _context.FieldWorkLogAggregates.Add(new FieldWorkLogAggregateModel
                    {
                        FieldWorkLogId = logId,
                        EquipmentType = row.EquipmentType,
                        EquipmentId = row.EquipmentId,
                        LastModified = DateTime.UtcNow,
                        ArchivedAt = null
                    });
                }
            }
        }

        private void ApplyPostedMaterials(int modelIndex, int planId)
        {
            var existing = _context.FieldWorkLogPlanMaterials
                .Where(x => x.FieldWorkLogPlanId == planId)
                .ToList();

            var postedRows = ReadPostedMaterialRows(modelIndex, "models");
            var keepIds = postedRows.Where(x => x.RowId > 0).Select(x => x.RowId).ToHashSet();

            foreach (var toArchive in existing.Where(x => x.ArchivedAt == null && !keepIds.Contains(x.Id)).ToList())
            {
                toArchive.ArchivedAt = DateTime.UtcNow;
                toArchive.LastModified = DateTime.UtcNow;
                _context.Entry(toArchive).State = EntityState.Modified;
            }

            foreach (var row in postedRows)
            {
                if (string.IsNullOrWhiteSpace(row.Category))
                    continue;

                if (row.RowId > 0)
                {
                    var ex = existing.FirstOrDefault(x => x.Id == row.RowId);
                    if (ex == null)
                        continue;

                    ex.Category = row.Category;
                    ex.SeedTypeId = row.SeedTypeId;
                    ex.MaterialId = row.MaterialId;
                    ex.Rate = row.Rate;
                    ex.Total = row.Total;
                    ex.ArchivedAt = null;
                    ex.LastModified = DateTime.UtcNow;
                    _context.Entry(ex).State = EntityState.Modified;
                }
                else
                {
                    _context.FieldWorkLogPlanMaterials.Add(new FieldWorkLogPlanMaterialModel
                    {
                        FieldWorkLogPlanId = planId,
                        Category = row.Category,
                        SeedTypeId = row.SeedTypeId,
                        MaterialId = row.MaterialId,
                        Rate = row.Rate,
                        Total = row.Total,
                        LastModified = DateTime.UtcNow,
                        ArchivedAt = null
                    });
                }
            }
        }

        private void ApplyPostedAggregates(int modelIndex, int planId)
        {
            var existing = _context.FieldWorkLogPlanAggregates
                .Where(x => x.FieldWorkLogPlanId == planId)
                .ToList();

            var postedRows = ReadPostedAggregateRows(modelIndex, "models");
            var keepIds = postedRows.Where(x => x.RowId > 0).Select(x => x.RowId).ToHashSet();

            foreach (var toArchive in existing.Where(x => x.ArchivedAt == null && !keepIds.Contains(x.Id)).ToList())
            {
                toArchive.ArchivedAt = DateTime.UtcNow;
                toArchive.LastModified = DateTime.UtcNow;
                _context.Entry(toArchive).State = EntityState.Modified;
            }

            foreach (var row in postedRows)
            {
                if (row.EquipmentId == null && row.EquipmentType == null)
                    continue;

                if (row.RowId > 0)
                {
                    var ex = existing.FirstOrDefault(x => x.Id == row.RowId);
                    if (ex == null)
                        continue;

                    ex.EquipmentType = row.EquipmentType;
                    ex.EquipmentId = row.EquipmentId;
                    ex.ArchivedAt = null;
                    ex.LastModified = DateTime.UtcNow;
                    _context.Entry(ex).State = EntityState.Modified;
                }
                else
                {
                    _context.FieldWorkLogPlanAggregates.Add(new FieldWorkLogPlanAggregateModel
                    {
                        FieldWorkLogPlanId = planId,
                        EquipmentType = row.EquipmentType,
                        EquipmentId = row.EquipmentId,
                        LastModified = DateTime.UtcNow,
                        ArchivedAt = null
                    });
                }
            }
        }

        private sealed class PostedMaterialRow
        {
            public int RowId { get; set; }
            public string Category { get; set; } = string.Empty;
            public int? SeedTypeId { get; set; }
            public int? MaterialId { get; set; }
            public double? Rate { get; set; }
            public double? Total { get; set; }
        }

        private List<PostedMaterialRow> ReadPostedMaterialRows(int modelIndex, string prefix)
        {
            var rows = new List<PostedMaterialRow>();
            var indexes = Request.Form.Keys
                .Where(x => x.StartsWith($"{prefix}[{modelIndex}].") && x.Contains("_List["))
                .Select(x =>
                {
                    var start = x.LastIndexOf('[') + 1;
                    var end = x.LastIndexOf(']');
                    return start > 0 && end > start && int.TryParse(x.Substring(start, end - start), out var value)
                        ? value
                        : (int?)null;
                })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            foreach (var m in indexes)
            {
                var rowIdRaw = Request.Form[$"{prefix}[{modelIndex}].MaterialRowIds_List[{m}]"];
                var categoryRaw = Request.Form[$"{prefix}[{modelIndex}].MaterialCategories_List[{m}]"];
                var seedTypeRaw = Request.Form[$"{prefix}[{modelIndex}].SeedTypeIds_List[{m}]"];
                var materialIdRaw = Request.Form[$"{prefix}[{modelIndex}].MaterialIds_List[{m}]"];
                var rateRaw = Request.Form[$"{prefix}[{modelIndex}].Rates_List[{m}]"];
                var totalRaw = Request.Form[$"{prefix}[{modelIndex}].Totals_List[{m}]"];

                if (string.IsNullOrEmpty(rowIdRaw) && string.IsNullOrEmpty(categoryRaw) && string.IsNullOrEmpty(seedTypeRaw)
                    && string.IsNullOrEmpty(materialIdRaw) && string.IsNullOrEmpty(rateRaw) && string.IsNullOrEmpty(totalRaw))
                    continue;

                rows.Add(new PostedMaterialRow
                {
                    RowId = ParseInt(rowIdRaw) ?? 0,
                    Category = categoryRaw.ToString(),
                    SeedTypeId = ParseInt(seedTypeRaw),
                    MaterialId = ParseInt(materialIdRaw),
                    Rate = ParseDouble(rateRaw),
                    Total = ParseDouble(totalRaw)
                });
            }

            return rows;
        }

        private sealed class PostedAggregateRow
        {
            public int RowId { get; set; }
            public Diplom.Models.EquipmentType? EquipmentType { get; set; }
            public int? EquipmentId { get; set; }
        }

        private List<PostedAggregateRow> ReadPostedAggregateRows(int modelIndex, string prefix)
        {
            var rows = new List<PostedAggregateRow>();

            for (int a = 0; ; a++)
            {
                var rowIdRaw = Request.Form[$"{prefix}[{modelIndex}].AggregateRowIds_List[{a}]"];
                var typeRaw = Request.Form[$"{prefix}[{modelIndex}].AggregateEquipmentTypes_List[{a}]"];
                var equipmentIdRaw = Request.Form[$"{prefix}[{modelIndex}].AggregateEquipmentIds_List[{a}]"];

                if (string.IsNullOrEmpty(rowIdRaw) && string.IsNullOrEmpty(typeRaw) && string.IsNullOrEmpty(equipmentIdRaw))
                    break;

                var typeInt = ParseInt(typeRaw);
                Diplom.Models.EquipmentType? eqType = null;
                if (typeInt.HasValue)
                    eqType = (Diplom.Models.EquipmentType)typeInt.Value;

                rows.Add(new PostedAggregateRow
                {
                    RowId = ParseInt(rowIdRaw) ?? 0,
                    EquipmentType = eqType,
                    EquipmentId = ParseInt(equipmentIdRaw)
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

        private DateTime? ParseFormDate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            return DateTime.TryParse(input, out var dt) ? DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc) : null;
        }
    }
}
