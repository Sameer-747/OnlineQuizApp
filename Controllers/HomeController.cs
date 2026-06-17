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
            var query = _context.Categories.Include(c => c.Quizzes).AsQueryable();

            // Students (and anonymous visitors) only see categories from their own section, plus global ones.
            // Admins see everything here for simplicity (this is just the landing page).
            if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Admin"))
            {
                var userId = _userManager.GetUserId(User);
                var currentUser = await _context.Users.FindAsync(userId);
                var studentSectionId = currentUser?.SectionId;

                query = query.Where(c => c.SectionId == null || c.SectionId == studentSectionId);
            }
            else if (User.Identity?.IsAuthenticated != true)
            {
                // Anonymous visitors only see global categories.
                query = query.Where(c => c.SectionId == null);
            }

            var categories = await query.ToListAsync();

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
