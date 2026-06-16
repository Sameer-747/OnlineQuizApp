using Microsoft.AspNetCore.Authorization;
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

        public QuizAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Quiz
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var quizzes = await _context.Quizzes
                .Include(q => q.Category)
                .Include(q => q.Questions)
                .ToListAsync();

            return View(quizzes);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesAsync();
            return View(new Quiz());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quiz quiz)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(quiz);
            }

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null) return NotFound();

            await PopulateCategoriesAsync();
            return View(quiz);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Quiz quiz)
        {
            if (id != quiz.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync();
                return View(quiz);
            }

            _context.Update(quiz);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Category)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();
            return View(quiz);
        }

        [HttpPost("Delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz != null)
            {
                _context.Quizzes.Remove(quiz);
                await _context.SaveChangesAsync();
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

            var quizzesToDelete = await _context.Quizzes
                .Where(q => selectedIds.Contains(q.Id))
                .ToListAsync();

            _context.Quizzes.RemoveRange(quizzesToDelete);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{quizzesToDelete.Count} quiz(zes) deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateCategoriesAsync()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
        }
    }
}
