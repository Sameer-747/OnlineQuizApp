using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Question")]
    public class QuestionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string SuperAdminEmail = "admin@quizapp.com";

        public QuestionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

        private async Task<bool> CanAccessQuizAsync(int quizId)
        {
            var (isSuper, sectionId) = await GetScopeAsync();
            if (isSuper) return true;

            var quizSectionId = await _context.Quizzes
                .Where(q => q.Id == quizId)
                .Select(q => q.SectionId)
                .FirstOrDefaultAsync();

            return quizSectionId == sectionId;
        }

        // GET: /Admin/Question?quizId=1
        [HttpGet("")]
        public async Task<IActionResult> Index(int quizId)
        {
            if (!await CanAccessQuizAsync(quizId)) return Forbid();

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            return View(quiz);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create(int quizId)
        {
            if (!await CanAccessQuizAsync(quizId)) return Forbid();

            var question = new Question
            {
                QuizId = quizId,
                Options = new List<Option>
                {
                    new Option(), new Option(), new Option(), new Option()
                }
            };
            return View(question);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Question question, int correctOptionIndex)
        {
            if (!await CanAccessQuizAsync(question.QuizId)) return Forbid();

            if (!ModelState.IsValid) return View(question);

            for (int i = 0; i < question.Options.Count; i++)
            {
                question.Options.ElementAt(i).IsCorrect = (i == correctOptionIndex);
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { quizId = question.QuizId });
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound();
            if (!await CanAccessQuizAsync(question.QuizId)) return Forbid();

            return View(question);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Question question, int correctOptionIndex)
        {
            if (id != question.Id) return NotFound();
            if (!await CanAccessQuizAsync(question.QuizId)) return Forbid();

            if (!ModelState.IsValid) return View(question);

            for (int i = 0; i < question.Options.Count; i++)
            {
                question.Options.ElementAt(i).IsCorrect = (i == correctOptionIndex);
            }

            _context.Update(question);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { quizId = question.QuizId });
        }

        [HttpGet("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound();
            if (!await CanAccessQuizAsync(question.QuizId)) return Forbid();

            return View(question);
        }

        [HttpPost("Delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return RedirectToAction(nameof(Index), new { quizId = 0 });

            if (!await CanAccessQuizAsync(question.QuizId)) return Forbid();

            int quizId = question.QuizId;
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { quizId });
        }
    }
}
