using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;

namespace OnlineQuizApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Results")]
    public class ResultsAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResultsAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Results
        [HttpGet("")]
        public async Task<IActionResult> Index(int? quizId)
        {
            var query = _context.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.User)
                .AsQueryable();

            if (quizId.HasValue)
                query = query.Where(a => a.QuizId == quizId.Value);

            var attempts = await query
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            ViewBag.Quizzes = await _context.Quizzes.ToListAsync();
            ViewBag.SelectedQuizId = quizId;

            return View(attempts);
        }
    }
}
