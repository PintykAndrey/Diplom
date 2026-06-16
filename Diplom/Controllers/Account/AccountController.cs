using Diplom.Models.ViewModels;
using Diplom.Data;
using Diplom.Models.Tools;
using Diplom.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Diplom.Models.Identity;

namespace Diplom.Controllers.Account
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                DisplayName = model.UserName,
                UserTag = await GenerateUniqueUserTagAsync(model.UserName)
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                _context.EncyclopediaItems.Add(new EncyclopediaItem
                {
                    OwnerUserId = user.Id,
                    Category = "Operation",
                    Name = "Sowing"
                });
                await _context.SaveChangesAsync();

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(
                    nameof(ConfirmEmail),
                    "Account",
                    new { userId = user.Id, token },
                    Request.Scheme);

                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Confirm your email",
                    $"<p>Confirm your account by clicking this link:</p><p><a href=\"{confirmationLink}\">Confirm email</a></p>");

                TempData["StatusMessage"] = "Registration successful. Check your email and confirm your account.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(Login));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToAction(nameof(Login));

            var result = await _userManager.ConfirmEmailAsync(user, token);
            TempData["StatusMessage"] = result.Succeeded
                ? "Email confirmed. You can now log in."
                : "Email confirmation failed.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["StatusMessage"] = TempData["StatusMessage"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await FindUserByLoginAsync(model.Login);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Email is not confirmed.");
                ViewData["UnconfirmedEmail"] = user.Email;
                return View(model);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, model.RememberMe);
                return LocalRedirect(returnUrl ?? Url.Action("Index", "Home"));
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        private async Task<ApplicationUser> FindUserByLoginAsync(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return null;

            var normalizedLogin = login.Trim();

            if (normalizedLogin.Contains('@'))
            {
                var userByEmail = await _userManager.FindByEmailAsync(normalizedLogin);
                if (userByEmail != null)
                    return userByEmail;
            }

            return await _context.Users.FirstOrDefaultAsync(x =>
                x.UserTag == normalizedLogin);
        }

        private async Task<string> GenerateUniqueUserTagAsync(string displayName)
        {
            var baseTag = Regex.Replace(displayName?.Trim().ToLowerInvariant() ?? "user", "[^a-z0-9]+", "");
            if (string.IsNullOrWhiteSpace(baseTag))
                baseTag = "user";

            string tag;
            do
            {
                tag = $"{baseTag}#{Random.Shared.Next(1000, 10000)}";
            }
            while (await _context.Users.AnyAsync(x => x.UserTag == tag));

            return tag;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Remove("SharedOwnerUserId");
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["StatusMessage"] = "Enter your email first.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["StatusMessage"] = "If the email exists, a confirmation message will be sent.";
                return RedirectToAction(nameof(Login));
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["StatusMessage"] = "Email is already confirmed. You can log in.";
                return RedirectToAction(nameof(Login));
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new { userId = user.Id, token },
                Request.Scheme);

            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Confirm your email",
                    $"<p>Confirm your account by clicking this link:</p><p><a href=\"{confirmationLink}\">Confirm email</a></p>");
            }
            catch (Exception ex)
            {
                TempData["StatusMessage"] = $"Email sending failed: {ex.Message}";
                return RedirectToAction(nameof(Login));
            }

            TempData["StatusMessage"] = "Confirmation email sent again. Check Inbox and Spam.";
            return RedirectToAction(nameof(Login));
        }
    }
}
