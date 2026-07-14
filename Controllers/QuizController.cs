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
    public class QuizController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Quiz
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? categoryId)
        {
            var query = _context.Quizzes
                .Include(q => q.Category)
                .Include(q => q.Section)
                .Include(q => q.CreatedByUser)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(q => q.CategoryId == categoryId.Value);

            // Students AND section-admins only see quizzes from their own section, plus truly global quizzes.
            // Only the super admin sees everything unfiltered.
            bool isSuperAdmin = User.Identity?.Name?.ToLower() == "admin@quizapp.com";
            int? viewerSectionId = null;

            if (User.Identity?.IsAuthenticated == true && !isSuperAdmin)
            {
                var userId = _userManager.GetUserId(User);
                var currentUser = await _context.Users.FindAsync(userId);
                viewerSectionId = currentUser?.SectionId;

                query = query.Where(q => q.SectionId == null || q.SectionId == viewerSectionId);
            }

            var categoryQuery = _context.Categories.AsQueryable();
            if (User.Identity?.IsAuthenticated == true && !isSuperAdmin)
            {
                categoryQuery = categoryQuery.Where(c => c.SectionId == null || c.SectionId == viewerSectionId);
            }
            else if (User.Identity?.IsAuthenticated != true)
            {
                categoryQuery = categoryQuery.Where(c => c.SectionId == null);
            }
            ViewBag.Categories = await categoryQuery.ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;

            var attemptedQuizMap = new Dictionary<int, int>(); // quizId -> attemptId

            if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                attemptedQuizMap = await _context.QuizAttempts
                    .Where(a => a.UserId == userId)
                    .GroupBy(a => a.QuizId)
                    .Select(g => new { QuizId = g.Key, AttemptId = g.Max(a => a.Id) })
                    .ToDictionaryAsync(x => x.QuizId, x => x.AttemptId);
            }

            ViewBag.AttemptedQuizMap = attemptedQuizMap;

            return View(await query.ToListAsync());
        }

        // GET: /Quiz/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Category)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();

            return View(quiz);
        }

        // GET: /Quiz/Take/5
        public async Task<IActionResult> Take(int id)
        {
            var userId = _userManager.GetUserId(User);
            bool isSuperAdmin = User.Identity?.Name?.ToLower() == "admin@quizapp.com";

            if (!isSuperAdmin)
            {
                var currentUser = await _context.Users.FindAsync(userId);
                var quizSectionId = await _context.Quizzes
                    .Where(q => q.Id == id)
                    .Select(q => q.SectionId)
                    .FirstOrDefaultAsync();

                if (quizSectionId != null && quizSectionId != currentUser?.SectionId)
                {
                    return Forbid();
                }
            }

            // Block retakes for non-admins: one attempt per quiz, ever.
            if (!User.IsInRole("Admin"))
            {
                var existingAttempt = await _context.QuizAttempts
                    .Where(a => a.QuizId == id && a.UserId == userId)
                    .OrderByDescending(a => a.CompletedAt)
                    .FirstOrDefaultAsync();

                if (existingAttempt != null)
                {
                    TempData["Info"] = "You have already attempted this quiz.";
                    return RedirectToAction(nameof(Result), new { attemptId = existingAttempt.Id });
                }
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();

            var viewModel = new QuizPlayViewModel
            {
                QuizId = quiz.Id,
                Title = quiz.Title,
                DurationMinutes = quiz.DurationMinutes,
                Questions = quiz.Questions
                    .OrderBy(_ => Guid.NewGuid())
                    .Select(q => new QuestionPlayViewModel
                    {
                        QuestionId = q.Id,
                        Text = q.Text,
                        Options = q.Options
                            .OrderBy(_ => Guid.NewGuid())
                            .Select(o => new OptionPlayViewModel
                            {
                                OptionId = o.Id,
                                Text = o.Text
                            }).ToList()
                    }).ToList()
            };

            return View(viewModel);
        }

        // POST: /Quiz/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(QuizSubmissionViewModel submission)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == submission.QuizId);

            if (quiz == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var existingAttempt = await _context.QuizAttempts
                    .Where(a => a.QuizId == submission.QuizId && a.UserId == userId)
                    .OrderByDescending(a => a.CompletedAt)
                    .FirstOrDefaultAsync();

                if (existingAttempt != null)
                {
                    TempData["Info"] = "You have already attempted this quiz.";
                    return RedirectToAction(nameof(Result), new { attemptId = existingAttempt.Id });
                }
            }

            var attempt = new QuizAttempt
            {
                UserId = userId,
                QuizId = quiz.Id,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                TotalQuestions = quiz.Questions.Count
            };

            int score = 0;

            foreach (var question in quiz.Questions)
            {
                var submitted = submission.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                var selectedOptionId = submitted?.SelectedOptionId;

                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                bool isCorrect = selectedOptionId.HasValue
                    && correctOption != null
                    && selectedOptionId.Value == correctOption.Id;

                if (isCorrect) score++;

                attempt.Answers.Add(new UserAnswer
                {
                    QuestionId = question.Id,
                    SelectedOptionId = selectedOptionId
                });
            }

            attempt.Score = score;

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Result), new { attemptId = attempt.Id });
        }

        // GET: /Quiz/Result/5
        public async Task<IActionResult> Result(int attemptId)
        {
            var userId = _userManager.GetUserId(User);

            var attempt = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.Answers)
                    .ThenInclude(ans => ans.Question)
                        .ThenInclude(q => q!.Options)
                .Include(a => a.Answers)
                    .ThenInclude(ans => ans.SelectedOption)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null) return NotFound();
            if (attempt.UserId != userId && !User.IsInRole("Admin")) return Forbid();

            var viewModel = new QuizResultViewModel
            {
                AttemptId = attempt.Id,
                QuizTitle = attempt.Quiz?.Title ?? string.Empty,
                Score = attempt.Score,
                TotalQuestions = attempt.TotalQuestions,
                CompletedAt = attempt.CompletedAt ?? DateTime.UtcNow,
                QuestionResults = attempt.Answers.Select(ans => new QuestionResultViewModel
                {
                    QuestionText = ans.Question?.Text ?? string.Empty,
                    SelectedOptionText = ans.SelectedOption?.Text,
                    CorrectOptionText = ans.Question?.Options.FirstOrDefault(o => o.IsCorrect)?.Text ?? string.Empty,
                    IsCorrect = ans.SelectedOption != null && ans.SelectedOption.IsCorrect
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: /Quiz/History
        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);

            var attempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            return View(attempts);
        }
    }
}
