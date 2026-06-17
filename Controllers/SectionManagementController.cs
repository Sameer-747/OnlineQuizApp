using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Sections")]
    public class SectionManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string SuperAdminEmail = "admin@quizapp.com";

        public SectionManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool IsSuperAdmin() =>
            User.Identity?.Name?.ToLower() == SuperAdminEmail.ToLower();

        // GET: /Admin/Sections
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            if (!IsSuperAdmin()) return Forbid();

            var sections = await _context.Sections
                .Include(s => s.AdminUser)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Admins eligible to be assigned: @quizapp.com users who are in the Admin role
            // and not already assigned to a different section.
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var adminUserIds = new HashSet<string>();
            if (adminRole != null)
            {
                adminUserIds = (await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync()).ToHashSet();
            }

            var eligibleAdmins = await _context.Users
                .Where(u => adminUserIds.Contains(u.Id) && u.Email != SuperAdminEmail)
                .OrderBy(u => u.Email)
                .ToListAsync();

            ViewBag.EligibleAdmins = eligibleAdmins;

            return View(sections);
        }

        // POST: /Admin/Sections/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            if (!IsSuperAdmin()) return Forbid();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Section name is required.";
                return RedirectToAction(nameof(Index));
            }

            _context.Sections.Add(new Section { Name = name.Trim() });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Section '{name}' created.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Sections/AssignAdmin
        [HttpPost("AssignAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAdmin(int sectionId, string? adminUserId)
        {
            if (!IsSuperAdmin()) return Forbid();

            var section = await _context.Sections.FindAsync(sectionId);
            if (section == null)
            {
                TempData["Error"] = "Section not found.";
                return RedirectToAction(nameof(Index));
            }

            section.AdminUserId = string.IsNullOrEmpty(adminUserId) ? null : adminUserId;
            await _context.SaveChangesAsync();

            // Keep the user's own SectionId in sync so their "Manage Quizzes" etc. filter correctly.
            if (!string.IsNullOrEmpty(adminUserId))
            {
                var adminUser = await _userManager.FindByIdAsync(adminUserId);
                if (adminUser != null)
                {
                    adminUser.SectionId = section.Id;
                    await _userManager.UpdateAsync(adminUser);
                }
            }

            TempData["Success"] = "Section admin updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Sections/Delete
        [HttpPost("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int sectionId)
        {
            if (!IsSuperAdmin()) return Forbid();

            var section = await _context.Sections.FindAsync(sectionId);
            if (section != null)
            {
                _context.Sections.Remove(section);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Section deleted.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
