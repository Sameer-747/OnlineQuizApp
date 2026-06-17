using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Admins")]
    public class AdminManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string SuperAdminEmail = "admin@quizapp.com";

        public AdminManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool IsSuperAdmin() =>
            User.Identity?.Name?.ToLower() == SuperAdminEmail.ToLower();

        // GET: /Admin/Admins
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            if (!IsSuperAdmin())
                return Forbid();

            var allUsers = await _context.Users
                .Where(u => u.Email != null && u.Email.ToLower().EndsWith("@quizapp.com"))
                .OrderBy(u => u.Email)
                .ToListAsync();

            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var adminUserIds = new HashSet<string>();

            if (adminRole != null)
            {
                adminUserIds = (await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync()).ToHashSet();
            }

            ViewBag.AdminUserIds = adminUserIds;

            return View(allUsers);
        }

        // POST: /Admin/Admins/CreateAdmin
        [HttpPost("CreateAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(string email, string fullName, string password)
        {
            if (!IsSuperAdmin())
                return Forbid();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Email and password are required.";
                return RedirectToAction(nameof(Index));
            }

            if (!email.ToLower().EndsWith("@quizapp.com"))
            {
                TempData["Error"] = "Admin email must end with @quizapp.com.";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                TempData["Error"] = "An account with this email already exists.";
                return RedirectToAction(nameof(Index));
            }

            var newAdmin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = string.IsNullOrWhiteSpace(fullName) ? email : fullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newAdmin, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newAdmin, "Admin");
                TempData["Success"] = $"Admin account created for {email}. Share these credentials with them: Email: {email}, Password: {password}";
            }
            else
            {
                TempData["Error"] = "Failed to create admin: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Admins/Promote
        [HttpPost("Promote")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Promote(string userId)
        {
            if (!IsSuperAdmin())
                return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            if (user.Email == null || !user.Email.ToLower().EndsWith("@quizapp.com"))
            {
                TempData["Error"] = "Only @quizapp.com users can be made Admin.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["Success"] = $"{user.Email} is now an Admin.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Admins/Demote
        [HttpPost("Demote")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Demote(string userId)
        {
            if (!IsSuperAdmin())
                return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent removing the super admin itself
            if (user.Email?.ToLower() == SuperAdminEmail.ToLower())
            {
                TempData["Error"] = "Cannot remove the super admin.";
                return RedirectToAction(nameof(Index));
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                TempData["Success"] = $"{user.Email} is no longer an Admin.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Admins/DeleteAccount
        [HttpPost("DeleteAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(string userId)
        {
            if (!IsSuperAdmin())
                return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            if (user.Email?.ToLower() == SuperAdminEmail.ToLower())
            {
                TempData["Error"] = "Cannot delete the super admin account.";
                return RedirectToAction(nameof(Index));
            }

            var email = user.Email;
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = $"{email}'s account has been permanently deleted.";
            }
            else
            {
                TempData["Error"] = $"Failed to delete {email}: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
