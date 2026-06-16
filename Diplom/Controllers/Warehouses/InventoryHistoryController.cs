using Microsoft.AspNetCore.Mvc;
using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models.Warehouses;
using Diplom.Models.Identity;

namespace Diplom.Controllers.Warehouses
{
    public class InventoryHistoryController : BaseController
    {
        public InventoryHistoryController(ApplicationDbContext context) : base(context) { }

        public IActionResult Index(List<string> types, DateTime? dateFrom, DateTime? dateTo)
        {
            if (!CanViewSection(SharedDataSection.Warehouses)) return Forbid();

            var query = _context.InventoryHistoryModels
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId)
                .AsQueryable();

            if (types != null && types.Any())
            {
                query = query.Where(x => types.Contains(x.Type));
            }

            if (dateFrom.HasValue)
            {
                var dateFromUtc = DateTime.SpecifyKind(dateFrom.Value, DateTimeKind.Utc).Date;
                query = query.Where(x => x.ChangeDate.Date >= dateFromUtc);
            }

            if (dateTo.HasValue)
            {
                var dateToUtc = DateTime.SpecifyKind(dateTo.Value, DateTimeKind.Utc).Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.ChangeDate.Date <= dateToUtc);
            }

            var history = query.OrderByDescending(x => x.ChangeDate).ToList();
            
            ViewBag.SelectedTypes = types;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            
            return View("~/Views/Warehouses/InventoryHistory.cshtml", history);
        }

        public void AddToWarehouseHistory(string warehouseType, string name, string unit, double quantityChange, double price, string operationType, double balance, string ownerUserId)
        {
            var historyItem = new InventoryHistoryModel
            {
                OwnerUserId = ownerUserId,
                ChangeDate = DateTime.UtcNow,
                Name = name,
                Unit = unit,
                QuantityChange = (decimal)quantityChange,
                Price = (decimal)price,
                Type = warehouseType, 
                Balance = (decimal)balance,
                Notes = $"{operationType}: {name} - {quantityChange} {unit}"
            };

            _context.InventoryHistoryModels.Add(historyItem);
        }
    }
}
