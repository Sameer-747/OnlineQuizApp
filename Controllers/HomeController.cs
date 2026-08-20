using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Test Event quizzes live under "My Tests" (with their own scheduling/assignment
            // rules), not the regular browsing flow - only count/show quizzes here that a
            // student could actually open from this page.
            var query = _context.Categories
                .Include(c => c.Quizzes.Where(q => q.TestEventId == null))
                .AsQueryable();

            bool isSuperAdmin = User.Identity?.Name?.ToLower() == "admin@quizapp.com";

            if (User.Identity?.IsAuthenticated == true && !isSuperAdmin)
            {
                // Both students AND section-admins only see their own section's categories, plus global ones.
                var userId = _userManager.GetUserId(User);
                var currentUser = await _context.Users.FindAsync(userId);
                var ownSectionId = currentUser?.SectionId;

                query = query.Where(c => c.SectionId == null || c.SectionId == ownSectionId);
            }
            else if (User.Identity?.IsAuthenticated != true)
            {
                // Anonymous visitors only see global categories.
                query = query.Where(c => c.SectionId == null);
            }
            // Super admin (isSuperAdmin == true) sees everything - no filter applied.

            var categories = (await query.ToListAsync())
                .Where(c => c.Quizzes.Count > 0)
                .ToList();

            // Show a "you have a live test" card while any assigned Test Event is currently
            // active and not yet attempted - it disappears on its own once the event expires,
            // since the query below simply stops matching it.
            int activeTestCount = 0;
            if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                var now = DateTime.UtcNow;

                var activeAssignments = await _context.TestEventAssignments
                    .Where(a => a.UserId == userId
                        && a.TestEvent!.StartTime <= now
                        && a.TestEvent!.EndTime >= now)
                    .ToListAsync();

                if (activeAssignments.Count > 0)
                {
                    var quizIds = activeAssignments.Select(a => a.QuizId).ToList();
                    var attemptedQuizIds = await _context.QuizAttempts
                        .Where(a => a.UserId == userId && quizIds.Contains(a.QuizId))
                        .Select(a => a.QuizId)
                        .ToListAsync();

                    activeTestCount = activeAssignments.Count(a => !attemptedQuizIds.Contains(a.QuizId));
                }
            }
            ViewBag.ActiveTestCount = activeTestCount;

            return View(categories);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
