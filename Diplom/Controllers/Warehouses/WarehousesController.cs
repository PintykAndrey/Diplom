using Microsoft.AspNetCore.Mvc;
using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models.Warehouses;
using Diplom.Models.Identity;

namespace Diplom.Controllers.Warehouses
{
    public class WarehousesController : BaseController
    {
        public WarehousesController(ApplicationDbContext context) : base(context) 
        {
            _inventoryHistoryController = new InventoryHistoryController(context);
        }

        private readonly InventoryHistoryController _inventoryHistoryController;

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Pesticides()
        {
            if (!CanViewSection(SharedDataSection.Warehouses)) return Forbid();

            var pesticides = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Pesticides" && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();

            ViewBag.PesticideTypes = _context.EncyclopediaItems
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Pesticide")
                .OrderBy(x => x.Name)
                .ToList();

            return View("~/Views/Warehouses/Pesticides.cshtml", pesticides);
        }

        public IActionResult Fertilizers()
        {
            if (!CanViewSection(SharedDataSection.Warehouses)) return Forbid();

            var fertilizers = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fertilizers" && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();

            ViewBag.FertilizerTypes = _context.EncyclopediaItems
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fertilizer")
                .OrderBy(x => x.Name)
                .ToList();

            return View("~/Views/Warehouses/Fertilizers.cshtml", fertilizers);
        }

        public IActionResult Seeds()
        {
            if (!CanViewSection(SharedDataSection.Warehouses)) return Forbid();

            var seeds = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Seeds" && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();

            ViewBag.SeedTypes = _context.EncyclopediaItems
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Seed")
                .OrderBy(x => x.Name)
                .ToList();

            return View("~/Views/Warehouses/Seeds.cshtml", seeds);
        }

        public IActionResult Fuel()
        {
            if (!CanViewSection(SharedDataSection.Warehouses)) return Forbid();

            var fuel = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fuel" && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();

            return View("~/Views/Warehouses/Fuel.cshtml", fuel);
        }

        public IActionResult Lubricants()
        {
            if (!CanViewSection(SharedDataSection.Warehouses)) return Forbid();

            var lubricants = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Lubricants" && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();

            return View("~/Views/Warehouses/Lubricants.cshtml", lubricants);
        }
        public IActionResult SpareParts()
        {
            if (!CanViewSection(SharedDataSection.Warehouses)) return Forbid();

            var spareParts = _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "SpareParts" && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();

            return View("~/Views/Warehouses/SpareParts.cshtml", spareParts);
        }
        public IActionResult SparePartsHistory(List<string> types, DateTime? dateFrom, DateTime? dateTo)
        {
            if (!CanViewSection(SharedDataSection.Warehouses)) return Forbid();

            var query = _context.InventoryHistoryModels.Where(x => x.OwnerUserId == EffectiveOwnerUserId).AsQueryable();

            query = query.Where(x => x.Type == "SpareParts");

                query = query.Where(x => x.ChangeDate >= dateFrom.Value)
                .Where(x => x.ChangeDate <= dateTo.Value.AddDays(1));

            var data = query
                .OrderByDescending(x => x.ChangeDate)
                .ToList();

            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            return View(data);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(List<MaterialLogModel> models, string category)
        {
            if (!CanEditSection(SharedDataSection.Warehouses)) return ForbidSharedEdit();

            for (int i = 0; i < models.Count; i++)
            {
                var m = models[i];

                if (string.IsNullOrWhiteSpace(m.Name))
                    continue;

                if (double.TryParse(Request.Form[$"models[{i}].Quantity"], out var quantity))
                    m.Quantity = quantity;
                if (double.TryParse(Request.Form[$"models[{i}].Price"], out var price))
                    m.Price = price;

                m.Category = category;

                if (m.Unit == null)
                    m.Unit = string.Empty;

                if (m.Id == 0)
                {
                    m.Date = DateTime.UtcNow;
                    m.LastModified = DateTime.UtcNow;
                    m.OwnerUserId = EffectiveOwnerUserId;
                    _context.MaterialLogs.Add(m);

                    _inventoryHistoryController.AddToWarehouseHistory(category, m.Name, m.Unit, (double)(m.Quantity ?? 0), (double)(m.Price ?? 0), "Add", (double)(m.Quantity ?? 0), EffectiveOwnerUserId);
                }
                else
                {
                    var existing = _context.MaterialLogs.FirstOrDefault(x => x.Id == m.Id && x.OwnerUserId == EffectiveOwnerUserId);
                    if (existing == null) continue;

                    if (!string.Equals(existing.Name, m.Name, StringComparison.Ordinal))
                    {
                        var oldName = existing.Name;
                        var historyItems = _context.InventoryHistoryModels
                            .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Type == category && x.Name == oldName)
                            .ToList();

                        foreach (var historyItem in historyItems)
                        {
                            historyItem.Name = m.Name;
                            historyItem.Notes = historyItem.Notes.Replace(oldName, m.Name);
                        }
                    }

                    if (existing.Quantity != m.Quantity)
                    {
                        var change = (m.Quantity ?? 0) - (existing.Quantity ?? 0);
                        var unit = m.Unit ?? string.Empty;
                        _inventoryHistoryController.AddToWarehouseHistory(category, m.Name, unit, (double)change, (double)(m.Price ?? 0), "Update", (double)(m.Quantity ?? 0), EffectiveOwnerUserId);
                    }

                    existing.Name = m.Name;
                    existing.Unit = m.Unit;
                    existing.Quantity = m.Quantity;
                    existing.Price = m.Price;
                    existing.TypeId = m.TypeId;
                    existing.LastModified = DateTime.UtcNow;
                }
            }

            _context.SaveChanges();
            return RedirectToAction(category);
        }

        public IActionResult InventoryHistory(List<string> types, DateTime? dateFrom, DateTime? dateTo)
        {
            return RedirectToAction("Index", "InventoryHistory", new { types, dateFrom, dateTo });
        }

        private void AddToWarehouseHistory(string warehouseType, string name, string unit, double quantityChange, double price, string operationType, double balance)
        {
            _inventoryHistoryController.AddToWarehouseHistory(warehouseType, name, unit, quantityChange, price, operationType, balance, CurrentUserId);
        }
    }
}
