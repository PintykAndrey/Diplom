using Diplom.Data;
using Diplom.Models.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Diplom.Controllers.Base
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        protected string SharedOwnerUserId
        {
            get => HttpContext.Session.GetString("SharedOwnerUserId");
            set => HttpContext.Session.SetString("SharedOwnerUserId", value);
        }

        protected string EffectiveOwnerUserId
        {
            get
            {
                var sharedOwnerUserId = SharedOwnerUserId;
                return string.IsNullOrWhiteSpace(sharedOwnerUserId) ? CurrentUserId : sharedOwnerUserId;
            }
        }

        protected bool IsSharedMode => EffectiveOwnerUserId != CurrentUserId;

        protected IQueryable<T> Own<T>(IQueryable<T> query) where T : class, IOwnedEntity
        {
            return query.Where(x => x.OwnerUserId == EffectiveOwnerUserId);
        }

        protected void SetOwner<T>(T entity) where T : IOwnedEntity
        {
            entity.OwnerUserId = EffectiveOwnerUserId;
        }

        protected bool HasSharedAccess(string ownerUserId, SharedDataSection section, DataAccessLevel requiredLevel)
        {
            return _context.DataAccessGrants.Any(x =>
                x.OwnerUserId == ownerUserId &&
                x.GranteeUserId == CurrentUserId &&
                x.Section == section &&
                (x.AccessLevel == DataAccessLevel.Edit || requiredLevel == DataAccessLevel.View));
        }

        protected bool CanViewSection(SharedDataSection section)
        {
            return !IsSharedMode || HasSharedAccess(EffectiveOwnerUserId, section, DataAccessLevel.View);
        }

        protected bool CanEditSection(SharedDataSection section)
        {
            return !IsSharedMode || HasSharedAccess(EffectiveOwnerUserId, section, DataAccessLevel.Edit);
        }

        protected IActionResult ForbidSharedEdit()
        {
            return Forbid();
        }
    }
}
