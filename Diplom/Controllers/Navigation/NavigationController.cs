using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Diplom.Controllers.Navigation
{
    public class NavigationController : BaseController
    {
        public NavigationController(ApplicationDbContext context) : base(context) { }

        [HttpGet]
        public IActionResult Fields()
        {
            if (!CanViewSection(SharedDataSection.Fields)) return Forbid();

            return View("~/Views/Fields/Index.cshtml");
        }

        [HttpGet]
        public IActionResult Warehouses()
        {
            if (!CanViewSection(SharedDataSection.Warehouses)) return Forbid();

            return View("~/Views/Warehouses/Index.cshtml");
        }

        [HttpGet]
        public IActionResult Equipment()
        {
            if (!CanViewSection(SharedDataSection.Equipment)) return Forbid();

            return View("~/Views/Equipment/Index.cshtml");
        }

        [HttpGet]
        [Route("Equipment/Equipments")]
        public IActionResult Equipments(int? type, int? id)
        {
            return RedirectToAction("Index", "Equipments", new { type, id });
        }

        [HttpGet]
        public IActionResult Maintenance()
        {
            return RedirectToAction("Maintenance", "Equipment");
        }

        [HttpGet]
        public IActionResult Tools()
        {
            if (!CanViewSection(SharedDataSection.Tools)) return Forbid();

            return View("~/Views/Tools/Index.cshtml");
        }

        [HttpGet]
        [Route("Fields/CropRotation")]
        public IActionResult CropRotation()
        {
            return RedirectToAction("CropRotation", "CropRotation");
        }

        [HttpGet]
        public IActionResult FieldWorkLog()
        {
            return RedirectToAction("FieldWorkLogPlan", "Fields");
        }

        [HttpGet]
        [Route("Fields/FieldSituation")]
        public IActionResult FieldSituationLog()
        {
            return RedirectToAction("FieldSituationLog", "FieldSituation");
        }

        [HttpGet]
        [Route("Fields/FieldsJournal")]
        public IActionResult FieldsJournal(int? id)
        {
            return RedirectToAction("FieldsJournal", "FieldsJournal", new { id });
        }

        [HttpGet]
        public IActionResult Vocabulary()
        {
            return RedirectToAction("Vocabulary", "Tools");
        }

        [HttpGet]
        [Route("Equipment/EquipmentJournal")]
        public IActionResult EquipmentJournal()
        {
            return RedirectToAction("Index", "EquipmentJournal");
        }

        [HttpGet]
        [Route("Equipment/Operators")]
        public IActionResult Operators()
        {
            return RedirectToAction("Index", "Operators");
        }

        [HttpGet]
        [Route("Tools/Archive")]
        public IActionResult Archive()
        {
            return RedirectToAction("Archive", "Archive");
        }
    }
}
