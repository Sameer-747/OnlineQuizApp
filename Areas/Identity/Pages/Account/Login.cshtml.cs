using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public List<SelectListItem> Sections { get; set; } = new();

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            public string LoginType { get; set; } = "student";

            // Admin login
            public string? Email { get; set; }

            // Student login
            public string? RollNumber { get; set; }
            public int? SectionId { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        private async Task LoadSectionsAsync()
        {
            var sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
            Sections = sections.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            returnUrl ??= Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ReturnUrl = returnUrl;
            await LoadSectionsAsync();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            await LoadSectionsAsync();

            string usernameToSignIn;

            if (Input.LoginType == "admin")
            {
                if (string.IsNullOrWhiteSpace(Input.Email))
                {
                    ModelState.AddModelError(string.Empty, "Email is required.");
                    return Page();
                }

                // Verify this email actually exists as an admin account
                var adminUser = await _userManager.FindByEmailAsync(Input.Email);
                if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }

                usernameToSignIn = Input.Email;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Input.RollNumber) || Input.SectionId == null)
                {
                    ModelState.AddModelError(string.Empty, "Roll number and section are required.");
                    return Page();
                }

                var section = await _context.Sections.FindAsync(Input.SectionId);
                if (section == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid section selected.");
                    return Page();
                }

                // Strictly verify the roll number exists in the database for this section
                var studentUser = await _context.Users.FirstOrDefaultAsync(u =>
                    u.RollNumber == Input.RollNumber.Trim() &&
                    u.SectionId == Input.SectionId);

                if (studentUser == null)
                {
                    ModelState.AddModelError(string.Empty, "No account found with this roll number and section. Please register first.");
                    return Page();
                }

                usernameToSignIn = studentUser.UserName!;
            }

            var result = await _signInManager.PasswordSignInAsync(usernameToSignIn, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return LocalRedirect(returnUrl);

            if (result.IsLockedOut)
                return RedirectToPage("./Lockout");

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}
