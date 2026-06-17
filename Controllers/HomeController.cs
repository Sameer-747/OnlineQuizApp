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
