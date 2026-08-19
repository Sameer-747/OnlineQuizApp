using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;
using OnlineQuizApp.ViewModels;

namespace OnlineQuizApp.Controllers
{
    // Student-facing view of the multi-language test events they've been assigned to.
    [Authorize]
    [Route("MyTests")]
    public class MyTestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyTestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /MyTests
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

            var assignments = await _context.TestEventAssignments
                .Include(a => a.TestEvent)
                .Include(a => a.Quiz)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.TestEvent!.CreatedAt)
                .ToListAsync();

            var quizIds = assignments.Select(a => a.QuizId).ToList();
            var attempts = await _context.QuizAttempts
                .Where(a => a.UserId == userId && quizIds.Contains(a.QuizId))
                .ToListAsync();

            var rows = new List<MyTestEventRow>();
            foreach (var assignment in assignments)
            {
                if (assignment.TestEvent == null || assignment.Quiz == null) continue;

                var attempt = attempts
                    .Where(a => a.QuizId == assignment.QuizId)
                    .OrderByDescending(a => a.CompletedAt)
                    .FirstOrDefault();

                rows.Add(new MyTestEventRow
                {
                    TestEventId = assignment.TestEventId,
                    EventTitle = assignment.TestEvent.Title,
                    QuizId = assignment.QuizId,
                    Language = assignment.Quiz.Title,
                    StartTimeIst = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(assignment.TestEvent.StartTime, DateTimeKind.Utc), ist),
                    EndTimeIst = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(assignment.TestEvent.EndTime, DateTimeKind.Utc), ist),
                    Status = assignment.TestEvent.Status,
                    Attempted = attempt != null,
                    AttemptId = attempt?.Id
                });
            }

            return View(rows);
        }
    }
}
