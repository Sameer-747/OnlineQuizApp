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
        private readonly ApplicationDbContext _context;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _signInManager = signInManager;
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
            // "admin" or "student" - which login form was used
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
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

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

                usernameToSignIn = $"{Input.RollNumber}_{section.Name}".Replace(" ", "");
            }

            var result = await _signInManager.PasswordSignInAsync(usernameToSignIn, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}
