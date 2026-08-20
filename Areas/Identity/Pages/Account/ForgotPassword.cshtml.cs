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
    // Self-service password reset that doesn't depend on email (students register with a
    // roll number, not a real inbox - there's nowhere to send a reset link). Identity is
    // instead verified using account details the user already knows, then the password is
    // reset directly using Identity's own reset-token mechanism (generated and consumed
    // in the same request, never emailed).
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> Sections { get; set; } = new();

        public class InputModel
        {
            public string ResetType { get; set; } = "student";

            // Student verification
            public string? RollNumber { get; set; }
            public int? SectionId { get; set; }

            // Admin verification
            public string? Email { get; set; }

            // Required for both - proves the requester actually knows the account.
            [Required(ErrorMessage = "Full name is required.")]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "New password is required.")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your new password.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        private async Task LoadSectionsAsync()
        {
            var sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
            Sections = sections.Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToList();
        }

        public async Task OnGetAsync()
        {
            await LoadSectionsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadSectionsAsync();

            if (!ModelState.IsValid) return Page();

            ApplicationUser? user = null;

            if (Input.ResetType == "admin")
            {
                if (string.IsNullOrWhiteSpace(Input.Email))
                {
                    ModelState.AddModelError(string.Empty, "Email is required.");
                    return Page();
                }

                var candidate = await _userManager.FindByEmailAsync(Input.Email.Trim());
                if (candidate != null
                    && await _userManager.IsInRoleAsync(candidate, "Admin")
                    && string.Equals((candidate.FullName ?? "").Trim(), Input.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    user = candidate;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Input.RollNumber) || Input.SectionId == null)
                {
                    ModelState.AddModelError(string.Empty, "Roll number and section are required.");
                    return Page();
                }

                var candidate = await _context.Users.FirstOrDefaultAsync(u =>
                    u.RollNumber == Input.RollNumber.Trim() &&
                    u.SectionId == Input.SectionId);

                if (candidate != null
                    && string.Equals((candidate.FullName ?? "").Trim(), Input.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    user = candidate;
                }
            }

            if (user == null)
            {
                // Deliberately generic - don't reveal which field was wrong.
                ModelState.AddModelError(string.Empty, "We couldn't verify those details. Double-check them and try again, or ask your admin for help.");
                return Page();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            TempData["ResetSuccess"] = "Your password has been reset. You can log in with your new password now.";
            return RedirectToPage("./Login");
        }
    }
}
