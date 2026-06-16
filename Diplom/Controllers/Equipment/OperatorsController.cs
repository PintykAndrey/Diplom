using Microsoft.AspNetCore.Mvc;
using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models;
using Diplom.Models.Identity;

namespace Diplom.Controllers.Equipment
{
    public class OperatorsController : BaseController
    {
        public OperatorsController(ApplicationDbContext context) : base(context)
        {
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!CanViewSection(SharedDataSection.Equipment)) return Forbid();

            var operators = _context.Operators
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId)
                .ToList();
            ViewBag.CanEdit = CanEditSection(SharedDataSection.Equipment);
            return View("~/Views/Equipment/Operators.cshtml", operators);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(List<OperatorModel> models)
        {
            if (!CanEditSection(SharedDataSection.Equipment)) return ForbidSharedEdit();

            for (int i = 0; i < models.Count; i++)
            {
                var m = models[i];

                if (string.IsNullOrWhiteSpace(m.FirstName) && string.IsNullOrWhiteSpace(m.LastName))
                    continue;

                if (m.Id == 0)
                {
                    if (m.Surname == null) m.Surname = string.Empty;
                    m.OwnerUserId = EffectiveOwnerUserId;
                    _context.Operators.Add(m);
                }
                else
                {
                    var existing = _context.Operators.FirstOrDefault(x => x.Id == m.Id && x.OwnerUserId == EffectiveOwnerUserId);
                    if (existing == null) continue;

                    existing.FirstName = m.FirstName;
                    existing.LastName = m.LastName;
                    existing.Surname = m.Surname ?? string.Empty;
                }
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
