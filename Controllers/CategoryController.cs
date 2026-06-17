using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string SuperAdminEmail = "admin@quizapp.com";

        public CategoryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool IsSuperAdmin() => User.Identity?.Name?.ToLower() == SuperAdminEmail.ToLower();

        private async Task<(bool isSuper, int? sectionId)> GetScopeAsync()
        {
            if (IsSuperAdmin()) return (true, null);

            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            return (false, user?.SectionId);
        }

        public async Task<IActionResult> Index()
        {
            var (isSuper, sectionId) = await GetScopeAsync();

            var query = _context.Categories.Include(c => c.Quizzes).AsQueryable();

            if (!isSuper)
            {
                query = query.Where(c => c.SectionId == sectionId);
                if (sectionId == null)
                {
                    TempData["Error"] = "You are not yet assigned to a section. Ask the super admin to assign you one before managing categories.";
                }
            }

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            if (!isSuper && sectionId == null)
            {
                TempData["Error"] = "You are not yet assigned to a section.";
                return RedirectToAction(nameof(Index));
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            if (!isSuper && sectionId == null)
            {
                TempData["Error"] = "You are not yet assigned to a section.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid) return View(category);

            category.SectionId = isSuper ? null : sectionId;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            if (!isSuper && category.SectionId != sectionId) return Forbid();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            var (isSuper, sectionId) = await GetScopeAsync();
            var existing = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null) return NotFound();
            if (!isSuper && existing.SectionId != sectionId) return Forbid();

            if (!ModelState.IsValid) return View(category);

            category.SectionId = existing.SectionId;

            _context.Update(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            if (!isSuper && category.SectionId != sectionId) return Forbid();

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                if (!isSuper && category.SectionId != sectionId) return Forbid();

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
