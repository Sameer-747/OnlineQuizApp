using Microsoft.AspNetCore.Authorization;
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

        public QuestionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Question?quizId=1
        [HttpGet("")]
        public async Task<IActionResult> Index(int quizId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            return View(quiz);
        }

        [HttpGet("Create")]
        public IActionResult Create(int quizId)
        {
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
            return View(question);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Question question, int correctOptionIndex)
        {
            if (id != question.Id) return NotFound();
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
            return View(question);
        }

        [HttpPost("Delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            int quizId = question?.QuizId ?? 0;

            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { quizId });
        }
    }
}
