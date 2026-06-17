using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Results")]
    public class ResultsAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string SuperAdminEmail = "admin@quizapp.com";

        public ResultsAdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Results
        [HttpGet("")]
        public async Task<IActionResult> Index(int? quizId)
        {
            bool isSuper = User.Identity?.Name?.ToLower() == SuperAdminEmail.ToLower();
            int? sectionId = null;

            if (!isSuper)
            {
                var userId = _userManager.GetUserId(User);
                var currentUser = await _context.Users.FindAsync(userId);
                sectionId = currentUser?.SectionId;

                if (sectionId == null)
                {
                    TempData["Error"] = "You are not yet assigned to a section. Ask the super admin to assign you one.";
                }
            }

            var quizQuery = _context.Quizzes.AsQueryable();
            if (!isSuper)
            {
                quizQuery = quizQuery.Where(q => q.SectionId == sectionId);
            }
            var visibleQuizIds = await quizQuery.Select(q => q.Id).ToListAsync();

            var query = _context.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.User)
                .Where(a => visibleQuizIds.Contains(a.QuizId))
                .AsQueryable();

            if (quizId.HasValue)
                query = query.Where(a => a.QuizId == quizId.Value);

            var attempts = await query
                .OrderByDescending(a => a.CompletedAt)
                .ToListAsync();

            ViewBag.Quizzes = await quizQuery.ToListAsync();
            ViewBag.SelectedQuizId = quizId;

            return View(attempts);
        }
    }
}
