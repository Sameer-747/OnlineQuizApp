using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Quiz")]
    public class QuizAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string SuperAdminEmail = "admin@quizapp.com";

        public QuizAdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool IsSuperAdmin() => User.Identity?.Name?.ToLower() == SuperAdminEmail.ToLower();

        // Returns true if this admin is the super admin (sees/manages everything).
        // Returns false + sectionId for a section-admin (sectionId may be null if not yet assigned -> sees nothing).
        private async Task<(bool isSuper, int? sectionId)> GetScopeAsync()
        {
            if (IsSuperAdmin()) return (true, null);

            var userId = _userManager.GetUserId(User);
            var user = await _context.Users.FindAsync(userId);
            return (false, user?.SectionId);
        }

        // GET: /Admin/Quiz
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var (isSuper, sectionId) = await GetScopeAsync();

            var query = _context.Quizzes
                .Include(q => q.Category)
                .Include(q => q.Questions)
                .Include(q => q.Section)
                // Test Event quizzes are managed (and deleted) from the Test Events page,
                // where deleting the parent event cleans up its quizzes + attempts together.
                .Where(q => q.TestEventId == null)
                .AsQueryable();

            if (!isSuper)
            {
                // Section admin with no section assigned yet sees nothing (not everything).
                query = query.Where(q => q.SectionId == sectionId);
                if (sectionId == null)
                {
                    TempData["Error"] = "You are not yet assigned to a section. Ask the super admin to assign you one before creating quizzes.";
                }
            }

            var quizzes = await query.ToListAsync();
            return View(quizzes);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            if (!isSuper && sectionId == null)
            {
                TempData["Error"] = "You are not yet assigned to a section. Ask the super admin to assign you one before creating quizzes.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateCategoriesAsync();
            return View(new Quiz());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quiz quiz)
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            if (!isSuper && sectionId == null)
            {
                TempData["Error"] = "You are not yet assigned to a section.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(quiz);
            }

            // Auto-tag the quiz with the creating admin's section.
            // Super admin's quizzes remain section-less (global/visible to everyone) unless edited later.
            quiz.SectionId = isSuper ? null : sectionId;
            quiz.CreatedByUserId = _userManager.GetUserId(User);

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound();

            if (!isSuper && quiz.SectionId != sectionId) return Forbid();

            await PopulateCategoriesAsync();
            return View(quiz);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Quiz quiz)
        {
            if (id != quiz.Id) return NotFound();

            var (isSuper, sectionId) = await GetScopeAsync();
            var existing = await _context.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == id);
            if (existing == null) return NotFound();
            if (!isSuper && existing.SectionId != sectionId) return Forbid();

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(quiz);
            }

            // Preserve the section tag - section admins can't move a quiz out of their own section.
            quiz.SectionId = existing.SectionId;

            _context.Update(quiz);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var (isSuper, sectionId) = await GetScopeAsync();

            var quiz = await _context.Quizzes
                .Include(q => q.Category)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();
            if (!isSuper && quiz.SectionId != sectionId) return Forbid();

            ViewBag.AttemptCount = await _context.QuizAttempts.CountAsync(a => a.QuizId == id);

            return View(quiz);
        }

        [HttpPost("Delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool force = false)
        {
            var (isSuper, sectionId) = await GetScopeAsync();

            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz != null)
            {
                if (!isSuper && quiz.SectionId != sectionId) return Forbid();

                try
                {
                    if (force)
                    {
                        var attempts = await _context.QuizAttempts
                            .Where(a => a.QuizId == id)
                            .ToListAsync();
                        if (attempts.Count > 0)
                        {
                            _context.QuizAttempts.RemoveRange(attempts);
                            await _context.SaveChangesAsync();
                        }
                    }

                    _context.Quizzes.Remove(quiz);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = force
                        ? "Quiz deleted, along with its recorded student attempts."
                        : "Quiz deleted.";
                }
                catch (DbUpdateException)
                {
                    TempData["Error"] = "Can't delete this quiz: one or more students have already attempted it.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Quiz/BulkDelete
        [HttpPost("BulkDelete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(List<int> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
            {
                TempData["Error"] = "No quizzes were selected.";
                return RedirectToAction(nameof(Index));
            }

            var (isSuper, sectionId) = await GetScopeAsync();

            var query = _context.Quizzes.Where(q => selectedIds.Contains(q.Id));
            if (!isSuper)
            {
                query = query.Where(q => q.SectionId == sectionId);
            }

            var quizzesToDelete = await query.ToListAsync();

            int deleted = 0;
            var blocked = new List<string>();

            foreach (var quiz in quizzesToDelete)
            {
                try
                {
                    _context.Quizzes.Remove(quiz);
                    await _context.SaveChangesAsync();
                    deleted++;
                }
                catch (DbUpdateException)
                {
                    // Roll back just this failed delete so the next one can still be attempted.
                    _context.Entry(quiz).State = EntityState.Unchanged;
                    blocked.Add(quiz.Title);
                }
            }

            if (deleted > 0)
                TempData["Success"] = $"{deleted} quiz(zes) deleted successfully.";

            if (blocked.Count > 0)
                TempData["Error"] = $"Couldn't delete (students already attempted): {string.Join(", ", blocked)}";

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateCategoriesAsync()
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            var query = _context.Categories.AsQueryable();

            if (!isSuper)
            {
                query = query.Where(c => c.SectionId == null || c.SectionId == sectionId);
            }

            ViewBag.Categories = new SelectList(await query.ToListAsync(), "Id", "Name");
        }
    }
}
