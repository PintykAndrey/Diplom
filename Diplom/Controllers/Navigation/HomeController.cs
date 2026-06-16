using System.Diagnostics;

using Diplom.Models;
using Diplom.Controllers.Base;
using Diplom.Models.Identity;
using Diplom.Models.Navigation;

using Diplom.Localization;

using Diplom.Data;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace Diplom.Controllers

{

    public class HomeController : BaseController

    {

        private readonly ILogger<HomeController> _logger;

        private readonly DbVocabularyStringLocalizer _localizer;

        public HomeController(ILogger<HomeController> logger, DbVocabularyStringLocalizer localizer, ApplicationDbContext context)
            : base(context)

        {

            _logger = logger;

            _localizer = localizer;

        }


        public IActionResult Index()

        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var enabledActions = _context.UserQuickActions

                .Where(u => u.OwnerUserId == userId && u.IsEnabled)

                .OrderBy(u => u.DisplayOrder)

                .ToList();


            ViewBag.EnabledQuickActions = enabledActions;
            ViewBag.CanViewFields = CanViewSection(SharedDataSection.Fields);
            ViewBag.CanViewWarehouses = CanViewSection(SharedDataSection.Warehouses);
            ViewBag.CanViewEquipment = CanViewSection(SharedDataSection.Equipment);
            ViewBag.CanViewTools = CanViewSection(SharedDataSection.Tools);

            ViewData["Title"] = _localizer["Dashboard"];

            return View();

        }

        [HttpGet]
        public IActionResult GetDashboardStatistics()
        {
            var userId = EffectiveOwnerUserId;

            var stats = new
            {
                totalFields = CanViewSection(SharedDataSection.Fields) ? _context.Fields.Count(x => x.OwnerUserId == userId && x.ArchivedAt == null) : 0,
                totalArea = CanViewSection(SharedDataSection.Fields) ? _context.Fields.Where(x => x.OwnerUserId == userId && x.ArchivedAt == null).Sum(x => x.AreaHectares) : 0,
                operators = CanViewSection(SharedDataSection.Equipment) ? _context.Operators.Count(x => x.OwnerUserId == userId) : 0,
                equipment = CanViewSection(SharedDataSection.Equipment) ? _context.Equipments.Count(x => x.OwnerUserId == userId) : 0,
                operations = CanViewSection(SharedDataSection.Tools) ? _context.EncyclopediaItems.Count(x => x.OwnerUserId == userId && x.Category == "Operation") : 0,
                archivedFields = CanViewSection(SharedDataSection.Tools) ? _context.Fields.Count(x => x.OwnerUserId == userId && x.ArchivedAt != null) : 0,
                totalItems = CanViewSection(SharedDataSection.Warehouses) ? _context.MaterialLogs
                    .Where(x => x.OwnerUserId == userId && x.ArchivedAt == null && !string.IsNullOrWhiteSpace(x.Name))
                    .Select(x => x.Name)
                    .Distinct()
                    .Count() : 0
            };

            return Json(stats);
        }

        [HttpGet]
        public IActionResult GetQuickActionsSettings()

        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userActions = _context.UserQuickActions
                .Where(u => u.OwnerUserId == userId)
                .OrderBy(u => u.DisplayOrder)
                .Select(u => new
                {
                    u.Id,

                    u.Name,

                    u.Icon,

                    u.Color,

                    u.Url,

                    u.IsEnabled,

                    u.DisplayOrder
                })
                .ToList();

            return Json(userActions);

        }

        [HttpPost]
        public IActionResult AddQuickAction([FromBody] QuickActionCreateModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var action = new UserQuickAction
            {
                OwnerUserId = userId,
                UserId = userId,
                Name = model.Name,
                Icon = model.Icon,
                Color = model.Color,
                Url = model.Url,
                IsEnabled = true,
                DisplayOrder = model.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserQuickActions.Add(action);

            _context.SaveChanges();

            return Json(new { success = true, id = action.Id });
        }

        [HttpPost]
        public IActionResult UpdateQuickAction([FromBody] QuickActionUpdateModel model)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var action = _context.UserQuickActions.FirstOrDefault(u => u.Id == model.Id && u.OwnerUserId == userId);

            if (action == null) return Json(new { success = false });

            action.Name = model.Name;

            action.Icon = model.Icon;

            action.Color = model.Color;

            action.Url = model.Url;

            action.IsEnabled = model.IsEnabled;

            action.DisplayOrder = model.DisplayOrder;

            action.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteQuickAction(int id)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var action = _context.UserQuickActions.FirstOrDefault(u => u.Id == id && u.OwnerUserId == userId);

            if (action == null) return Json(new { success = false });

                _context.UserQuickActions.Remove(action);

            _context.SaveChanges();

            return Json(new { success = true });

        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public class QuickActionCreateModel
        {
            public string Name { get; set; }
            public string Icon { get; set; }
            public string Color { get; set; }
            public string Url { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class QuickActionUpdateModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Icon { get; set; }
            public string Color { get; set; }
            public string Url { get; set; }
            public bool IsEnabled { get; set; }
            public int DisplayOrder { get; set; }

        }

        public class QuickActionSetting
        {
            public string Key { get; set; }
            public bool IsEnabled { get; set; }
            public int DisplayOrder { get; set; }

        }

    }

}