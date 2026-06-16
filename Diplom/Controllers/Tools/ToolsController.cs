using Diplom.Data;
using Diplom.Controllers.Base;
using Diplom.Models;
using Diplom.Models.Identity;
using Diplom.Models.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Diplom.Controllers.Tools
{
    public class ToolsController : BaseController
    {
        public ToolsController(ApplicationDbContext context) : base(context) { }

        public IActionResult Encyclopedia(string category = "Operation")
        {
            if (!CanViewSection(SharedDataSection.Tools)) return Forbid();

            ViewBag.CurrentCategory = category;

            var items = _context.EncyclopediaItems
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == category)
                .OrderBy(x => x.Name)
                .ToList();

            return View("~/Views/Tools/Encyclopedia.cshtml", items);
        }

        [HttpPost]
        public IActionResult Add(string category, string name)
        {
            if (!CanEditSection(SharedDataSection.Tools)) return ForbidSharedEdit();

            if (!string.IsNullOrWhiteSpace(name))
            {
                var normalizedName = name.Trim();
                var exists = _context.EncyclopediaItems.Any(x =>
                    x.OwnerUserId == EffectiveOwnerUserId &&
                    x.Category == category &&
                    x.Name.ToLower() == normalizedName.ToLower());

                if (!exists)
                {
                    _context.EncyclopediaItems.Add(new EncyclopediaItem
                    {
                        OwnerUserId = EffectiveOwnerUserId,
                        Category = category,
                        Name = normalizedName
                    });
                }

                _context.SaveChanges();
            }

            return RedirectToAction("Encyclopedia", new { category });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (!CanEditSection(SharedDataSection.Tools)) return ForbidSharedEdit();

            var item = _context.EncyclopediaItems.FirstOrDefault(x => x.Id == id && x.OwnerUserId == EffectiveOwnerUserId);

            if (item != null)
            {
                if (item.Category == "Operation" && item.Name == "Sowing")
                {
                    return RedirectToAction("Encyclopedia", new { category = item.Category });
                }

                var category = item.Category;
                _context.EncyclopediaItems.Remove(item);
                _context.SaveChanges();

                return RedirectToAction("Encyclopedia", new { category });
            }

            return RedirectToAction("Encyclopedia");
        }

        [HttpPost]
        public IActionResult Update(int id, string name)
        {
            if (!CanEditSection(SharedDataSection.Tools)) return ForbidSharedEdit();

            var item = _context.EncyclopediaItems.FirstOrDefault(x => x.Id == id && x.OwnerUserId == EffectiveOwnerUserId);

            if (item != null && !string.IsNullOrWhiteSpace(name))
            {
                if (item.Category == "Operation" && item.Name == "Sowing")
                {
                    return RedirectToAction("Encyclopedia", new { category = item.Category });
                }

                var normalizedName = name.Trim();
                var exists = _context.EncyclopediaItems.Any(x =>
                    x.Id != item.Id &&
                    x.OwnerUserId == EffectiveOwnerUserId &&
                    x.Category == item.Category &&
                    x.Name.ToLower() == normalizedName.ToLower());

                if (!exists)
                {
                    item.Name = normalizedName;
                }

                _context.SaveChanges();

                return RedirectToAction("Encyclopedia", new { category = item.Category });
            }

            return RedirectToAction("Encyclopedia");
        }

        [HttpGet]
        public IActionResult GetStatistics()
        {
            var stats = new
            {
                operations = _context.EncyclopediaItems.Count(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Operation"),
                seeds = _context.EncyclopediaItems.Count(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Seed"),
                fertilizers = _context.EncyclopediaItems.Count(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Fertilizer"),
                pesticides = _context.EncyclopediaItems.Count(x => x.OwnerUserId == EffectiveOwnerUserId && x.Category == "Pesticide")
            };

            return Json(stats);
        }

        [HttpGet]
        public IActionResult Vocabulary()
        {
            var model = _context.Vocabulary
                .AsNoTracking()
                .GroupBy(v => v.Key)
                .Select(g => new
                {
                    Key = g.Key,
                    Translations = g.Select(t => new { t.Language, t.Value }).ToList()
                })
                .ToList();

            return View("~/Views/Tools/Vocabulary.cshtml", model);
        }
    }
}

