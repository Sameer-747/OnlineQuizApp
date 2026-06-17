using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineQuizApp.Data;
using OnlineQuizApp.Models;

namespace OnlineQuizApp.Areas.Identity.Pages.Account
{
    public class StudentRegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public StudentRegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public List<SelectListItem> Sections { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Roll Number")]
            public string RollNumber { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Section")]
            public int SectionId { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        private async Task LoadSectionsAsync()
        {
            var sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
            Sections = sections.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            await LoadSectionsAsync();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            await LoadSectionsAsync();

            if (ModelState.IsValid)
            {
                var section = await _context.Sections.FindAsync(Input.SectionId);
                if (section == null)
                {
                    ModelState.AddModelError(string.Empty, "Please select a valid section.");
                    return Page();
                }

                var rollNumberExists = await _context.Users.AnyAsync(u => u.RollNumber == Input.RollNumber);
                if (rollNumberExists)
                {
                    ModelState.AddModelError(string.Empty, "This roll number is already registered.");
                    return Page();
                }

                // Internal-only username; students never see or type this.
                var generatedUserName = $"{Input.RollNumber}_{section.Name}".Replace(" ", "");

                var user = new ApplicationUser
                {
                    UserName = generatedUserName,
                    FullName = Input.FullName,
                    RollNumber = Input.RollNumber,
                    SectionId = section.Id
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }
    }
}
