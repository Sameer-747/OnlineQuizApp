using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;
using OnlineQuizApp.Services;
using OnlineQuizApp.ViewModels;

namespace OnlineQuizApp.Controllers
{
    [Authorize]
    public class QuizController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly BadgeService _badgeService;

        public QuizController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, BadgeService badgeService)
        {
            _context = context;
            _userManager = userManager;
            _badgeService = badgeService;
        }

        // GET: /Quiz
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? categoryId)
        {
            var query = _context.Quizzes
                .Include(q => q.Category)
                .Include(q => q.Section)
                .Include(q => q.CreatedByUser)
                // Test-event language quizzes have their own dedicated "My Tests" flow with
                // scheduling/assignment rules, so keep them out of the regular quiz browsing list.
                .Where(q => q.TestEventId == null)
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
            ViewBag.IsAdminViewer = User.IsInRole("Admin");

            return View(await query.ToListAsync());
        }

        // If quizId belongs to a multi-language TestEvent, verify the caller was actually assigned
        // that exact language and that the event's time window is currently open. Returns an
        // IActionResult to short-circuit the caller if access should be blocked, or null to proceed.
        // Super admins bypass this entirely (for preview/QA purposes).
        private async Task<IActionResult?> CheckTestEventAccessAsync(int quizId, string? userId, bool isSuperAdmin)
        {
            if (isSuperAdmin) return null;

            var testEventId = await _context.Quizzes
                .Where(q => q.Id == quizId)
                .Select(q => q.TestEventId)
                .FirstOrDefaultAsync();

            if (testEventId == null) return null;

            var testEvent = await _context.TestEvents.FindAsync(testEventId.Value);
            if (testEvent == null) return null;

            var hasAssignment = await _context.TestEventAssignments
                .AnyAsync(a => a.TestEventId == testEventId.Value && a.UserId == userId && a.QuizId == quizId);

            if (!hasAssignment)
            {
                TempData["Error"] = "You were not assigned this test.";
                return RedirectToAction("Index", "MyTests");
            }

            var now = DateTime.UtcNow;
            if (now < testEvent.StartTime)
            {
                TempData["Info"] = "This test hasn't started yet.";
                return RedirectToAction("Index", "MyTests");
            }

            if (now > testEvent.EndTime)
            {
                TempData["Error"] = "This test has expired.";
                return RedirectToAction("Index", "MyTests");
            }

            return null;
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

            ViewBag.IsAdminViewer = User.IsInRole("Admin");

            return View(quiz);
        }

        // GET: /Quiz/Take/5
        public async Task<IActionResult> Take(int id)
        {
            // Admins manage quizzes, they don't attempt them.
            if (User.IsInRole("Admin"))
            {
                TempData["Info"] = "Admin accounts can't attempt quizzes.";
                return RedirectToAction(nameof(Index));
            }

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

            // If this is a multi-language test-event quiz, enforce the assignment + time window.
            var testEventGuard = await CheckTestEventAccessAsync(id, userId, isSuperAdmin);
            if (testEventGuard != null) return testEventGuard;

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
                IsTestEvent = quiz.TestEventId != null,
                Questions = quiz.Questions.Select(q => new QuestionPlayViewModel
                {
                    QuestionId = q.Id,
                    Text = q.Text,
                    Options = q.Options.Select(o => new OptionPlayViewModel
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
            if (User.IsInRole("Admin"))
            {
                TempData["Info"] = "Admin accounts can't attempt quizzes.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            bool isSuperAdminSubmit = User.Identity?.Name?.ToLower() == "admin@quizapp.com";
            var testEventGuard = await CheckTestEventAccessAsync(submission.QuizId, userId, isSuperAdminSubmit);
            if (testEventGuard != null) return testEventGuard;

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
                TotalQuestions = quiz.Questions.Count,
                // Only meaningful for proctored Test Event quizzes; stays 0/false otherwise
                // since regular quizzes never post these fields.
                TabSwitchCount = submission.TabSwitchCount,
                AutoSubmitted = submission.AutoSubmitted,
                ViolationReason = submission.ViolationReason
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

            // Award badges based on performance
            var attemptWithQuiz = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .FirstOrDefaultAsync(a => a.Id == attempt.Id);
            if (attemptWithQuiz != null)
                await _badgeService.AwardBadgesAsync(attemptWithQuiz);

            return RedirectToAction(nameof(Result), new { attemptId = attempt.Id });
        }

        // POST: /Quiz/ReportCameraViolation - fired by the proctoring JS on Test Event exams
        // when a sustained camera issue (no face / multiple faces / phone in frame) is
        // detected. Best-effort: saves evidence for admin review but never blocks the exam.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportCameraViolation([FromBody] CameraViolationRequest request)
        {
            if (User.IsInRole("Admin")) return Ok();

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            bool isSuperAdminReport = User.Identity?.Name?.ToLower() == "admin@quizapp.com";
            var guard = await CheckTestEventAccessAsync(request.QuizId, userId, isSuperAdminReport);
            if (guard != null) return Ok(); // don't leak access details here; just skip silently

            if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.ImageData))
                return BadRequest();

            // Guard against oversized payloads (the client sends a small downscaled JPEG).
            if (request.ImageData.Length > 3_000_000)
                return BadRequest();

            _context.ExamSnapshots.Add(new ExamSnapshot
            {
                UserId = userId,
                QuizId = request.QuizId,
                Reason = request.Reason.Length > 200 ? request.Reason[..200] : request.Reason,
                ImageData = request.ImageData
            });
            await _context.SaveChangesAsync();

            return Ok();
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

            var ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

            var viewModel = new QuizResultViewModel
            {
                AttemptId = attempt.Id,
                QuizTitle = attempt.Quiz?.Title ?? string.Empty,
                Score = attempt.Score,
                TotalQuestions = attempt.TotalQuestions,
                CompletedAt = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(attempt.CompletedAt ?? DateTime.UtcNow, DateTimeKind.Utc), ist),
                AutoSubmitted = attempt.AutoSubmitted,
                TabSwitchCount = attempt.TabSwitchCount,
                ViolationReason = attempt.ViolationReason,
                QuestionResults = attempt.Answers.Select(ans => new QuestionResultViewModel
                {
                    QuestionText = ans.Question?.Text ?? string.Empty,
                    SelectedOptionText = ans.SelectedOption?.Text,
                    CorrectOptionText = ans.Question?.Options.FirstOrDefault(o => o.IsCorrect)?.Text ?? string.Empty,
                    IsCorrect = ans.SelectedOption != null && ans.SelectedOption.IsCorrect
                }).ToList()
            };

            // Load badges earned on this attempt
            var newBadges = await _context.StudentBadges
                .Where(b => b.UserId == userId && b.QuizAttemptId == attemptId)
                .ToListAsync();
            ViewBag.NewBadges = newBadges;

            // Pass percentage for certificate eligibility
            double percentage = attempt.TotalQuestions > 0
                ? (double)attempt.Score / attempt.TotalQuestions * 100 : 0;
            ViewBag.Percentage = percentage;
            ViewBag.AttemptId = attemptId;

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

        // GET: /Quiz/MyBadges
        public async Task<IActionResult> MyBadges()
        {
            var userId = _userManager.GetUserId(User);

            var badges = await _context.StudentBadges
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.EarnedAt)
                .ToListAsync();

            var currentStreak = await _badgeService.GetCurrentStreakAsync(userId!);
            var longestStreak = await _badgeService.GetLongestStreakAsync(userId!);
            var totalAttempts = await _context.QuizAttempts.CountAsync(a => a.UserId == userId);
            var attempts = await _context.QuizAttempts
                .Where(a => a.UserId == userId && a.TotalQuestions > 0)
                .Select(a => new { a.Score, a.TotalQuestions })
                .ToListAsync();

            var avgScore = attempts.Any()
                ? Math.Round(attempts.Average(a => (double)a.Score / a.TotalQuestions * 100), 1)
                : 0.0;

            ViewBag.CurrentStreak = currentStreak;
            ViewBag.LongestStreak = longestStreak;
            ViewBag.TotalAttempts = totalAttempts;
            ViewBag.AvgScore = Math.Round(avgScore, 1);

            return View(badges);
        }

        // GET: /Quiz/Certificate/5
        public async Task<IActionResult> Certificate(int attemptId)
        {
            var userId = _userManager.GetUserId(User);

            var attempt = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null) return NotFound();
            if (attempt.UserId != userId) return Forbid();

            double percentage = attempt.TotalQuestions > 0
                ? (double)attempt.Score / attempt.TotalQuestions * 100 : 0;

            if (percentage < 70)
                return RedirectToAction(nameof(Result), new { attemptId });

            var ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            ViewBag.CompletedAt = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(attempt.CompletedAt ?? DateTime.UtcNow, DateTimeKind.Utc), ist);
            ViewBag.Percentage = Math.Round(percentage, 1);

            return View(attempt);
        }
    }
}
