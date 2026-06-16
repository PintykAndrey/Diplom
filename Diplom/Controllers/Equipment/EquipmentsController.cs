using Diplom.Controllers.Base;
using Diplom.Data;

using Diplom.Models;
using Diplom.Models.Identity;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;


namespace Diplom.Controllers

{

    public class EquipmentsController : BaseController

    {

        public EquipmentsController(ApplicationDbContext context)
            : base(context)
        {
        }

        [HttpGet]
        public IActionResult Index(int? type, int? id)
        {
            if (!CanViewSection(SharedDataSection.Equipment)) return Forbid();

            var equipmentsQuery = _context.Equipments
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId)
                .AsQueryable();

            if (type.HasValue)
            {
                equipmentsQuery = equipmentsQuery

                    .Where(x => (int)x.Type == type.Value);
            }
            var equipments = equipmentsQuery.ToList();
            var journals = _context.EquipmentJournals
                .Include(x => x.JournalMaterials)
                .Where(x => x.OwnerUserId == EffectiveOwnerUserId && x.ArchivedAt == null)
                .OrderBy(x => x.Id)
                .ToList();
            var materialIds = journals
                .SelectMany(x => x.JournalMaterials ?? new List<EquipmentJournalModel.EquipmentJournalMaterialModel>())
                .Where(x => x.ArchivedAt == null && x.MaterialId.HasValue)
                .Select(x => x.MaterialId!.Value)
                .Distinct()
                .ToList();
            var materialLookup = materialIds.Count == 0
                ? new Dictionary<int, Diplom.Models.Warehouses.MaterialLogModel>()
                : _context.MaterialLogs
                    .Where(x => x.OwnerUserId == EffectiveOwnerUserId && materialIds.Contains(x.Id))
                    .ToDictionary(x => x.Id, x => x);
            EquipmentModel selectedEquipment = null;
            if (id.HasValue)
            {
                selectedEquipment = _context.Equipments.FirstOrDefault(x => x.Id == id.Value && x.OwnerUserId == EffectiveOwnerUserId);

            }
            ViewBag.Type = type;
            ViewBag.SelectedEquipment = selectedEquipment;
            ViewBag.Journals = journals;
            ViewBag.MaterialLookup = materialLookup;
            return View("~/Views/Equipment/Equipments.cshtml", equipments);
        }
    }
}