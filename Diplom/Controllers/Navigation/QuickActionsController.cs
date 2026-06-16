using Diplom.Data;
using Microsoft.AspNetCore.Mvc;
using Diplom.Controllers.Base;
using Diplom.Models.Navigation;

namespace Diplom.Controllers.Navigation
{
    public class QuickActionsController : BaseController
    {
        public QuickActionsController(ApplicationDbContext context) : base(context) { }

        [HttpGet]
        public IActionResult GetActions()
        {
            var actions = _context.UserQuickActions
                .Where(u => u.OwnerUserId == CurrentUserId)
                .OrderBy(u => u.DisplayOrder)
                .ToList();
            return Json(actions);
        }

        [HttpPost]
        public IActionResult Create([FromBody] UserQuickAction model)
        {
            model.OwnerUserId = CurrentUserId;
            model.UserId = CurrentUserId;
            model.ActionKey = "custom";
            model.IsEnabled = true;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            _context.UserQuickActions.Add(model);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult Toggle(int id, bool enabled)
        {
            var action = _context.UserQuickActions.FirstOrDefault(x => x.Id == id && x.OwnerUserId == CurrentUserId);
            if (action == null)
                return Json(new { success = false });

            action.IsEnabled = enabled;
            action.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateOrder(int id, int order)
        {
            var action = _context.UserQuickActions.FirstOrDefault(x => x.Id == id && x.OwnerUserId == CurrentUserId);
            if (action == null)
                return Json(new { success = false });

            action.DisplayOrder = order;
            action.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult Delete([FromQuery] int id)
        {
            var action = _context.UserQuickActions.FirstOrDefault(x => x.Id == id && x.OwnerUserId == CurrentUserId);
            if (action == null)
                return Json(new { success = false });

            _context.UserQuickActions.Remove(action);
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}
