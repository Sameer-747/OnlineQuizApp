using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;
using OnlineQuizApp.ViewModels;

namespace OnlineQuizApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var user = await _context.Users
                .Include(u => u.Section)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            return View(user);
        }

        // GET: /Profile/ChangePassword
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Changing the password rotates the security stamp, which would otherwise sign
            // the user out mid-request - refresh the auth cookie so they stay logged in.
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Your password has been changed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
