using Diplom.Controllers.Base;
using Diplom.Data;
using Diplom.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Controllers
{
    [Authorize]
    public class SharingController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public SharingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var friendIds = await _context.Friendships
                .Where(x => x.UserId == CurrentUserId)
                .Select(x => x.FriendUserId)
                .ToListAsync();

            var friends = await _context.Users
                .Where(x => friendIds.Contains(x.Id))
                .OrderBy(x => x.DisplayName)
                .ToListAsync();

            var incomingRequests = await _context.FriendRequests
                .Where(x => x.ReceiverUserId == CurrentUserId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var incomingUserIds = incomingRequests.Select(x => x.SenderUserId).ToList();
            var incomingUsers = await _context.Users
                .Where(x => incomingUserIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var outgoingRequests = await _context.FriendRequests
                .Where(x => x.SenderUserId == CurrentUserId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var outgoingUserIds = outgoingRequests.Select(x => x.ReceiverUserId).ToList();
            var outgoingUsers = await _context.Users
                .Where(x => outgoingUserIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var grants = await _context.DataAccessGrants
                .Where(x => x.OwnerUserId == CurrentUserId)
                .ToListAsync();

            var sharedWithMeGrants = await _context.DataAccessGrants
                .Where(x => x.GranteeUserId == CurrentUserId)
                .ToListAsync();

            ViewBag.Friends = friends;
            ViewBag.IncomingRequests = incomingRequests;
            ViewBag.IncomingUsers = incomingUsers;
            ViewBag.OutgoingRequests = outgoingRequests;
            ViewBag.OutgoingUsers = outgoingUsers;
            ViewBag.Grants = grants;
            ViewBag.SharedWithMeGrants = sharedWithMeGrants;
            ViewBag.Sections = Enum.GetValues<SharedDataSection>();
            ViewBag.AccessLevels = Enum.GetValues<DataAccessLevel>();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> IncomingRequestsCount()
        {
            var count = await _context.FriendRequests.CountAsync(x => x.ReceiverUserId == CurrentUserId);
            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(string login)
        {
            var target = await FindUserByLoginAsync(login);
            if (target == null || target.Id == CurrentUserId)
                return RedirectToAction(nameof(Index));

            var alreadyFriends = await _context.Friendships.AnyAsync(x => x.UserId == CurrentUserId && x.FriendUserId == target.Id);
            var alreadyRequested = await _context.FriendRequests.AnyAsync(x => x.SenderUserId == CurrentUserId && x.ReceiverUserId == target.Id);
            var hasIncomingRequest = await _context.FriendRequests.AnyAsync(x => x.SenderUserId == target.Id && x.ReceiverUserId == CurrentUserId);

            if (!alreadyFriends && !alreadyRequested && !hasIncomingRequest)
            {
                _context.FriendRequests.Add(new FriendRequest
                {
                    SenderUserId = CurrentUserId,
                    ReceiverUserId = target.Id
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var request = await _context.FriendRequests.FirstOrDefaultAsync(x => x.Id == id && x.ReceiverUserId == CurrentUserId);
            if (request == null)
                return RedirectToAction(nameof(Index));

            _context.Friendships.Add(new Friendship { UserId = CurrentUserId, FriendUserId = request.SenderUserId });
            _context.Friendships.Add(new Friendship { UserId = request.SenderUserId, FriendUserId = CurrentUserId });
            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var request = await _context.FriendRequests.FirstOrDefaultAsync(x => x.Id == id && x.ReceiverUserId == CurrentUserId);
            if (request != null)
            {
                _context.FriendRequests.Remove(request);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFriend(string friendUserId)
        {
            var links = await _context.Friendships
                .Where(x => (x.UserId == CurrentUserId && x.FriendUserId == friendUserId) || (x.UserId == friendUserId && x.FriendUserId == CurrentUserId))
                .ToListAsync();

            var grants = await _context.DataAccessGrants
                .Where(x => (x.OwnerUserId == CurrentUserId && x.GranteeUserId == friendUserId) || (x.OwnerUserId == friendUserId && x.GranteeUserId == CurrentUserId))
                .ToListAsync();

            _context.Friendships.RemoveRange(links);
            _context.DataAccessGrants.RemoveRange(grants);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAccess(string friendUserId, SharedDataSection section, DataAccessLevel accessLevel)
        {
            var isFriend = await _context.Friendships.AnyAsync(x => x.UserId == CurrentUserId && x.FriendUserId == friendUserId);
            if (!isFriend)
                return RedirectToAction(nameof(Index));

            var grant = await _context.DataAccessGrants.FirstOrDefaultAsync(x => x.OwnerUserId == CurrentUserId && x.GranteeUserId == friendUserId && x.Section == section);
            if (grant == null)
            {
                _context.DataAccessGrants.Add(new DataAccessGrant
                {
                    OwnerUserId = CurrentUserId,
                    GranteeUserId = friendUserId,
                    Section = section,
                    AccessLevel = accessLevel
                });
            }
            else
            {
                grant.AccessLevel = accessLevel;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAccess(int id)
        {
            var grant = await _context.DataAccessGrants.FirstOrDefaultAsync(x => x.Id == id && x.OwnerUserId == CurrentUserId);
            if (grant != null)
            {
                _context.DataAccessGrants.Remove(grant);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpenSharedData(string ownerUserId, string returnUrl = null)
        {
            var hasAccess = await _context.DataAccessGrants.AnyAsync(x =>
                x.OwnerUserId == ownerUserId &&
                x.GranteeUserId == CurrentUserId);

            if (!hasAccess)
                return RedirectToLocal(returnUrl);

            SharedOwnerUserId = ownerUserId;

            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CloseSharedData(string returnUrl = null)
        {
            HttpContext.Session.Remove("SharedOwnerUserId");
            return RedirectToLocal(returnUrl);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        private async Task<ApplicationUser> FindUserByLoginAsync(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return null;

            var normalizedLogin = login.Trim();

            if (normalizedLogin.Contains('@'))
                return await _userManager.FindByEmailAsync(normalizedLogin);

            return await _context.Users.FirstOrDefaultAsync(x => x.UserTag == normalizedLogin);
        }
    }
}

