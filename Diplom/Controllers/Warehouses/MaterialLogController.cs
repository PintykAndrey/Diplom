using Diplom.Data;
using Diplom.Controllers.Base;
using Diplom.Models.Identity;
using Diplom.Models.Warehouses;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Diplom.Controllers.Warehouses
{
    public class MaterialLogController : BaseController
    {
        public MaterialLogController(ApplicationDbContext context) : base(context) { }

        public IActionResult Seeds()
        {
            return View("Seeds", GetData("Seeds"));
        }

        public IActionResult Pesticides()
        {
            return View("Pesticides", GetData("Pesticides"));
        }

        public IActionResult Fertilizers()
        {
            return View("Fertilizers", GetData("Fertilizers"));
        }

        public IActionResult Chemicals()
        {
            return View("Chemicals", GetData("Chemicals"));
        }

        private List<MaterialLogModel> GetData(string category)
        {
            ViewBag.Category = category;

            return _context.MaterialLogs
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == category && x.ArchivedAt == null)
                .OrderByDescending(x => x.LastModified)
                .ToList();
        }

        [HttpPost]
        public IActionResult Save(List<MaterialLogModel> models, string category)
        {
            if (!CanEditSection(SharedDataSection.Warehouses)) return ForbidSharedEdit();

            for (int i = 0; i < models.Count; i++)
            {
                var m = models[i];

                if (string.IsNullOrWhiteSpace(m.Name))
                    continue;

                m.Quantity = ParseDouble(Request.Form[$"models[{i}].Quantity"]);
                m.Price = ParseDouble(Request.Form[$"models[{i}].Price"]);
                m.Category = category;

                if (m.Id == 0)
                {
                    m.Date = DateTime.UtcNow;
                    m.LastModified = DateTime.UtcNow;
                    m.OwnerUserId = EffectiveOwnerUserId;
                    _context.MaterialLogs.Add(m);
                }
                else
                {
                    var existing = _context.MaterialLogs.FirstOrDefault(x => x.Id == m.Id && x.OwnerUserId == EffectiveOwnerUserId);
                    if (existing == null) continue;

                    existing.Name = m.Name;
                    existing.Unit = m.Unit;
                    existing.Quantity = m.Quantity;
                    existing.Price = m.Price;
                    existing.LastModified = DateTime.UtcNow;
                }
            }

            _context.SaveChanges();

            return RedirectToAction(category);
        }

        private double? ParseDouble(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;

            return double.TryParse(val.Replace(',', '.'),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var d) ? d : null;
        }
    }
}
